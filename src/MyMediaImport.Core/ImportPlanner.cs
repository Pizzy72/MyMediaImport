namespace MyMediaImport.Core;

public sealed class ImportPlanner
{
    private static readonly StringComparer TargetPathComparer =
        StringComparer.OrdinalIgnoreCase;

    private readonly ITargetFileLookup _targetFileLookup;
    private readonly MediaContentComparer? _mediaContentComparer;

    public ImportPlanner(ITargetFileLookup targetFileLookup)
    {
        ArgumentNullException.ThrowIfNull(targetFileLookup);
        _targetFileLookup = targetFileLookup;
        _mediaContentComparer = targetFileLookup is ITargetFileContent content
            ? new MediaContentComparer(content)
            : null;
    }

    public async ValueTask<ImportPlan> CreatePlanAsync(
        IReadOnlyList<MediaItem> mediaItems,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken = default)
        => await CreatePlanCoreAsync(
            mediaItems, null, pathTemplate, existingFilePolicy, cancellationToken);

    public async ValueTask<ImportPlan> CreatePlanAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IMediaSource mediaSource,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        return await CreatePlanCoreAsync(
            mediaItems, mediaSource, pathTemplate, existingFilePolicy, cancellationToken);
    }

    private async ValueTask<ImportPlan> CreatePlanCoreAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IMediaSource? mediaSource,
        PathTemplate pathTemplate,
        ExistingFilePolicy existingFilePolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediaItems);
        ArgumentNullException.ThrowIfNull(pathTemplate);

        List<ImportPlanItem> planItems = new(mediaItems.Count);
        HashSet<string> reservedTargetPaths = new(TargetPathComparer);

        foreach (MediaItem mediaItem in mediaItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImportPlanItem planItem = existingFilePolicy switch
            {
                ExistingFilePolicy.Skip => await PlanSkipAsync(
                    mediaItem, pathTemplate, reservedTargetPaths, cancellationToken),
                ExistingFilePolicy.Rename => await PlanRenameAsync(
                    mediaItem, mediaSource, pathTemplate, reservedTargetPaths, cancellationToken),
                ExistingFilePolicy.Overwrite => await PlanOverwriteAsync(
                    mediaItem, pathTemplate, reservedTargetPaths, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(existingFilePolicy), existingFilePolicy, "Unknown existing-file policy.")
            };

            planItems.Add(planItem);
        }

        return new ImportPlan(planItems);
    }

    private async ValueTask<ImportPlanItem> PlanSkipAsync(
        MediaItem mediaItem,
        PathTemplate pathTemplate,
        HashSet<string> reservedTargetPaths,
        CancellationToken cancellationToken)
    {
        (string? TargetPath, ImportPlanItem? Error) = TryRenderTarget(mediaItem, pathTemplate, 1);
        if (Error is not null)
        {
            return Error;
        }

        string targetPath = TargetPath!;
        if (reservedTargetPaths.Contains(targetPath))
        {
            return Conflict(mediaItem, targetPath,
                "Another selected media item already uses this target path.");
        }

        if (await _targetFileLookup.ExistsAsync(targetPath, cancellationToken))
        {
            return new ImportPlanItem(
                mediaItem,
                targetPath,
                ImportPlanStatus.ExistingFile,
                "The target file already exists and will be skipped.");
        }

        reservedTargetPaths.Add(targetPath);
        return new ImportPlanItem(mediaItem, targetPath, ImportPlanStatus.Ready);
    }

    private async ValueTask<ImportPlanItem> PlanRenameAsync(
        MediaItem mediaItem,
        IMediaSource? mediaSource,
        PathTemplate pathTemplate,
        HashSet<string> reservedTargetPaths,
        CancellationToken cancellationToken)
    {
        HashSet<string> candidates = new(TargetPathComparer);

        for (int collisionNumber = 1; ; collisionNumber++)
        {
            (string? TargetPath, ImportPlanItem? Error) = TryRenderTarget(mediaItem, pathTemplate, collisionNumber);
            if (Error is not null)
            {
                return Error;
            }

            string targetPath = TargetPath!;
            if (!candidates.Add(targetPath))
            {
                return Conflict(mediaItem, targetPath,
                    "The path template cannot generate a unique collision name.");
            }

            bool exists = await _targetFileLookup.ExistsAsync(targetPath, cancellationToken);
            if (reservedTargetPaths.Contains(targetPath))
            {
                continue;
            }

            if (exists)
            {
                if (mediaSource is not null && _mediaContentComparer is not null &&
                    await _mediaContentComparer.IsIdenticalAsync(
                        mediaSource, mediaItem, targetPath, cancellationToken))
                {
                    reservedTargetPaths.Add(targetPath);
                    return new ImportPlanItem(
                        mediaItem,
                        targetPath,
                        ImportPlanStatus.AlreadyImported,
                        "The existing target file is identical to the source.");
                }

                continue;
            }

            reservedTargetPaths.Add(targetPath);
            return collisionNumber == 1
                ? new ImportPlanItem(mediaItem, targetPath, ImportPlanStatus.Ready)
                : new ImportPlanItem(
                    mediaItem,
                    targetPath,
                    ImportPlanStatus.Renamed,
                    $"The target name was changed to resolve collision {collisionNumber:00}.");
        }
    }

    private async ValueTask<ImportPlanItem> PlanOverwriteAsync(
        MediaItem mediaItem,
        PathTemplate pathTemplate,
        HashSet<string> reservedTargetPaths,
        CancellationToken cancellationToken)
    {
        (string? TargetPath, ImportPlanItem? Error) rendered = TryRenderTarget(mediaItem, pathTemplate, 1);
        if (rendered.Error is not null)
        {
            return rendered.Error;
        }

        string targetPath = rendered.TargetPath!;
        if (!reservedTargetPaths.Contains(targetPath))
        {
            reservedTargetPaths.Add(targetPath);
            bool exists = await _targetFileLookup.ExistsAsync(targetPath, cancellationToken);
            return exists
                ? new ImportPlanItem(
                    mediaItem,
                    targetPath,
                    ImportPlanStatus.WillOverwrite,
                    "The existing target file will be replaced.")
                : new ImportPlanItem(mediaItem, targetPath, ImportPlanStatus.Ready);
        }

        HashSet<string> candidates = new(TargetPathComparer) { targetPath };
        for (int collisionNumber = 2; ; collisionNumber++)
        {
            rendered = TryRenderTarget(mediaItem, pathTemplate, collisionNumber);
            if (rendered.Error is not null)
            {
                return rendered.Error;
            }

            targetPath = rendered.TargetPath!;
            if (!candidates.Add(targetPath))
            {
                return Conflict(mediaItem, targetPath,
                    "The path template cannot generate a unique name for selected media items.");
            }

            if (reservedTargetPaths.Contains(targetPath))
            {
                continue;
            }

            reservedTargetPaths.Add(targetPath);
            bool exists = await _targetFileLookup.ExistsAsync(targetPath, cancellationToken);
            string diagnostic = exists
                ? "The target was renamed to avoid an in-plan conflict and the existing file will be replaced."
                : "The target was renamed to avoid a conflict within the current selection.";
            return new ImportPlanItem(
                mediaItem, targetPath, ImportPlanStatus.Renamed, diagnostic);
        }
    }

    private static (string? TargetPath, ImportPlanItem? Error) TryRenderTarget(
        MediaItem mediaItem,
        PathTemplate pathTemplate,
        int collisionNumber)
    {
        try
        {
            string targetPath = pathTemplate.Render(mediaItem, collisionNumber);
            if (!Path.IsPathFullyQualified(targetPath))
            {
                return (null, InvalidTarget(
                    mediaItem, targetPath, "The target path must be fully qualified."));
            }

            targetPath = Path.GetFullPath(targetPath);
            if (ContainsInvalidPathSegment(targetPath))
            {
                return (null, InvalidTarget(
                    mediaItem, targetPath, "The target path contains invalid characters."));
            }

            return (targetPath, null);
        }
        catch (MissingCaptureTimeException exception)
        {
            return (null, new ImportPlanItem(
                mediaItem,
                string.Empty,
                ImportPlanStatus.MissingCaptureTime,
                exception.Message));
        }
        catch (UnresolvedCaptureTimeException exception)
        {
            return (null, InvalidTarget(mediaItem, string.Empty, exception.Message));
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or NotSupportedException)
        {
            return (null, InvalidTarget(mediaItem, string.Empty, exception.Message));
        }
    }

    private static bool ContainsInvalidPathSegment(string fullPath)
    {
        int rootLength = Path.GetPathRoot(fullPath)?.Length ?? 0;
        string[] segments = fullPath[rootLength..]
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries);
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        return segments.Any(segment => segment.IndexOfAny(invalidCharacters) >= 0);
    }

    private static ImportPlanItem Conflict(
        MediaItem mediaItem,
        string targetPath,
        string diagnostic) =>
        new(mediaItem, targetPath, ImportPlanStatus.Conflict, diagnostic);

    private static ImportPlanItem InvalidTarget(
        MediaItem mediaItem,
        string targetPath,
        string diagnostic) =>
        new(mediaItem, targetPath, ImportPlanStatus.InvalidTarget, diagnostic);
}
