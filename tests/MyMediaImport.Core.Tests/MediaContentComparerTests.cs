namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaContentComparerTests
{
    [TestMethod]
    public async Task IsIdenticalAsync_DifferentKnownSize_DoesNotOpenSource()
    {
        ComparisonMediaSource source = new([1, 2, 3]);
        MediaContentComparer comparer = new(new ComparisonTargetContent([1, 2]));
        MediaItem item = CreateItem(3);

        bool identical = await comparer.IsIdenticalAsync(
            source, item, @"C:\Import\photo.jpg", TestContext.CancellationToken);

        Assert.IsFalse(identical);
        Assert.AreEqual(0, source.OpenCount);
    }

    [TestMethod]
    public async Task IsIdenticalAsync_SameSizeAndContent_ReturnsTrue()
    {
        byte[] content = [1, 2, 3];
        MediaContentComparer comparer = new(new ComparisonTargetContent(content));

        bool identical = await comparer.IsIdenticalAsync(
            new ComparisonMediaSource(content),
            CreateItem(content.Length),
            @"C:\Import\photo.jpg", TestContext.CancellationToken);

        Assert.IsTrue(identical);
    }

    [TestMethod]
    public async Task IsIdenticalAsync_CancellationDuringComparison_IsPropagated()
    {
        CancellationTokenSource cancellation = new();
        ComparisonTargetContent target = new(
            [1, 2, 3], () => new CancellingReadStream([1, 2, 3], cancellation));
        MediaContentComparer comparer = new(target);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await comparer.IsIdenticalAsync(
                new ComparisonMediaSource([1, 2, 3]),
                CreateItem(3),
                @"C:\Import\photo.jpg",
                cancellation.Token));
    }

    private static MediaItem CreateItem(long size) =>
        new("1", "photo.jpg", size, null, MediaKind.Photo, "image/jpeg");

    private sealed class ComparisonMediaSource(byte[] content) : IMediaSource
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
            OpenCount++;
            return ValueTask.FromResult<Stream>(new MemoryStream(content, writable: false));
        }

        public ValueTask<Stream?> OpenThumbnailAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream?>(null);
    }

    private sealed class ComparisonTargetContent(
        byte[] content,
        Func<Stream>? openStream = null) : ITargetFileContent
    {
        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(true);

        public ValueTask<long> GetSizeAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((long)content.Length);

        public ValueTask<Stream> OpenReadAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(openStream?.Invoke() ?? new MemoryStream(content, writable: false));
    }

    private sealed class CancellingReadStream(
        byte[] content,
        CancellationTokenSource cancellation) : MemoryStream(content)
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(0);
        }
    }

    public TestContext TestContext { get; set; }
}
