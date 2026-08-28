using MyMediaImport.Core;

namespace MyMediaImport.App;

public sealed class TargetPathPreviewItemViewModel
{
    public TargetPathPreviewItemViewModel(ImportPlanItem planItem)
    {
        ArgumentNullException.ThrowIfNull(planItem);
        SourceName = planItem.MediaItem.Name;
        TargetPath = string.IsNullOrWhiteSpace(planItem.TargetPath)
            ? "No target path"
            : planItem.TargetPath;
        Status = FormatStatus(planItem.Status);
        Diagnostic = planItem.Diagnostic;
        IsImportReady = planItem.Status is
            ImportPlanStatus.Ready or
            ImportPlanStatus.Renamed or
            ImportPlanStatus.WillOverwrite;
        IsAlreadyImported = planItem.Status == ImportPlanStatus.AlreadyImported;
        IsConflictOrError = planItem.Status is
            ImportPlanStatus.ExistingFile or
            ImportPlanStatus.Conflict or
            ImportPlanStatus.InvalidTarget or
            ImportPlanStatus.MissingCaptureTime;
    }

    public string SourceName { get; }

    public string TargetPath { get; }

    public string Status { get; }

    public string? Diagnostic { get; }

    public bool IsImportReady { get; }

    public bool IsAlreadyImported { get; }

    public bool IsConflictOrError { get; }

    private static string FormatStatus(ImportPlanStatus status) => status switch
    {
        ImportPlanStatus.Ready => "Ready",
        ImportPlanStatus.Renamed => "Ready (renamed)",
        ImportPlanStatus.WillOverwrite => "Ready (overwrite)",
        ImportPlanStatus.ExistingFile => "Existing file (skip)",
        ImportPlanStatus.AlreadyImported => "Already imported",
        ImportPlanStatus.Conflict => "Conflict",
        ImportPlanStatus.InvalidTarget => "Invalid target",
        ImportPlanStatus.MissingCaptureTime => "Missing capture time",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown import plan status.")
    };
}
