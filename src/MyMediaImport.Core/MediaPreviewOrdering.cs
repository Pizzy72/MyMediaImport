namespace MyMediaImport.Core;

public static class MediaPreviewOrdering
{
    public static IReadOnlyList<MediaItem> NewestFirst(IEnumerable<MediaItem> mediaItems)
    {
        ArgumentNullException.ThrowIfNull(mediaItems);

        return mediaItems
            .Select((mediaItem, index) => new IndexedMediaItem(
                mediaItem ?? throw new ArgumentException(
                    "The media collection must not contain null items.",
                    nameof(mediaItems)),
                index))
            .OrderBy(item => item.MediaItem.CaptureTime is null)
            .ThenByDescending(item => item.MediaItem.CaptureTime?.ResolvedTime?.UtcDateTime)
            .ThenByDescending(item => item.MediaItem.CaptureTime?.LocalTime)
            .ThenBy(item => item.Index)
            .Select(item => item.MediaItem)
            .ToArray();
    }

    private sealed record IndexedMediaItem(MediaItem MediaItem, int Index);
}
