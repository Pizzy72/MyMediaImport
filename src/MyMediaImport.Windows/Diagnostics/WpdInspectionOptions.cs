namespace MyMediaImport.Windows.Diagnostics;

public sealed record WpdInspectionOptions
{
    public int ObjectLimit { get; init; } = 20;

    public int MaximumDepth { get; init; } = 4;

    public bool VerboseResources { get; init; }

    public IReadOnlyList<string> Extensions { get; init; } = [];
}
