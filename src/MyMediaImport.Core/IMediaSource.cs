namespace MyMediaImport.Core;

public interface IMediaSource
{
    IAsyncEnumerable<MediaItem> GetMediaItemsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<Stream> OpenReadAsync(
        MediaItem mediaItem,
        CancellationToken cancellationToken = default);

    ValueTask<Stream?> OpenThumbnailAsync(
        MediaItem mediaItem,
        CancellationToken cancellationToken = default);
}
