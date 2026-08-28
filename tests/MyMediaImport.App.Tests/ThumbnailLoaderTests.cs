using MyMediaImport.Core;

namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class ThumbnailLoaderTests
{
    private static readonly byte[] TestPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [TestMethod]
    public async Task LoadAsyncUsesOriginalPhotoWhenThumbnailIsUnavailable()
    {
        TestMediaSource mediaSource = new(thumbnailBytes: null, sourceBytes: TestPng);
        ThumbnailLoader loader = new(mediaSource);
        MediaItem mediaItem = CreateMediaItem(MediaKind.Photo);

        ThumbnailLoadResult result = await loader.LoadAsync(mediaItem);

        Assert.AreEqual(ThumbnailLoadState.Loaded, result.State);
        Assert.IsNotNull(result.Image);
        Assert.IsGreaterThan(0, result.Image.PixelWidth);
        Assert.AreEqual(1, mediaSource.OpenThumbnailCount);
        Assert.AreEqual(1, mediaSource.OpenReadCount);
    }

    [TestMethod]
    public async Task LoadAsyncDoesNotOpenOriginalVideoWhenThumbnailIsUnavailable()
    {
        TestMediaSource mediaSource = new(thumbnailBytes: null, sourceBytes: TestPng);
        ThumbnailLoader loader = new(mediaSource);
        MediaItem mediaItem = CreateMediaItem(MediaKind.Video);

        ThumbnailLoadResult result = await loader.LoadAsync(mediaItem);

        Assert.AreEqual(ThumbnailLoadState.Unavailable, result.State);
        Assert.AreEqual(1, mediaSource.OpenThumbnailCount);
        Assert.AreEqual(0, mediaSource.OpenReadCount);
    }

    private static MediaItem CreateMediaItem(MediaKind mediaKind) =>
        new("item-1", "example.jpg", TestPng.Length, null, mediaKind, "image/jpeg");

    private sealed class TestMediaSource : IMediaSource
    {
        private readonly byte[]? _thumbnailBytes;
        private readonly byte[] _sourceBytes;

        public TestMediaSource(byte[]? thumbnailBytes, byte[] sourceBytes)
        {
            _thumbnailBytes = thumbnailBytes;
            _sourceBytes = sourceBytes;
        }

        public int OpenReadCount { get; private set; }

        public int OpenThumbnailCount { get; private set; }

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
            OpenReadCount++;
            return ValueTask.FromResult<Stream>(new MemoryStream(_sourceBytes, writable: false));
        }

        public ValueTask<Stream?> OpenThumbnailAsync(
            MediaItem mediaItem,
            CancellationToken cancellationToken = default)
        {
            OpenThumbnailCount++;
            Stream? stream = _thumbnailBytes is null
                ? null
                : new MemoryStream(_thumbnailBytes, writable: false);
            return ValueTask.FromResult(stream);
        }
    }
}
