using System.Text.Json.Serialization;

namespace MyMediaImport.App;

internal sealed record AppUserSettings
{
    public const int CurrentVersion = 1;

    [JsonRequired]
    public int Version { get; init; } = CurrentVersion;

    public string Theme { get; init; } = nameof(AppTheme.System);

    public string TextSize { get; init; } = nameof(AppFontSize.Medium);

    public string TimeSelection { get; init; } = nameof(TimeSelectionMode.Today);

    public int LastDays { get; init; } = 7;

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string CaptureTimeZone { get; init; } = nameof(TimeZoneSelectionMode.Local);

    public string FixedUtcOffset { get; init; } =
        AppUserSettingsDefaults.CreateFixedUtcOffset();

    public string ExtensionFilter { get; init; } = "JPG,HEIC,MOV";

    public string TargetTemplate { get; init; } =
        AppUserSettingsDefaults.CreateTargetTemplate();

    public string ExistingFilePolicy { get; init; } =
        nameof(MyMediaImport.Core.ExistingFilePolicy.Skip);

    public string? DeviceId { get; init; }

    public WindowPlacementSettings? MainWindow { get; init; } = new();

    public static AppUserSettings CreateDefault() => new();
}
