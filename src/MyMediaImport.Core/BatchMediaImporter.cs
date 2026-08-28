namespace MyMediaImport.Core;

public sealed class BatchMediaImporter(MediaImporter mediaImporter)
{
    public async ValueTask<BatchImportResult> ImportAsync(
        IMediaSource mediaSource,
        ImportPlan importPlan,
        ExistingFilePolicy existingFilePolicy,
        IProgress<BatchImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        ArgumentNullException.ThrowIfNull(importPlan);

        List<ImportResult> results = new(importPlan.Items.Count);
        for (int index = 0; index < importPlan.Items.Count; index++)
        {
            ImportPlanItem planItem = importPlan.Items[index];
            ImportResult result;

            if (cancellationToken.IsCancellationRequested)
            {
                result = new ImportResult(
                    planItem.MediaItem,
                    planItem.TargetPath,
                    planItem.MediaItem.Size,
                    0,
                    ImportResultStatus.Cancelled,
                    "The import was cancelled.");
            }
            else if (planItem.Status == ImportPlanStatus.AlreadyImported)
            {
                result = new ImportResult(
                    planItem.MediaItem,
                    planItem.TargetPath,
                    planItem.MediaItem.Size,
                    0,
                    ImportResultStatus.AlreadyImported,
                    planItem.Diagnostic);
            }
            else if (planItem.Status is ImportPlanStatus.Conflict or
                ImportPlanStatus.InvalidTarget or
                ImportPlanStatus.MissingCaptureTime)
            {
                result = new ImportResult(
                    planItem.MediaItem,
                    planItem.TargetPath,
                    planItem.MediaItem.Size,
                    0,
                    ImportResultStatus.Failed,
                    planItem.Diagnostic ?? "The planned import item is not importable.");
            }
            else
            {
                ExistingFilePolicy executionPolicy = existingFilePolicy == ExistingFilePolicy.Overwrite
                    ? ExistingFilePolicy.Overwrite
                    : ExistingFilePolicy.Skip;
                result = await mediaImporter.ImportAsync(
                    mediaSource,
                    new ImportRequest(planItem.MediaItem, planItem.TargetPath, executionPolicy),
                    cancellationToken);
            }

            results.Add(result);
            progress?.Report(new BatchImportProgress(
                index + 1, importPlan.Items.Count, result));

            if (result.Status == ImportResultStatus.Cancelled)
            {
                break;
            }
        }

        return new BatchImportResult(results);
    }
}
