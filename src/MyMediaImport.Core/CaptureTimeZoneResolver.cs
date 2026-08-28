namespace MyMediaImport.Core;

public sealed class CaptureTimeZoneResolver
{
    private readonly TimeZoneInfo _localTimeZone;

    public CaptureTimeZoneResolver()
        : this(TimeZoneInfo.Local)
    {
    }

    public CaptureTimeZoneResolver(TimeZoneInfo localTimeZone)
    {
        ArgumentNullException.ThrowIfNull(localTimeZone);
        _localTimeZone = localTimeZone;
    }

    public string LocalTimeZoneId => _localTimeZone.Id;

    public string LocalTimeZoneDisplayName => _localTimeZone.DisplayName;

    public DateTimeOffset Resolve(
        DateTime localCaptureTime,
        CaptureTimeZoneSpec timeZoneSpec)
    {
        ArgumentNullException.ThrowIfNull(timeZoneSpec);
        DateTime wallClockTime = DateTime.SpecifyKind(localCaptureTime, DateTimeKind.Unspecified);

        if (timeZoneSpec.Kind == CaptureTimeZoneKind.FixedOffset)
        {
            return new DateTimeOffset(wallClockTime, timeZoneSpec.FixedOffset!.Value);
        }

        if (_localTimeZone.IsInvalidTime(wallClockTime))
        {
            throw new CaptureTimeResolutionException(
                $"Local capture time '{wallClockTime:O}' is invalid in timezone '{_localTimeZone.Id}'.");
        }

        if (_localTimeZone.IsAmbiguousTime(wallClockTime))
        {
            throw new CaptureTimeResolutionException(
                $"Local capture time '{wallClockTime:O}' is ambiguous in timezone '{_localTimeZone.Id}'.");
        }

        return new DateTimeOffset(wallClockTime, _localTimeZone.GetUtcOffset(wallClockTime));
    }

    public CaptureTimestamp Resolve(
        CaptureTimestamp captureTimestamp,
        CaptureTimeZoneSpec timeZoneSpec)
    {
        ArgumentNullException.ThrowIfNull(captureTimestamp);
        return captureTimestamp.IsResolved
            ? captureTimestamp
            : CaptureTimestamp.FromKnownTime(Resolve(captureTimestamp.LocalTime, timeZoneSpec));
    }
}
