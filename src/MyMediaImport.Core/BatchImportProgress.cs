namespace MyMediaImport.Core;

public sealed record BatchImportProgress(
    int CompletedCount,
    int TotalCount,
    ImportResult Result);
