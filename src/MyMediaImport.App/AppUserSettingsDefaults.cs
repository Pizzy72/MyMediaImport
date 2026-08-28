using System.IO;

namespace MyMediaImport.App;

internal static class AppUserSettingsDefaults
{
    public static string CreateTargetTemplate()
    {
        string picturesDirectory = GetPicturesDirectory();
        return Path.Combine(
            picturesDirectory,
            "MyMediaImport",
            "{capture:yyyy}",
            "{capture:MM}",
            "{capture:yyyy-MM-dd_HHmmss}{collision:_00}.{ext}");
    }

    public static string CreateFixedUtcOffset() =>
        CreateFixedUtcOffset(DateTimeOffset.Now);

    internal static string CreateFixedUtcOffset(DateTimeOffset instant)
    {
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(instant);
        return FormatUtcOffset(offset);
    }

    internal static string FormatUtcOffset(TimeSpan offset)
    {
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        TimeSpan absoluteOffset = offset.Duration();
        int hours = checked((int)absoluteOffset.TotalHours);
        return FormattableString.Invariant(
            $"{sign}{hours:00}:{absoluteOffset.Minutes:00}");
    }

    internal static string GetPicturesDirectory()
    {
        string picturesDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.MyPictures);
        if (!string.IsNullOrWhiteSpace(picturesDirectory))
        {
            return picturesDirectory;
        }

        string userProfile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return Path.Combine(userProfile, "Pictures");
        }

        string desktopDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);
        return string.IsNullOrWhiteSpace(desktopDirectory)
            ? Path.GetTempPath()
            : desktopDirectory;
    }
}
