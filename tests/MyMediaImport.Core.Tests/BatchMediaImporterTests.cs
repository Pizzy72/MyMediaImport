namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class BatchMediaImporterTests
{
    [TestMethod]
    public async Task ImportAsync_ContinuesAfterFileFailureAndSummarizesResults()
    {
        MediaItem[] items = new[]
        {
            CreateItem("one", 3),
            CreateItem("bad", 4),
            CreateItem("three", 1)
        };
        BatchTestMediaSource source = new(new Dictionary<string, byte[]>
        {
            ["one"] = [1, 2, 3],
            ["bad"] = [1, 2],
            ["three"] = [1]
        });
        BatchMemoryFileSystem fileSystem = new();
        BatchMediaImporter batchImporter = new(new MediaImporter(fileSystem));
        ImportPlan plan = new([.. items.Select(item => new ImportPlanItem(
            item,
            $@"C:\Import\{item.Name}",
            ImportPlanStatus.Ready))]);
        List<BatchImportProgress> reportedProgress = [];
        IProgress<BatchImportProgress> progress = new RecordingProgress<BatchImportProgress>(
            reportedProgress.Add);

        BatchImportResult result = await batchImporter.ImportAsync(
            source,
            plan,
            ExistingFilePolicy.Skip,
            progress,
            TestContext.CancellationToken);

        Assert.AreEqual(2, result.ImportedCount);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.AreEqual(1, result.FailedCount);
        Assert.IsFalse(result.IsSuccess);
        CollectionAssert.AreEqual(
            new[]
            {
                ImportResultStatus.Succeeded,
                ImportResultStatus.Failed,
                ImportResultStatus.Succeeded
            },
            result.Results.Select(item => item.Status).ToArray());
        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            reportedProgress
                .Where(item => item.Result is not null)
                .Select(item => item.CompletedCount)
                .ToArray());
        Assert.IsTrue(reportedProgress.All(item => item.TotalCount == 3));
        Assert.IsTrue(reportedProgress.Any(item => item.Result is null));
        Assert.AreEqual(
            6,
            reportedProgress.Last().TransferredBytes);
    }

    [TestMethod]
    public async Task ImportAsync_ExistingFileProducesExpectedSkipSummary()
    {
        MediaItem item = CreateItem("one", 3);
        string target = @"C:\Import\one.jpg";
        BatchTestMediaSource source = new(new Dictionary<string, byte[]>
        {
            ["one"] = [1, 2, 3]
        });
        BatchMemoryFileSystem fileSystem = new((target, [9]));
        BatchMediaImporter batchImporter = new(new MediaImporter(fileSystem));
        ImportPlan plan = new(
            [new ImportPlanItem(item, target, ImportPlanStatus.ExistingFile)]);

        BatchImportResult result = await batchImporter.ImportAsync(
            source, plan, ExistingFilePolicy.Skip, cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(0, result.ImportedCount);
        Assert.AreEqual(1, result.SkippedCount);
        Assert.AreEqual(0, result.FailedCount);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImportAsync_CancelledBeforeAlreadyImportedItem_StopsImmediately()
    {
        MediaItem first = CreateItem("one", 3);
        MediaItem second = CreateItem("two", 3);
        BatchTestMediaSource source = new(new Dictionary<string, byte[]>());
        BatchMemoryFileSystem fileSystem = new();
        BatchMediaImporter batchImporter = new(new MediaImporter(fileSystem));
        ImportPlan plan = new(
        [
            new ImportPlanItem(first, @"C:\Import\one.jpg", ImportPlanStatus.AlreadyImported),
            new ImportPlanItem(second, @"C:\Import\two.jpg", ImportPlanStatus.AlreadyImported)
        ]);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        BatchImportResult result = await batchImporter.ImportAsync(
            source, plan, ExistingFilePolicy.Rename, cancellationToken: cancellation.Token);

        Assert.HasCount(1, result.Results);
        Assert.AreEqual(ImportResultStatus.Cancelled, result.Results[0].Status);
    }

    private static MediaItem CreateItem(string id, long size) =>
        new(
            id,
            $"{id}.jpg",
            size,
            CaptureTimestamp.FromLocalTime(new DateTime(2026, 8, 23, 12, 0, 0)),
            MediaKind.Photo,
            "image/jpeg");

    private sealed class BatchTestMediaSource(IReadOnlyDictionary<string, byte[]> content)
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
            CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream(content[mediaItem.Id]);
            return ValueTask.FromResult(stream);
        }

        public ValueTask<Stream?> OpenThumbnailAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream?>(null);
    }

    private sealed class BatchMemoryFileSystem(
        params (string Path, byte[] Content)[] initialFiles) : IImportFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = initialFiles.ToDictionary(
            file => file.Path, file => file.Content, StringComparer.OrdinalIgnoreCase);

        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_files.ContainsKey(fullPath));

        public ValueTask<long> GetSizeAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((long)_files[fullPath].Length);

        public ValueTask<Stream> OpenReadAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(_files[fullPath], writable: false));

        public ValueTask<Stream> OpenPartialWriteAsync(
            string partialPath,
            CancellationToken cancellationToken = default)
        {
            Stream stream = new CommitStream(bytes => _files[partialPath] = bytes);
            return ValueTask.FromResult(stream);
        }

        public ValueTask DeleteIfExistsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            _files.Remove(path);
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishAsync(
            string partialPath,
            string targetPath,
            bool overwrite,
            DateTimeOffset? creationTime,
            CancellationToken cancellationToken = default)
        {
            _files[targetPath] = _files[partialPath];
            _files.Remove(partialPath);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CommitStream(Action<byte[]> commit) : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                commit(ToArray());
            }

            base.Dispose(disposing);
        }
    }

    private sealed class RecordingProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    public TestContext TestContext { get; set; }
}
