namespace MyMediaImport.Core;

public sealed record BatchImportProgress(
    int CompletedCount,
    int TotalCount,
    MediaItem CurrentItem,
    long CurrentItemTransferredBytes,
    long? CurrentItemExpectedBytes,
    long TransferredBytes,
    ImportResult? Result);
