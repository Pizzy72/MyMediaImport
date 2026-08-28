using System.IO;

namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class AppUserSettingsDefaultsTests
{
    [TestMethod]
    public void CreateDefault_UsesWindowsPicturesDirectoryForTargetTemplate()
    {
        string picturesDirectory = AppUserSettingsDefaults.GetPicturesDirectory();

        AppUserSettings settings = AppUserSettings.CreateDefault();

        StringAssert.StartsWith(settings.TargetTemplate, picturesDirectory);
        StringAssert.Contains(
            settings.TargetTemplate,
            Path.Combine("MyMediaImport", "{capture:yyyy}", "{capture:MM}"));
        StringAssert.EndsWith(
            settings.TargetTemplate,
            "{capture:yyyy-MM-dd_HHmmss}{collision:_00}.{ext}");
    }

    [TestMethod]
    public void CreateFixedUtcOffset_UsesLocalOffsetAtGivenInstant()
    {
        DateTimeOffset instant = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        TimeSpan expectedOffset = TimeZoneInfo.Local.GetUtcOffset(instant);

        string offset = AppUserSettingsDefaults.CreateFixedUtcOffset(instant);

        Assert.AreEqual(
            AppUserSettingsDefaults.FormatUtcOffset(expectedOffset),
            offset);
    }

    [TestMethod]
    [DataRow(2, 30, "+02:30")]
    [DataRow(0, 0, "+00:00")]
    [DataRow(-5, -30, "-05:30")]
    public void FormatUtcOffset_UsesCliCompatibleFormat(
        int hours,
        int minutes,
        string expected)
    {
        TimeSpan offset = new(hours, minutes, 0);

        string result = AppUserSettingsDefaults.FormatUtcOffset(offset);

        Assert.AreEqual(expected, result);
    }
}
