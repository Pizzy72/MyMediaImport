using System.Globalization;

namespace MyMediaImport.Core;

public sealed record CaptureTimeZoneSpec
{
    private CaptureTimeZoneSpec(CaptureTimeZoneKind kind, TimeSpan? fixedOffset)
    {
        Kind = kind;
        FixedOffset = fixedOffset;
    }

    public CaptureTimeZoneKind Kind { get; }

    public TimeSpan? FixedOffset { get; }

    public static CaptureTimeZoneSpec Local { get; } =
        new(CaptureTimeZoneKind.Local, null);

    public static CaptureTimeZoneSpec FromFixedOffset(TimeSpan offset)
    {
        if (offset < TimeSpan.FromHours(-14) || offset > TimeSpan.FromHours(14) ||
            offset.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset), "A fixed UTC offset must be a whole minute between -14:00 and +14:00.");
        }

        return new CaptureTimeZoneSpec(CaptureTimeZoneKind.FixedOffset, offset);
    }

    public static CaptureTimeZoneSpec Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            return Local;
        }

        if (value.Length != 6 ||
            (value[0] != '+' && value[0] != '-') ||
            value[3] != ':' ||
            !int.TryParse(value.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int hours) ||
            !int.TryParse(value.AsSpan(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int minutes) ||
            hours > 14 || minutes > 59 || (hours == 14 && minutes != 0))
        {
            throw new FormatException(
                $"Invalid capture timezone '{value}'. Use 'local' or an offset such as +02:00 or -05:00.");
        }

        TimeSpan offset = new(hours, minutes, 0);
        return FromFixedOffset(value[0] == '-' ? -offset : offset);
    }

    public override string ToString()
    {
        if (Kind == CaptureTimeZoneKind.Local)
        {
            return "local";
        }

        TimeSpan offset = FixedOffset!.Value;
        return $"{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration():hh\\:mm}";
    }
}
