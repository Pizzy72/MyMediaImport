namespace MyMediaImport.Core;

public enum ImportResultStatus
{
    Succeeded,
    Skipped,
    AlreadyImported,
    Failed,
    Cancelled
}
