namespace MyMediaImport.Core;

public sealed class BatchImportResult
{
    public BatchImportResult(IReadOnlyList<ImportResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        Results = results;
    }

    public IReadOnlyList<ImportResult> Results { get; }

    public int ImportedCount => Results.Count(result =>
        result.Status == ImportResultStatus.Succeeded);

    public int SkippedCount => Results.Count(result =>
        result.Status is ImportResultStatus.Skipped or ImportResultStatus.AlreadyImported);

    public int FailedCount => Results.Count(result =>
        result.Status is ImportResultStatus.Failed or ImportResultStatus.Cancelled);

    public bool IsSuccess => FailedCount == 0;
}
