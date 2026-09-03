using MyMediaImport.Core;

namespace MyMediaImport.App;

public sealed class TargetPathPreviewItemViewModel
{
    public TargetPathPreviewItemViewModel(ImportPlanItem planItem)
    {
        ArgumentNullException.ThrowIfNull(planItem);
        SourceName = planItem.MediaItem.Name;
        ExpectedSize = planItem.MediaItem.Size;
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
        CanRevealInExplorer = planItem.Status is
            ImportPlanStatus.AlreadyImported or
            ImportPlanStatus.ExistingFile or
            ImportPlanStatus.WillOverwrite;
        IsConflictOrError = planItem.Status is
            ImportPlanStatus.ExistingFile or
            ImportPlanStatus.Conflict or
            ImportPlanStatus.InvalidTarget or
            ImportPlanStatus.MissingCaptureTime;
    }

    public TargetPathPreviewItemViewModel(ImportResult importResult)
    {
        ArgumentNullException.ThrowIfNull(importResult);
        SourceName = importResult.MediaItem.Name;
        ExpectedSize = importResult.ExpectedSize;
        TargetPath = importResult.TargetPath;
        Status = FormatStatus(importResult.Status);
        Diagnostic = importResult.Diagnostic;
        IsImportReady = importResult.Status == ImportResultStatus.Succeeded;
        IsAlreadyImported = importResult.Status == ImportResultStatus.AlreadyImported;
        CanRevealInExplorer = importResult.Status is
            ImportResultStatus.Succeeded or
            ImportResultStatus.Skipped or
            ImportResultStatus.AlreadyImported;
        IsConflictOrError = importResult.Status is
            ImportResultStatus.Failed or
            ImportResultStatus.Cancelled;
    }

    public string SourceName { get; }

    public long? ExpectedSize { get; }

    public string TargetPath { get; }

    public string Status { get; }

    public string? Diagnostic { get; }

    public bool IsImportReady { get; }

    public bool IsAlreadyImported { get; }

    public bool CanRevealInExplorer { get; }

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

    private static string FormatStatus(ImportResultStatus status) => status switch
    {
        ImportResultStatus.Succeeded => "Imported",
        ImportResultStatus.Skipped => "Skipped",
        ImportResultStatus.AlreadyImported => "Already imported",
        ImportResultStatus.Failed => "Failed",
        ImportResultStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown import result status.")
    };
}
