namespace MyMediaImport.App;

internal static class WindowPlacementValidator
{
    private const double MinimumVisibleTitleBarWidth = 64d;
    private const double MinimumVisibleTitleBarHeight = 16d;
    private const double AssumedTitleBarHeight = 32d;

    public static bool TryGetVisibleGeometry(
        WindowPlacementSettings? settings,
        IReadOnlyList<WindowWorkArea> workAreas,
        out WindowGeometry geometry)
    {
        geometry = default;
        if (settings?.Left is not { } left
            || settings.Top is not { } top
            || settings.Width is not { } width
            || settings.Height is not { } height)
        {
            return false;
        }

        WindowGeometry candidate = new(left, top, width, height);
        if (!HasValidValues(candidate) || workAreas.Count == 0)
        {
            return false;
        }

        double titleBarHeight = Math.Min(AssumedTitleBarHeight, candidate.Height);
        foreach (WindowWorkArea workArea in workAreas)
        {
            if (!HasValidValues(new(
                    workArea.Left,
                    workArea.Top,
                    workArea.Width,
                    workArea.Height)))
            {
                continue;
            }

            double intersectionWidth = Math.Max(
                0d,
                Math.Min(candidate.Left + candidate.Width, workArea.Left + workArea.Width)
                    - Math.Max(candidate.Left, workArea.Left));
            double intersectionHeight = Math.Max(
                0d,
                Math.Min(candidate.Top + titleBarHeight, workArea.Top + workArea.Height)
                    - Math.Max(candidate.Top, workArea.Top));
            double requiredWidth = Math.Min(MinimumVisibleTitleBarWidth, candidate.Width);
            double requiredHeight = Math.Min(MinimumVisibleTitleBarHeight, titleBarHeight);
            if (intersectionWidth >= requiredWidth && intersectionHeight >= requiredHeight)
            {
                geometry = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool HasValidValues(WindowGeometry geometry) =>
        IsFinite(geometry.Left)
        && IsFinite(geometry.Top)
        && IsFinite(geometry.Width)
        && IsFinite(geometry.Height)
        && geometry.Width > 0d
        && geometry.Height > 0d;

    private static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}
