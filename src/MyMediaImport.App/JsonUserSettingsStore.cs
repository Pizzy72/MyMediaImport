using System.IO;
using System.Security;
using System.Text.Json;

namespace MyMediaImport.App;

internal sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private const string ApplicationDirectoryName = "MyMediaImport";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public JsonUserSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public static JsonUserSettingsStore CreateForCurrentUser()
    {
        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string settingsPath = Path.Combine(
            localApplicationData,
            ApplicationDirectoryName,
            SettingsFileName);
        return new(settingsPath);
    }

    public AppUserSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return AppUserSettings.CreateDefault();
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            AppUserSettings? settings = JsonSerializer.Deserialize<AppUserSettings>(
                json,
                _serializerOptions);
            return settings is { Version: AppUserSettings.CurrentVersion }
                ? settings
                : AppUserSettings.CreateDefault();
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or SecurityException
            or JsonException
            or NotSupportedException)
        {
            return AppUserSettings.CreateDefault();
        }
    }

    public void Save(AppUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        AppUserSettings versionedSettings = settings with
        {
            Version = AppUserSettings.CurrentVersion
        };
        string json = JsonSerializer.Serialize(versionedSettings, _serializerOptions);
        string? settingsDirectory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        string temporaryPath = _settingsPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or SecurityException)
        {
        }
    }
}
