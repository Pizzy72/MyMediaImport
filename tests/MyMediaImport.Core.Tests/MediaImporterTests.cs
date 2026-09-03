namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaImporterTests
{
    private const string TargetPath = @"C:\Import\photo.jpg";
    private static readonly byte[] SourceBytes = [1, 2, 3, 4, 5];

    [TestMethod]
    [DataRow(2)]
    [DataRow(-5)]
    public async Task ImportAsync_UsesResolvedCaptureTimeForCreationTime(int offsetHours)
    {
        MemoryImportFileSystem fileSystem = new();
        TestMediaSource source = new(() => new MemoryStream(SourceBytes));
        MediaImporter importer = new(fileSystem);
        ImportRequest request = CreateRequest(ExistingFilePolicy.Skip);
        CaptureTimeZoneResolver resolver = new();
        CaptureTimestamp captureTime = resolver.Resolve(
            request.MediaItem.CaptureTime!,
            CaptureTimeZoneSpec.FromFixedOffset(TimeSpan.FromHours(offsetHours)));
        request = request with { MediaItem = request.MediaItem.WithCaptureTime(captureTime) };

        ImportResult result = await importer.ImportAsync(source, request, TestContext.CancellationToken);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.AreEqual(captureTime.ResolvedTime, fileSystem.PublishedCreationTime);
        CollectionAssert.AreEqual(SourceBytes, fileSystem.Files[TargetPath]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ImportAsync_WithoutResolvedCaptureTime_DoesNotInventCreationTime(bool missing)
    {
        MemoryImportFileSystem fileSystem = new();
        TestMediaSource source = new(() => new MemoryStream(SourceBytes));
        MediaImporter importer = new(fileSystem);
        ImportRequest request = CreateRequest(ExistingFilePolicy.Skip);
        if (missing)
        {
            request = request with { MediaItem = request.MediaItem.WithCaptureTime(null) };
        }

        ImportResult result = await importer.ImportAsync(source, request, TestContext.CancellationToken);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.IsNull(fileSystem.PublishedCreationTime);
    }

    [TestMethod]
    public async Task ImportAsync_TransfersAndPublishesFile()
    {
        MemoryImportFileSystem fileSystem = new();
        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Skip);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.AreEqual(SourceBytes.Length, result.TransferredBytes);
        CollectionAssert.AreEqual(SourceBytes, fileSystem.Files[TargetPath]);
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath + ".partial"));
    }

    [TestMethod]
    public async Task ImportAsync_ExactExpectedSize_Succeeds()
    {
        ImportResult result = await ImportAsync(new MemoryImportFileSystem(), ExistingFilePolicy.Skip);

        Assert.AreEqual<long?>(5, result.ExpectedSize);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImportAsync_ReportsTransferredBytesWhileCopying()
    {
        MemoryImportFileSystem fileSystem = new();
        TestMediaSource source = new(() => new MemoryStream(SourceBytes));
        MediaImporter importer = new(fileSystem);
        List<MediaImportProgress> reportedProgress = [];
        IProgress<MediaImportProgress> progress =
            new RecordingProgress<MediaImportProgress>(reportedProgress.Add);

        ImportResult result = await importer.ImportAsync(
            source,
            CreateRequest(ExistingFilePolicy.Skip),
            TestContext.CancellationToken,
            progress);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotEmpty(reportedProgress);
        long transferredBytes = SourceBytes.Length;
        Assert.AreEqual(transferredBytes, reportedProgress.Last().TransferredBytes);
        long? expectedBytes = SourceBytes.Length;
        Assert.AreEqual(expectedBytes, reportedProgress.Last().ExpectedBytes);
    }

    [TestMethod]
    public async Task ImportAsync_SizeMismatch_FailsAndRemovesPartialFile()
    {
        MemoryImportFileSystem fileSystem = new();
        ImportResult result = await ImportAsync(
            fileSystem,
            ExistingFilePolicy.Skip,
            expectedSize: SourceBytes.Length + 1);

        Assert.AreEqual(ImportResultStatus.Failed, result.Status);
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath));
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath + ".partial"));
        Assert.Contains("Size verification failed", result.Diagnostic!);
    }

    [TestMethod]
    public async Task ImportAsync_Cancellation_RemovesPartialFile()
    {
        CancellationTokenSource cancellation = new();
        TestMediaSource source = new(
            () => new CancelAfterFirstReadStream(SourceBytes, cancellation));
        MemoryImportFileSystem fileSystem = new();
        MediaImporter importer = new(fileSystem);

        ImportResult result = await importer.ImportAsync(
            source,
            CreateRequest(ExistingFilePolicy.Skip),
            cancellation.Token);

        Assert.AreEqual(ImportResultStatus.Cancelled, result.Status);
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath));
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath + ".partial"));
    }

    [TestMethod]
    public async Task ImportAsync_SourceOpenFails_RemovesPreparedPartialFile()
    {
        MemoryImportFileSystem fileSystem = new();
        TestMediaSource source = new(
            () => throw new IOException("Source could not be opened."));
        MediaImporter importer = new(fileSystem);

        ImportResult result = await importer.ImportAsync(
            source, CreateRequest(ExistingFilePolicy.Skip), TestContext.CancellationToken);

        Assert.AreEqual(ImportResultStatus.Failed, result.Status);
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath));
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath + ".partial"));
        Assert.Contains("Source could not be opened", result.Diagnostic!);
    }

    [TestMethod]
    public async Task ImportAsync_ExistingFileWithSkip_DoesNotOpenSource()
    {
        byte[] original = new byte[] { 9, 9, 9 };
        MemoryImportFileSystem fileSystem = new((TargetPath, original));
        TestMediaSource source = new(() => new MemoryStream(SourceBytes));
        MediaImporter importer = new(fileSystem);

        ImportResult result = await importer.ImportAsync(
            source, CreateRequest(ExistingFilePolicy.Skip), TestContext.CancellationToken);

        Assert.AreEqual(ImportResultStatus.Skipped, result.Status);
        Assert.AreEqual(0, source.OpenCount);
        CollectionAssert.AreEqual(original, fileSystem.Files[TargetPath]);
    }

    [TestMethod]
    public async Task ImportAsync_ExistingFileWithRename_UsesCollisionSuffix()
    {
        MemoryImportFileSystem fileSystem = new((TargetPath, [9]));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.AreEqual(@"C:\Import\photo_02.jpg", result.TargetPath);
        CollectionAssert.AreEqual(SourceBytes, fileSystem.Files[result.TargetPath]);
        CollectionAssert.AreEqual(new byte[] { 9 }, fileSystem.Files[TargetPath]);
    }

    [TestMethod]
    public async Task ImportAsync_ExistingIdenticalFileWithRename_IsAlreadyImported()
    {
        MemoryImportFileSystem fileSystem = new((TargetPath, SourceBytes));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportResultStatus.AlreadyImported, result.Status);
        Assert.AreEqual(TargetPath, result.TargetPath);
        Assert.HasCount(1, fileSystem.Files);
    }

    [TestMethod]
    public async Task ImportAsync_SameSizeDifferentContentWithRename_IsRealCollision()
    {
        MemoryImportFileSystem fileSystem = new((TargetPath, [9, 9, 9, 9, 9]));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.AreEqual(@"C:\Import\photo_02.jpg", result.TargetPath);
    }

    [TestMethod]
    public async Task ImportAsync_IdenticalSecondCollision_IsAlreadyImportedWithoutThirdFile()
    {
        string secondPath = @"C:\Import\photo_02.jpg";
        MemoryImportFileSystem fileSystem = new(
            (TargetPath, [9]),
            (secondPath, SourceBytes));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportResultStatus.AlreadyImported, result.Status);
        Assert.AreEqual(secondPath, result.TargetPath);
        Assert.IsFalse(fileSystem.Files.ContainsKey(@"C:\Import\photo_03.jpg"));
    }

    [TestMethod]
    public async Task ImportAsync_SearchesAllCollisionsBeforeUsingNextFreeName()
    {
        MemoryImportFileSystem fileSystem = new(
            (TargetPath, [9]),
            (@"C:\Import\photo_02.jpg", [8]),
            (@"C:\Import\photo_03.jpg", [7]));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Rename);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        Assert.AreEqual(@"C:\Import\photo_04.jpg", result.TargetPath);
    }

    [TestMethod]
    public async Task ImportAsync_ExistingFileWithOverwrite_ReplacesAfterSuccess()
    {
        MemoryImportFileSystem fileSystem = new((TargetPath, [9, 9]));

        ImportResult result = await ImportAsync(fileSystem, ExistingFilePolicy.Overwrite);

        Assert.AreEqual(ImportResultStatus.Succeeded, result.Status);
        CollectionAssert.AreEqual(SourceBytes, fileSystem.Files[TargetPath]);
    }

    [TestMethod]
    public async Task ImportAsync_FailedOverwrite_PreservesExistingValidFile()
    {
        byte[] original = new byte[] { 9, 9, 9 };
        MemoryImportFileSystem fileSystem = new((TargetPath, original));

        ImportResult result = await ImportAsync(
            fileSystem,
            ExistingFilePolicy.Overwrite,
            expectedSize: SourceBytes.Length + 1);

        Assert.AreEqual(ImportResultStatus.Failed, result.Status);
        CollectionAssert.AreEqual(original, fileSystem.Files[TargetPath]);
        Assert.IsFalse(fileSystem.Files.ContainsKey(TargetPath + ".partial"));
    }

    private static async ValueTask<ImportResult> ImportAsync(
        MemoryImportFileSystem fileSystem,
        ExistingFilePolicy policy,
        long? expectedSize = null)
    {
        TestMediaSource source = new(() => new MemoryStream(SourceBytes));
        MediaImporter importer = new(fileSystem);
        return await importer.ImportAsync(
            source,
            CreateRequest(policy, expectedSize ?? SourceBytes.Length));
    }

    private static ImportRequest CreateRequest(
        ExistingFilePolicy policy,
        long? expectedSize = null) =>
        new(
            new MediaItem(
                "object-1",
                "photo.jpg",
                expectedSize ?? SourceBytes.Length,
                CaptureTimestamp.FromLocalTime(new DateTime(2026, 8, 23, 14, 3, 33)),
                MediaKind.Photo,
                "image/jpeg"),
            TargetPath,
            policy);

    private sealed class TestMediaSource(Func<Stream> openStream) : IMediaSource
    {
        public int OpenCount { get; private set; }

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
            cancellationToken.ThrowIfCancellationRequested();
            OpenCount++;
            return ValueTask.FromResult(openStream());
        }

        public ValueTask<Stream?> OpenThumbnailAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream?>(null);
    }

    private sealed class RecordingProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class MemoryImportFileSystem(params (string Path, byte[] Content)[] files)
        : IImportFileSystem
    {
        internal DateTimeOffset? PublishedCreationTime { get; private set; }

        internal Dictionary<string, byte[]> Files { get; } =
            files.ToDictionary(file => file.Path, file => file.Content, StringComparer.OrdinalIgnoreCase);

        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Files.ContainsKey(fullPath));
        }

        public ValueTask<long> GetSizeAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((long)Files[fullPath].Length);

        public ValueTask<Stream> OpenReadAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(Files[fullPath], writable: false));

        public ValueTask<Stream> OpenPartialWriteAsync(
            string partialPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stream stream = new CommittingMemoryStream(bytes => Files[partialPath] = bytes);
            return ValueTask.FromResult(stream);
        }

        public ValueTask DeleteIfExistsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Files.Remove(path);
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishAsync(
            string partialPath,
            string targetPath,
            bool overwrite,
            DateTimeOffset? creationTime,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!overwrite && Files.ContainsKey(targetPath))
            {
                throw new IOException("Target already exists.");
            }

            Files[targetPath] = Files[partialPath];
            PublishedCreationTime = creationTime;
            Files.Remove(partialPath);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CommittingMemoryStream(Action<byte[]> commit) : MemoryStream
    {
        private bool _committed;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed)
            {
                commit(ToArray());
                _committed = true;
            }

            base.Dispose(disposing);
        }
    }

    private sealed class CancelAfterFirstReadStream(
        byte[] content,
        CancellationTokenSource cancellation) : MemoryStream(content)
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int bytesRead = base.Read(buffer.Span);
            cancellation.Cancel();
            return ValueTask.FromResult(bytesRead);
        }
    }

    public TestContext TestContext { get; set; }
}
