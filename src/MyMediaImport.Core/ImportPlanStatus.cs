namespace MyMediaImport.Core;

public enum ImportPlanStatus
{
    Ready,
    ExistingFile,
    AlreadyImported,
    Renamed,
    WillOverwrite,
    Conflict,
    InvalidTarget,
    MissingCaptureTime
}
