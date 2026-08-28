namespace MyMediaImport.Core;

public sealed class ImportBatchPlanner
{
    private readonly ImportPlanner _importPlanner;
    private readonly CaptureTimeZoneResolver _captureTimeZoneResolver;

    public ImportBatchPlanner(
        ImportPlanner importPlanner,
        CaptureTimeZoneResolver captureTimeZoneResolver)
    {
        ArgumentNullException.ThrowIfNull(importPlanner);
        ArgumentNullException.ThrowIfNull(captureTimeZoneResolver);
        _importPlanner = importPlanner;
        _captureTimeZoneResolver = captureTimeZoneResolver;
    }

    public async ValueTask<ImportPlan> CreatePlanAsync(
        IReadOnlyList<MediaItem> mediaItems,
        CaptureTimeZoneSpec captureTimeZone,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken = default)
        => await CreatePlanCoreAsync(
            mediaItems, null, captureTimeZone, pathTemplate,
            existingFilePolicy, cancellationToken);

    public async ValueTask<ImportPlan> CreatePlanAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IMediaSource mediaSource,
        CaptureTimeZoneSpec captureTimeZone,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        return await CreatePlanCoreAsync(
            mediaItems, mediaSource, captureTimeZone, pathTemplate,
            existingFilePolicy, cancellationToken);
    }

    private async ValueTask<ImportPlan> CreatePlanCoreAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IMediaSource? mediaSource,
        CaptureTimeZoneSpec captureTimeZone,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediaItems);
        ArgumentNullException.ThrowIfNull(captureTimeZone);
        ArgumentNullException.ThrowIfNull(pathTemplate);

        List<MediaItem> resolvedItems = new(mediaItems.Count);
        foreach (MediaItem mediaItem in mediaItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureTimestamp? captureTime = mediaItem.CaptureTime is { } timestamp
                ? _captureTimeZoneResolver.Resolve(timestamp, captureTimeZone)
                : null;
            resolvedItems.Add(mediaItem.WithCaptureTime(captureTime));
        }

        return mediaSource is null
            ? await _importPlanner.CreatePlanAsync(
                resolvedItems, pathTemplate, existingFilePolicy, cancellationToken)
            : await _importPlanner.CreatePlanAsync(
                resolvedItems, mediaSource, pathTemplate, existingFilePolicy, cancellationToken);
    }
}
