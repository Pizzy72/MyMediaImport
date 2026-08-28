namespace MyMediaImport.App;

internal sealed record WindowPlacementSettings
{
    public double? Left { get; init; }

    public double? Top { get; init; }

    public double? Width { get; init; }

    public double? Height { get; init; }

    public string WindowState { get; init; } = "Normal";
}
