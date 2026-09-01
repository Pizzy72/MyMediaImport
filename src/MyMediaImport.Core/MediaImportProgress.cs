namespace MyMediaImport.Core;

public sealed record MediaImportProgress(
    MediaItem MediaItem,
    long TransferredBytes,
    long? ExpectedBytes);
