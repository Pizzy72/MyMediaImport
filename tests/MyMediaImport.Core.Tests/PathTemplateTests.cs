namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class PathTemplateTests
{
    [TestMethod]
    public void SupportedPlaceholders_DescribeTheImplementedSyntax()
    {
        string[] syntax = PathTemplate.SupportedPlaceholders
            .Select(placeholder => placeholder.Syntax)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "{capture:FORMAT}",
                "{captureUtc:FORMAT}",
                "{original}",
                "{ext}",
                "{collision:FORMAT}"
            },
            syntax);
        Assert.AreEqual("{capture:yyyy-MM-dd_HHmmss}", PathTemplate.SupportedPlaceholders[0].Example);
        Assert.AreEqual("{captureUtc:yyyy-MM-dd_HHmmss'Z'}", PathTemplate.SupportedPlaceholders[1].Example);
        Assert.AreEqual("{collision:_00}", PathTemplate.SupportedPlaceholders[4].Example);
    }

    private static readonly DateTimeOffset CaptureTime =
        new(2026, 8, 22, 17, 30, 23, TimeSpan.FromHours(2));

    [TestMethod]
    public void Render_UsesCaptureTimeWithOriginalOffset()
    {
        PathTemplate template = new("{capture:yyyy-MM-dd_HHmmss}");

        string result = template.Render(CreateItem());

        Assert.AreEqual("2026-08-22_173023", result);
    }

    [TestMethod]
    public void Render_ConvertsCaptureTimeToUtcBeforeFormatting()
    {
        PathTemplate template = new("{captureUtc:yyyy-MM-dd_HHmmss'Z'}");

        string result = template.Render(CreateItem());

        Assert.AreEqual("2026-08-22_153023Z", result);
    }

    [TestMethod]
    public void Render_UsesLocalCaptureTimeIndependentlyOfResolvedOffset()
    {
        PathTemplate template = new("{capture:yyyy-MM-dd_HHmmss}");
        MediaItem plusTwo = CreateItem(CaptureTimestamp.FromKnownTime(CaptureTime));
        MediaItem minusFive = CreateItem(CaptureTimestamp.FromKnownTime(
            new DateTimeOffset(2026, 8, 22, 17, 30, 23, TimeSpan.FromHours(-5))));

        Assert.AreEqual(template.Render(plusTwo), template.Render(minusFive));
        Assert.AreEqual("2026-08-22_173023", template.Render(plusTwo));
    }

    [TestMethod]
    public void Render_RequiresResolvedOffsetForUtcPlaceholder()
    {
        PathTemplate template = new("{captureUtc:yyyy-MM-dd_HHmmss'Z'}");
        MediaItem item = CreateItem(CaptureTimestamp.FromLocalTime(
            new DateTime(2026, 8, 22, 17, 30, 23)));

        Assert.ThrowsExactly<UnresolvedCaptureTimeException>(() => template.Render(item));
    }

    [TestMethod]
    public void Render_UsesUtcTimeAfterExplicitTimezoneResolution()
    {
        CaptureTimestamp unresolved = CaptureTimestamp.FromLocalTime(
            new DateTime(2026, 8, 23, 8, 43, 28));
        CaptureTimestamp resolved = new CaptureTimeZoneResolver().Resolve(
            unresolved, CaptureTimeZoneSpec.Parse("-04:00"));
        MediaItem item = CreateItem(resolved);
        PathTemplate template = new(
            "{capture:yyyy-MM-dd_HHmmss}|{captureUtc:yyyy-MM-dd_HHmmss'Z'}");

        Assert.AreEqual(
            "2026-08-23_084328|2026-08-23_124328Z",
            template.Render(item));
    }

    [TestMethod]
    public void Render_RejectsZGeneratedFromLocalCaptureTime()
    {
        PathTemplate template = new("{capture:yyyy-MM-dd_HHmmss'Z'}");

        Assert.ThrowsExactly<FormatException>(() => template.Render(CreateItem()));
    }

    [TestMethod]
    public void Render_UsesOriginalNameWithoutExtensionAndExtensionWithoutDot()
    {
        PathTemplate template = new("{original}.{ext}");

        string result = template.Render(CreateItem(name: "IMG_1234.HEIC"));

        Assert.AreEqual("IMG_1234.heic", result);
    }

    [TestMethod]
    [DataRow("photo.JPG", "photo.jpg")]
    [DataRow("photo.jpg", "photo.jpg")]
    [DataRow("photo.JpG", "photo.jpg")]
    [DataRow("photo.HEIC", "photo.heic")]
    [DataRow("video.MOV", "video.mov")]
    public void Render_NormalizesExtensionToInvariantLowercase(
        string sourceName,
        string expectedTargetName)
    {
        MediaItem item = CreateItem(name: sourceName);

        string result = new PathTemplate("{original}.{ext}").Render(item);

        Assert.AreEqual(expectedTargetName, result);
        Assert.AreEqual(sourceName, item.Name);
    }

    [TestMethod]
    public void Render_SupportsNestedTargetPaths()
    {
        PathTemplate template = new(
            @"E:\Bilder\{captureUtc:yyyy}\{captureUtc:MM}\{original}.{ext}");

        string result = template.Render(CreateItem());

        Assert.AreEqual(@"E:\Bilder\2026\08\IMG_1234.heic", result);
    }

    [TestMethod]
    public void Render_ExpandsEnvironmentVariablesAfterTemplatePlaceholders()
    {
        const string variableName = "MYMEDIAIMPORT_PATH_TEMPLATE_TEST_ROOT";
        string? previousValue = Environment.GetEnvironmentVariable(variableName);
        string rootPath = Path.Combine(Path.GetTempPath(), "MyMediaImportTemplateTest");
        try
        {
            Environment.SetEnvironmentVariable(variableName, rootPath);
            string templateText =
                $"%{variableName}%{Path.DirectorySeparatorChar}{{original}}.{{ext}}";
            PathTemplate template = new(templateText);

            string result = template.Render(CreateItem());

            Assert.AreEqual(Path.Combine(rootPath, "IMG_1234.heic"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [TestMethod]
    public void Render_OmitsCollisionSuffixWithoutConflict()
    {
        PathTemplate template = new("{original}{collision:_00}.{ext}");

        Assert.AreEqual("IMG_1234.heic", template.Render(CreateItem()));
    }

    [TestMethod]
    [DataRow(2, "IMG_1234_02.heic")]
    [DataRow(3, "IMG_1234_03.heic")]
    public void Render_FormatsAdditionalCollisionNumbers(int collisionNumber, string expected)
    {
        PathTemplate template = new("{original}{collision:_00}.{ext}");

        Assert.AreEqual(expected, template.Render(CreateItem(), collisionNumber));
    }

    [TestMethod]
    public void Render_DifferentExtensionsProduceDifferentTargetPaths()
    {
        PathTemplate template = new("{original}{collision:_00}.{ext}");

        string photoPath = template.Render(CreateItem(name: "IMG_1234.jpg"));
        string videoPath = template.Render(CreateItem(name: "IMG_1234.mp4"));

        Assert.AreNotEqual(photoPath, videoPath);
        Assert.AreEqual("IMG_1234.jpg", photoPath);
        Assert.AreEqual("IMG_1234.mp4", videoPath);
    }

    [TestMethod]
    public void Render_RejectsUnknownPlaceholder()
    {
        PathTemplate template = new("{unknown}");

        Assert.ThrowsExactly<FormatException>(() => template.Render(CreateItem()));
    }

    [TestMethod]
    public void Render_RejectsUnmatchedBrace()
    {
        PathTemplate template = new("{original");

        Assert.ThrowsExactly<FormatException>(() => template.Render(CreateItem()));
    }

    [TestMethod]
    public void Render_RequiresCaptureTimeForTimePlaceholder()
    {
        PathTemplate template = new("{capture:yyyy}");

        Assert.ThrowsExactly<MissingCaptureTimeException>(
            () => template.Render(CreateItemWithoutCaptureTime()));
    }

    [TestMethod]
    public void Render_DoesNotRequireCaptureTimeWithoutTimePlaceholder()
    {
        PathTemplate template = new("{original}.{ext}");

        Assert.AreEqual(
            "IMG_1234.heic",
            template.Render(CreateItemWithoutCaptureTime()));
    }

    private static MediaItem CreateItem(
        string name = "IMG_1234.heic") =>
        CreateItem(CaptureTimestamp.FromKnownTime(CaptureTime), name);

    private static MediaItem CreateItem(
        CaptureTimestamp captureTime,
        string name = "IMG_1234.heic") =>
        new(
            id: "item-1",
            name: name,
            size: 12_345,
            captureTime: captureTime,
            mediaKind: MediaKind.Photo,
            mimeType: "image/heic");

    private static MediaItem CreateItemWithoutCaptureTime() =>
        new(
            id: "item-without-time",
            name: "IMG_1234.heic",
            size: 12_345,
            captureTime: null,
            mediaKind: MediaKind.Photo,
            mimeType: "image/heic");
}
