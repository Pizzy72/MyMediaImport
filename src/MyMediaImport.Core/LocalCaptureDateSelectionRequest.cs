namespace MyMediaImport.Core;

public enum LocalCaptureDatePreset
{
    Today,
    Yesterday,
    All
}

public sealed record LocalCaptureDateSelectionRequest(
    LocalCaptureDatePreset? Preset = null,
    int? LastDays = null,
    DateOnly? From = null,
    DateOnly? To = null)
{
    public LocalCaptureDateRange Resolve(DateOnly currentLocalDate)
    {
        int modeCount = (Preset is null ? 0 : 1) +
                        (LastDays is null ? 0 : 1) +
                        (From is null && To is null ? 0 : 1);
        if (modeCount != 1)
        {
            throw new ArgumentException(
                "Use exactly one time selection: --today, --yesterday, --last, " +
                "--all, or --from/--to.");
        }

        if (LastDays is { } days)
        {
            if (days < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(LastDays), "The number of days must be at least 1.");
            }

            return new LocalCaptureDateRange(currentLocalDate.AddDays(-(days - 1)), currentLocalDate);
        }

        if (Preset is { } preset)
        {
            return preset switch
            {
                LocalCaptureDatePreset.Today =>
                    new LocalCaptureDateRange(currentLocalDate, currentLocalDate),
                LocalCaptureDatePreset.Yesterday =>
                    new LocalCaptureDateRange(currentLocalDate.AddDays(-1), currentLocalDate.AddDays(-1)),
                LocalCaptureDatePreset.All => LocalCaptureDateRange.All,
                _ => throw new ArgumentOutOfRangeException(nameof(Preset))
            };
        }

        return new LocalCaptureDateRange(From, To);
    }
}
