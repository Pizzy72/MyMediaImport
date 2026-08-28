namespace MyMediaImport.Core;

public sealed record ImportPlanItem(
    MediaItem MediaItem,
    string TargetPath,
    ImportPlanStatus Status,
    string? Diagnostic = null);
