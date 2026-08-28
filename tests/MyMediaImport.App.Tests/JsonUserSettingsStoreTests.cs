using System.IO;
using System.Text.Json;

namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class JsonUserSettingsStoreTests
{
    [TestMethod]
    public void Load_WhenFileIsMissing_ReturnsDefaults()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            JsonUserSettingsStore store = new(settingsPath);

            AppUserSettings settings = store.Load();

            Assert.AreEqual(AppUserSettings.CreateDefault(), settings);
        });
    }

    [TestMethod]
    public void SaveAndLoad_PersistsSettings()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            JsonUserSettingsStore store = new(settingsPath);
            AppUserSettings expected = AppUserSettings.CreateDefault() with
            {
                Theme = "Dark",
                TextSize = "Large"
            };

            store.Save(expected);
            AppUserSettings actual = store.Load();

            Assert.AreEqual(expected, actual);
        });
    }

    [TestMethod]
    public void Load_WithUnknownJsonFields_IgnoresThem()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            Assert.IsNotNull(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                settingsPath,
                """
                {
                  "Version": 1,
                  "Theme": "Dark",
                  "FutureOption": { "Enabled": true }
                }
                """);
            JsonUserSettingsStore store = new(settingsPath);

            AppUserSettings settings = store.Load();

            Assert.AreEqual("Dark", settings.Theme);
            Assert.AreEqual("Medium", settings.TextSize);
        });
    }

    [TestMethod]
    public void Load_WhenJsonIsCorrupt_ReturnsDefaults()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            Assert.IsNotNull(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{ definitely not valid JSON");
            JsonUserSettingsStore store = new(settingsPath);

            AppUserSettings settings = store.Load();

            Assert.AreEqual(AppUserSettings.CreateDefault(), settings);
        });
    }

    [TestMethod]
    public void Save_WritesCurrentVersionField()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            JsonUserSettingsStore store = new(settingsPath);
            AppUserSettings oldSettings = AppUserSettings.CreateDefault() with
            {
                Version = 0
            };

            store.Save(oldSettings);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            int version = document.RootElement.GetProperty("Version").GetInt32();

            Assert.AreEqual(AppUserSettings.CurrentVersion, version);
        });
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsEssentialSettings()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            JsonUserSettingsStore store = new(settingsPath);
            AppUserSettings expected = new()
            {
                Theme = "Light",
                TextSize = "Small",
                TimeSelection = "FromTo",
                LastDays = 21,
                FromDate = new DateOnly(2026, 7, 1),
                ToDate = new DateOnly(2026, 8, 24),
                CaptureTimeZone = "FixedOffset",
                FixedUtcOffset = "-05:00",
                ExtensionFilter = "JPG,HEIC,MOV,MP4",
                TargetTemplate = @"%TEMP%\{captureUtc:yyyy}\{original}.{ext}",
                ExistingFilePolicy = "Rename",
                DeviceId = "stable-device-id",
                MainWindow = new()
                {
                    Left = -1200d,
                    Top = 80d,
                    Width = 1180d,
                    Height = 760d,
                    WindowState = "Maximized"
                }
            };

            store.Save(expected);
            AppUserSettings actual = store.Load();

            Assert.AreEqual(expected, actual);
        });
    }

    [TestMethod]
    public void Load_WhenVersionIsUnsupported_ReturnsDefaults()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            Assert.IsNotNull(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{ \"Version\": 999, \"Theme\": \"Dark\" }");
            JsonUserSettingsStore store = new(settingsPath);

            AppUserSettings settings = store.Load();

            Assert.AreEqual(AppUserSettings.CreateDefault(), settings);
        });
    }

    [TestMethod]
    public void Load_WhenVersionIsMissing_ReturnsDefaults()
    {
        RunWithTemporarySettingsPath(settingsPath =>
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            Assert.IsNotNull(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{ \"Theme\": \"Dark\" }");
            JsonUserSettingsStore store = new(settingsPath);

            AppUserSettings settings = store.Load();

            Assert.AreEqual(AppUserSettings.CreateDefault(), settings);
        });
    }

    private static void RunWithTemporarySettingsPath(Action<string> test)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "MyMediaImport.App.Tests",
            Guid.NewGuid().ToString("N"));
        string settingsPath = Path.Combine(directory, "settings.json");
        try
        {
            test(settingsPath);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
