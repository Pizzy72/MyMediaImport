namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class ImportPlannerTests
{
    private const string TargetTemplate =
        @"C:\Imports\{capture:yyyy}\{original}{collision:_00}.{ext}";
    private static readonly string[] expected = new[]
            {
                @"C:\Imports\2026\photo_02.jpg",
                @"C:\Imports\2026\photo_03.jpg",
                @"C:\Imports\2026\photo_04.jpg"
            };

    [TestMethod]
    public async Task CreatePlanAsync_PlansMultipleFilesWithoutConflicts()
    {
        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "one.jpg"), CreateItem("2", "two.mp4")],
            ExistingFilePolicy.Rename);

        Assert.HasCount(2, plan.Items);
        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        Assert.AreEqual(@"C:\Imports\2026\one.jpg", plan.Items[0].TargetPath);
        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[1].Status);
        Assert.AreEqual(@"C:\Imports\2026\two.mp4", plan.Items[1].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_MarksExistingFileForSkip()
    {
        string existing = @"C:\Imports\2026\photo.jpg";

        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "photo.jpg")], ExistingFilePolicy.Skip, existing);

        Assert.AreEqual(ImportPlanStatus.ExistingFile, plan.Items[0].Status);
        Assert.AreEqual(existing, plan.Items[0].TargetPath);
        Assert.IsNotNull(plan.Items[0].Diagnostic);
    }

    [TestMethod]
    public async Task CreatePlanAsync_RenamesExistingFile()
    {
        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "photo.jpg")],
            ExistingFilePolicy.Rename,
            @"C:\Imports\2026\photo.jpg");

        Assert.AreEqual(ImportPlanStatus.Renamed, plan.Items[0].Status);
        Assert.AreEqual(@"C:\Imports\2026\photo_02.jpg", plan.Items[0].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_OverwritesExistingFileWithoutRenaming()
    {
        string existing = @"C:\Imports\2026\photo.jpg";

        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "photo.jpg")], ExistingFilePolicy.Overwrite, existing);

        Assert.AreEqual(ImportPlanStatus.WillOverwrite, plan.Items[0].Status);
        Assert.AreEqual(existing, plan.Items[0].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_RenamesIdenticalSelectedTargets()
    {
        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "photo.jpg"), CreateItem("2", "photo.jpg")],
            ExistingFilePolicy.Rename);

        Assert.AreEqual(@"C:\Imports\2026\photo.jpg", plan.Items[0].TargetPath);
        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        Assert.AreEqual(@"C:\Imports\2026\photo_02.jpg", plan.Items[1].TargetPath);
        Assert.AreEqual(ImportPlanStatus.Renamed, plan.Items[1].Status);
    }

    [TestMethod]
    public async Task CreatePlanAsync_RepeatedRenamePlan_FindsIdenticalBaseAndSecondCollision()
    {
        MediaItem first = CreateItem("1", "photo.jpg");
        MediaItem second = CreateItem("2", "photo.jpg");
        byte[] firstContent = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
        byte[] secondContent = [2, 2, 2, 2, 2, 2, 2, 2, 2, 2];
        Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase)
        {
            [@"C:\Imports\2026\photo.jpg"] = firstContent,
            [@"C:\Imports\2026\photo_02.jpg"] = secondContent
        };
        ImportPlanner planner = new(new ContentTargetFileLookup(files));
        ContentMediaSource source = new(new Dictionary<string, byte[]>
        {
            [first.Id] = firstContent,
            [second.Id] = secondContent
        });

        ImportPlan plan = await planner.CreatePlanAsync(
            [first, second], source, new PathTemplate(TargetTemplate), ExistingFilePolicy.Rename, TestContext.CancellationToken);

        CollectionAssert.AreEqual(
            new[] { ImportPlanStatus.AlreadyImported, ImportPlanStatus.AlreadyImported },
            plan.Items.Select(item => item.Status).ToArray());
        Assert.AreEqual(@"C:\Imports\2026\photo.jpg", plan.Items[0].TargetPath);
        Assert.AreEqual(@"C:\Imports\2026\photo_02.jpg", plan.Items[1].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_AssignsMultipleCollisionNumbersDeterministically()
    {
        MediaItem[] items = new[]
        {
            CreateItem("1", "photo.jpg"),
            CreateItem("2", "photo.jpg"),
            CreateItem("3", "photo.jpg")
        };

        ImportPlan plan = await CreatePlanAsync(
            items,
            ExistingFilePolicy.Rename,
            @"C:\Imports\2026\photo.jpg");

        CollectionAssert.AreEqual(
            expected,
            plan.Items.Select(item => item.TargetPath).ToArray());
    }

    [TestMethod]
    public async Task CreatePlanAsync_DifferentExtensionsDoNotConflict()
    {
        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "same.jpg"), CreateItem("2", "same.mp4")],
            ExistingFilePolicy.Rename);

        Assert.IsTrue(plan.Items.All(item => item.Status == ImportPlanStatus.Ready));
        Assert.AreNotEqual(plan.Items[0].TargetPath, plan.Items[1].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_DifferentlyCasedSourceExtensions_UseNormalizedCollisionPaths()
    {
        ImportPlan plan = await CreatePlanAsync(
            [CreateItem("1", "same.JPG"), CreateItem("2", "same.JpG")],
            ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        Assert.AreEqual(@"C:\Imports\2026\same.jpg", plan.Items[0].TargetPath);
        Assert.AreEqual(ImportPlanStatus.Renamed, plan.Items[1].Status);
        Assert.AreEqual(@"C:\Imports\2026\same_02.jpg", plan.Items[1].TargetPath);
    }

    [TestMethod]
    public async Task CreatePlanAsync_ReportsMissingCaptureTime()
    {
        MediaItem item = new("1", "photo.jpg", 10, null, MediaKind.Photo, "image/jpeg");

        ImportPlan plan = await CreatePlanAsync([item], ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportPlanStatus.MissingCaptureTime, plan.Items[0].Status);
        Assert.AreEqual(string.Empty, plan.Items[0].TargetPath);
        Assert.IsNotNull(plan.Items[0].Diagnostic);
    }

    [TestMethod]
    public async Task CreatePlanAsync_ReportsInvalidPathTemplate()
    {
        ImportPlan plan = await CreatePlanWithTemplateAsync(
            [CreateItem("1", "photo.jpg")],
            ExistingFilePolicy.Rename,
            @"C:\Imports\{invalid}.jpg");

        Assert.AreEqual(ImportPlanStatus.InvalidTarget, plan.Items[0].Status);
        Assert.IsNotNull(plan.Items[0].Diagnostic);
    }

    [TestMethod]
    public async Task CreatePlanAsync_UsesExpandedEnvironmentPathInPlan()
    {
        const string variableName = "MYMEDIAIMPORT_IMPORT_PLAN_TEST_ROOT";
        string? previousValue = Environment.GetEnvironmentVariable(variableName);
        string rootPath = Path.Combine(Path.GetTempPath(), "MyMediaImportPlanTest");
        try
        {
            Environment.SetEnvironmentVariable(variableName, rootPath);
            string template =
                $"%{variableName}%{Path.DirectorySeparatorChar}{{original}}.{{ext}}";

            ImportPlan plan = await CreatePlanWithTemplateAsync(
                [CreateItem("1", "photo.jpg")],
                ExistingFilePolicy.Skip,
                template);

            Assert.AreEqual(Path.Combine(rootPath, "photo.jpg"), plan.Items[0].TargetPath);
            Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [TestMethod]
    public async Task CreatePlanAsync_ProducesSameResultForSameInputAndSnapshot()
    {
        MediaItem[] items = new[]
        {
            CreateItem("1", "photo.jpg"),
            CreateItem("2", "photo.jpg"),
            CreateItem("3", "clip.mp4")
        };
        string[] existing = new[] { @"C:\Imports\2026\photo.jpg" };

        ImportPlan first = await CreatePlanAsync(items, ExistingFilePolicy.Rename, existing);
        ImportPlan second = await CreatePlanAsync(items, ExistingFilePolicy.Rename, existing);

        CollectionAssert.AreEqual(first.Items.ToArray(), second.Items.ToArray());
    }

    [TestMethod]
    public async Task CreatePlanAsync_OverwriteAvoidsConflictsWithinSelection()
    {
        MediaItem[] items = new[]
        {
            CreateItem("1", "photo.jpg"),
            CreateItem("2", "photo.jpg"),
            CreateItem("3", "photo.jpg")
        };

        ImportPlan plan = await CreatePlanAsync(items, ExistingFilePolicy.Overwrite);

        CollectionAssert.AreEqual(
            new[]
            {
                @"C:\Imports\2026\photo.jpg",
                @"C:\Imports\2026\photo_02.jpg",
                @"C:\Imports\2026\photo_03.jpg"
            },
            plan.Items.Select(item => item.TargetPath).ToArray());
        Assert.HasCount(3, plan.Items.Select(item => item.TargetPath).Distinct().ToArray());
    }

    [TestMethod]
    public async Task CreatePlanAsync_ReportsConflictWhenTemplateCannotRename()
    {
        ImportPlan plan = await CreatePlanWithTemplateAsync(
            [CreateItem("1", "photo.jpg"), CreateItem("2", "photo.jpg")],
            ExistingFilePolicy.Rename,
            @"C:\Imports\{original}.{ext}");

        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        Assert.AreEqual(ImportPlanStatus.Conflict, plan.Items[1].Status);
    }

    private static async ValueTask<ImportPlan> CreatePlanAsync(
        IReadOnlyList<MediaItem> items,
        ExistingFilePolicy policy,
        params string[] existingPaths) =>
        await CreatePlanWithTemplateAsync(items, policy, TargetTemplate, existingPaths);

    private static async ValueTask<ImportPlan> CreatePlanWithTemplateAsync(
        IReadOnlyList<MediaItem> items,
        ExistingFilePolicy policy,
        string template,
        params string[] existingPaths)
    {
        ImportPlanner planner = new(new SnapshotTargetFileLookup(existingPaths));
        return await planner.CreatePlanAsync(items, new PathTemplate(template), policy);
    }

    private static MediaItem CreateItem(string id, string name) =>
        new(
            id,
            name,
            10,
            CaptureTimestamp.FromKnownTime(
                new DateTimeOffset(2026, 8, 22, 17, 30, 23, TimeSpan.FromHours(2))),
            Path.GetExtension(name).Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                ? MediaKind.Video
                : MediaKind.Photo,
            null);

    private sealed class SnapshotTargetFileLookup(IEnumerable<string> paths)
        : ITargetFileLookup
    {
        private readonly HashSet<string> _paths = new(paths, StringComparer.OrdinalIgnoreCase);

        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_paths.Contains(fullPath));
        }
    }

    private sealed class ContentTargetFileLookup(IReadOnlyDictionary<string, byte[]> files)
        : ITargetFileContent
    {
        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(files.ContainsKey(fullPath));

        public ValueTask<long> GetSizeAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((long)files[fullPath].Length);

        public ValueTask<Stream> OpenReadAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(files[fullPath], writable: false));
    }

    private sealed class ContentMediaSource(IReadOnlyDictionary<string, byte[]> files)
        : IMediaSource
    {
        public async IAsyncEnumerable<MediaItem> GetMediaItemsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<Stream> OpenReadAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(files[mediaItem.Id], writable: false));

        public ValueTask<Stream?> OpenThumbnailAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream?>(null);
    }

    public TestContext TestContext { get; set; }
}
