namespace MyMediaImport.Core;

public sealed record ImportResult(
    MediaItem MediaItem,
    string TargetPath,
    long? ExpectedSize,
    long TransferredBytes,
    ImportResultStatus Status,
    string? Diagnostic = null)
{
    public bool IsSuccess => Status is
        ImportResultStatus.Succeeded or
        ImportResultStatus.Skipped or
        ImportResultStatus.AlreadyImported;
}
