using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace MyMediaImport.App;

public sealed class AppThemeService
{
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmUseImmersiveDarkMode = 20;
    private const string ThemeDictionaryPrefix = "Themes/";

    public void Apply(AppTheme theme)
    {
        AppTheme resolvedTheme = ResolveTheme(theme);
        ResourceDictionary resources = Application.Current.Resources;
        ResourceDictionary? currentTheme = resources.MergedDictionaries.FirstOrDefault(
            dictionary => dictionary.Source?.OriginalString.StartsWith(
                ThemeDictionaryPrefix,
                StringComparison.OrdinalIgnoreCase) == true);
        ResourceDictionary replacement = new()
        {
            Source = new Uri($"Themes/{resolvedTheme}.xaml", UriKind.Relative)
        };

        if (currentTheme is null)
        {
            resources.MergedDictionaries.Insert(0, replacement);
        }
        else
        {
            int index = resources.MergedDictionaries.IndexOf(currentTheme);
            resources.MergedDictionaries[index] = replacement;
        }

        foreach (Window window in Application.Current.Windows)
        {
            ApplyTitleBar(window, resolvedTheme);
        }
    }

    public void ApplyTitleBar(Window window, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(window);
        nint windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        int useDarkMode = ResolveTheme(theme) == AppTheme.Dark ? 1 : 0;
        int result = DwmSetWindowAttribute(
            windowHandle,
            DwmUseImmersiveDarkMode,
            ref useDarkMode,
            sizeof(int));
        if (result != 0)
        {
            DwmSetWindowAttribute(
                windowHandle,
                DwmUseImmersiveDarkModeBefore20H1,
                ref useDarkMode,
                sizeof(int));
        }
    }

    public void ApplyFontSize(AppFontSize fontSize)
    {
        double points = fontSize switch
        {
            AppFontSize.Small => 10d,
            AppFontSize.Medium => 12d,
            AppFontSize.Large => 14d,
            _ => throw new ArgumentOutOfRangeException(nameof(fontSize))
        };
        double deviceIndependentSize = points * 96d / 72d;
        Application.Current.Resources["BaseFontSize"] = deviceIndependentSize;
        Application.Current.Resources["HeadingFontSize"] = deviceIndependentSize * 1.25d;
    }

    private static AppTheme GetSystemTheme()
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath);
        object? value = key?.GetValue("AppsUseLightTheme");
        return value is int appsUseLightTheme && appsUseLightTheme == 0
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    private static AppTheme ResolveTheme(AppTheme theme) =>
        theme == AppTheme.System ? GetSystemTheme() : theme;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
