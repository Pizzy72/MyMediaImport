namespace MyMediaImport.Core;

public sealed class LocalCaptureDateRangeSelectionRule(LocalCaptureDateRange range)
    : IMediaSelectionRule
{
    public LocalCaptureDateRange Range { get; } =
        range ?? throw new ArgumentNullException(nameof(range));

    public bool IsMatch(MediaItem mediaItem)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        return mediaItem.CaptureTime is { } captureTime &&
               Range.Contains(DateOnly.FromDateTime(captureTime.LocalTime));
    }
}
