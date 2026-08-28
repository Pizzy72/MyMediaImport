using System.Globalization;

namespace MyMediaImport.Core;

public sealed record LocalCaptureDateRange
{
    public LocalCaptureDateRange(DateOnly? from, DateOnly? to)
    {
        if (from is { } lower && to is { } upper && lower > upper)
        {
            throw new ArgumentException("The start date must not be after the end date.");
        }

        From = from;
        To = to;
    }

    public DateOnly? From { get; }

    public DateOnly? To { get; }

    public static LocalCaptureDateRange All { get; } = new(null, null);

    public bool Contains(DateOnly date) =>
        (From is null || date >= From.Value) &&
        (To is null || date <= To.Value);

    public static DateOnly ParseIsoDate(string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            throw new FormatException(
                $"Invalid date '{value}'. Use the culture-independent format yyyy-MM-dd.");
        }

        return date;
    }
}
