namespace MyMediaImport.Core;

public sealed class LocalCaptureDateSelectionRule(DateOnly captureDate) : IMediaSelectionRule
{
    private readonly LocalCaptureDateRangeSelectionRule _rangeRule = new(
        new LocalCaptureDateRange(captureDate, captureDate));

    public DateOnly CaptureDate { get; } = captureDate;

    public bool IsMatch(MediaItem mediaItem)
    {
        return _rangeRule.IsMatch(mediaItem);
    }
}
