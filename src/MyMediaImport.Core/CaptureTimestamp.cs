namespace MyMediaImport.Core;

public sealed record CaptureTimestamp
{
    private CaptureTimestamp(DateTime localTime, TimeSpan? offset)
    {
        LocalTime = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        Offset = offset;
    }

    public DateTime LocalTime { get; }

    public TimeSpan? Offset { get; }

    public bool IsResolved => Offset is not null;

    public DateTimeOffset? ResolvedTime =>
        Offset is { } offset ? new DateTimeOffset(LocalTime, offset) : null;

    public static CaptureTimestamp FromLocalTime(DateTime localTime) =>
        new(localTime, null);

    public static CaptureTimestamp FromKnownTime(DateTimeOffset captureTime) =>
        new(captureTime.DateTime, captureTime.Offset);
}
