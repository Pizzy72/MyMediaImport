namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaExtensionSelectionRuleTests
{
    [TestMethod]
    public void IsMatch_SingleExtension_SelectsMatchingFile()
    {
        MediaExtensionSelectionRule rule = MediaExtensionSelectionRule.Parse("JPG");

        Assert.IsTrue(rule.IsMatch(CreateItem("photo.JPG")));
        Assert.IsFalse(rule.IsMatch(CreateItem("clip.MOV")));
    }

    [TestMethod]
    public void IsMatch_MultipleExtensions_SelectEachRequestedType()
    {
        MediaExtensionSelectionRule rule = MediaExtensionSelectionRule.Parse("JPG,HEIC,MOV");

        Assert.IsTrue(rule.IsMatch(CreateItem("one.jpg")));
        Assert.IsTrue(rule.IsMatch(CreateItem("two.heic")));
        Assert.IsTrue(rule.IsMatch(CreateItem("three.mov")));
        Assert.IsFalse(rule.IsMatch(CreateItem("four.jpeg")));
    }

    [TestMethod]
    public void IsMatch_IgnoresCase()
    {
        MediaExtensionSelectionRule rule = MediaExtensionSelectionRule.Parse("hEiC");

        Assert.IsTrue(rule.IsMatch(CreateItem("PHOTO.HeIc")));
    }

    [TestMethod]
    public void Parse_RejectsLeadingDot()
    {
        Assert.ThrowsExactly<FormatException>(() =>
            MediaExtensionSelectionRule.Parse(".JPG"));
        Assert.ThrowsExactly<FormatException>(() =>
            MediaExtensionSelectionRule.Parse("JPG,.HEIC"));
    }

    [TestMethod]
    public void IsMatch_FileWithoutRequestedExtension_IsRejected()
    {
        MediaExtensionSelectionRule rule = MediaExtensionSelectionRule.Parse("JPG");

        Assert.IsFalse(rule.IsMatch(CreateItem("photo")));
        Assert.IsFalse(rule.IsMatch(CreateItem("photo.HEIC")));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("JPG,")]
    [DataRow("JPG,,MOV")]
    [DataRow(".")]
    [DataRow("JP/G")]
    [DataRow("JPG.")]
    public void Parse_InvalidInput_IsRejected(string value)
    {
        Assert.ThrowsExactly<FormatException>(() =>
            MediaExtensionSelectionRule.Parse(value));
    }

    [TestMethod]
    public void CombinedSelection_RequiresMatchingDateAndExtension()
    {
        LocalCaptureDateRangeSelectionRule dateRule = new(
            new LocalCaptureDateRange(
                new DateOnly(2026, 8, 23), new DateOnly(2026, 8, 23)));
        MediaExtensionSelectionRule extensionRule = MediaExtensionSelectionRule.Parse("JPG,HEIC");
        AllOfMediaSelectionRule combined = new(dateRule, extensionRule);

        Assert.IsTrue(combined.IsMatch(CreateItem("today.jpg", new DateTime(2026, 8, 23, 12, 0, 0))));
        Assert.IsFalse(combined.IsMatch(CreateItem("today.mov", new DateTime(2026, 8, 23, 12, 0, 0))));
        Assert.IsFalse(combined.IsMatch(CreateItem("yesterday.jpg", new DateTime(2026, 8, 22, 12, 0, 0))));
    }

    private static MediaItem CreateItem(
        string name,
        DateTime? captureTime = null) =>
        new(
            name,
            name,
            1,
            CaptureTimestamp.FromLocalTime(
                captureTime ?? new DateTime(2026, 8, 23, 12, 0, 0)),
            MediaKind.Photo,
            null);
}
