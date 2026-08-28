using System.Windows;
using System.Windows.Media;

namespace MyMediaImport.App;

internal static class WindowSettingsHelper
{
    public static void Apply(Window window, WindowPlacementSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(window);
        IReadOnlyList<WindowWorkArea> workAreas = GetWorkAreas(window);
        if (!WindowPlacementValidator.TryGetVisibleGeometry(
                settings,
                workAreas,
                out WindowGeometry geometry))
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.WindowState = WindowState.Normal;
            return;
        }

        window.WindowState = WindowState.Normal;
        window.Left = Math.Round(geometry.Left);
        window.Top = Math.Round(geometry.Top);
        window.Width = Math.Round(Math.Max(geometry.Width, window.MinWidth));
        window.Height = Math.Round(Math.Max(geometry.Height, window.MinHeight));
        if (string.Equals(
                settings?.WindowState,
                nameof(WindowState.Maximized),
                StringComparison.OrdinalIgnoreCase))
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    public static WindowPlacementSettings Capture(
        Window window,
        WindowPlacementSettings? previousSettings)
    {
        ArgumentNullException.ThrowIfNull(window);
        bool isMaximized = window.WindowState == WindowState.Maximized;
        Rect bounds = window.WindowState == WindowState.Normal
            ? new(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        WindowGeometry geometry = new(
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height);
        if (!WindowPlacementValidator.HasValidValues(geometry))
        {
            return previousSettings ?? new();
        }

        return new()
        {
            Left = geometry.Left,
            Top = geometry.Top,
            Width = geometry.Width,
            Height = geometry.Height,
            WindowState = isMaximized
                ? nameof(WindowState.Maximized)
                : nameof(WindowState.Normal)
        };
    }

    private static IReadOnlyList<WindowWorkArea> GetWorkAreas(Window window)
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        return System.Windows.Forms.Screen.AllScreens
            .Select(screen => new WindowWorkArea(
                screen.WorkingArea.Left / dpi.DpiScaleX,
                screen.WorkingArea.Top / dpi.DpiScaleY,
                screen.WorkingArea.Width / dpi.DpiScaleX,
                screen.WorkingArea.Height / dpi.DpiScaleY))
            .ToArray();
    }
}
