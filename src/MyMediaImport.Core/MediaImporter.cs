namespace MyMediaImport.Core;

public sealed class MediaImporter
{
    private const int CopyBufferSize = 128 * 1024;

    private readonly IImportFileSystem _fileSystem;

    public MediaImporter(IImportFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public async ValueTask<ImportResult> ImportAsync(
        IMediaSource mediaSource,
        ImportRequest request,
        CancellationToken cancellationToken = default,
        IProgress<MediaImportProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        ArgumentNullException.ThrowIfNull(request);

        string targetPath;
        try
        {
            targetPath = ValidateTargetPath(request.TargetPath);
            bool existingTarget = await _fileSystem.ExistsAsync(targetPath, cancellationToken);
            if (existingTarget && request.ExistingFilePolicy == ExistingFilePolicy.Skip)
            {
                return new ImportResult(
                    request.MediaItem,
                    targetPath,
                    request.MediaItem.Size,
                    0,
                    ImportResultStatus.Skipped,
                    "The target file already exists and was skipped.");
            }

            if (request.ExistingFilePolicy == ExistingFilePolicy.Rename)
            {
                (string? TargetPath, bool IsIdentical) = await FindAvailableTargetPathAsync(
                    mediaSource, request.MediaItem, targetPath, cancellationToken);
                targetPath = TargetPath;
                if (IsIdentical)
                {
                    return new ImportResult(
                        request.MediaItem,
                        targetPath,
                        request.MediaItem.Size,
                        0,
                        ImportResultStatus.AlreadyImported,
                        "The existing target file is identical to the source.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            return Cancelled(request, request.TargetPath, 0);
        }
        catch (Exception exception)
        {
            return Failed(request, request.TargetPath, 0, exception.Message);
        }

        string partialPath = targetPath + ".partial";
        long transferredBytes = 0;

        try
        {
            await _fileSystem.DeleteIfExistsAsync(partialPath, cancellationToken);
            await using (Stream destination = await _fileSystem.OpenPartialWriteAsync(
                             partialPath, cancellationToken))
            await using (Stream source = await mediaSource.OpenReadAsync(
                             request.MediaItem, cancellationToken))
            {
                byte[] buffer = new byte[CopyBufferSize];
                while (true)
                {
                    int bytesRead = await source.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await destination.WriteAsync(
                        buffer.AsMemory(0, bytesRead), cancellationToken);
                    transferredBytes = checked(transferredBytes + bytesRead);
                    progress?.Report(new MediaImportProgress(
                        request.MediaItem,
                        transferredBytes,
                        request.MediaItem.Size));
                }

                await destination.FlushAsync(cancellationToken);
            }

            if (request.MediaItem.Size is { } expectedSize &&
                transferredBytes != expectedSize)
            {
                await DeletePartialBestEffortAsync(partialPath);
                return Failed(
                    request,
                    targetPath,
                    transferredBytes,
                    $"Size verification failed: expected {expectedSize} bytes, " +
                    $"but transferred {transferredBytes} bytes.");
            }

            await _fileSystem.PublishAsync(
                partialPath,
                targetPath,
                request.ExistingFilePolicy == ExistingFilePolicy.Overwrite,
                cancellationToken);

            return new ImportResult(
                request.MediaItem,
                targetPath,
                request.MediaItem.Size,
                transferredBytes,
                ImportResultStatus.Succeeded);
        }
        catch (OperationCanceledException)
        {
            await DeletePartialBestEffortAsync(partialPath);
            return Cancelled(request, targetPath, transferredBytes);
        }
        catch (Exception exception)
        {
            await DeletePartialBestEffortAsync(partialPath);
            return Failed(request, targetPath, transferredBytes, exception.Message);
        }
    }

    private async ValueTask<(string TargetPath, bool IsIdentical)> FindAvailableTargetPathAsync(
        IMediaSource mediaSource,
        MediaItem mediaItem,
        string requestedTargetPath,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(requestedTargetPath)!;
        string extension = Path.GetExtension(requestedTargetPath);
        string baseName = Path.GetFileNameWithoutExtension(requestedTargetPath);
        MediaContentComparer comparer = new(_fileSystem);

        for (int collisionNumber = 1; ; collisionNumber++)
        {
            string candidate = collisionNumber == 1
                ? requestedTargetPath
                : Path.Combine(
                    directory,
                    $"{baseName}_{collisionNumber:00}{extension}");
            if (!await _fileSystem.ExistsAsync(candidate, cancellationToken))
            {
                return (candidate, false);
            }

            if (await comparer.IsIdenticalAsync(
                    mediaSource, mediaItem, candidate, cancellationToken))
            {
                return (candidate, true);
            }
        }
    }

    private async ValueTask DeletePartialBestEffortAsync(string partialPath)
    {
        try
        {
            await _fileSystem.DeleteIfExistsAsync(partialPath, CancellationToken.None);
        }
        catch
        {
            // The original transfer or publish error remains the primary diagnostic.
        }
    }

    private static string ValidateTargetPath(string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        if (!Path.IsPathFullyQualified(targetPath))
        {
            throw new ArgumentException("The import target path must be fully qualified.", nameof(targetPath));
        }

        return Path.GetFullPath(targetPath);
    }

    private static ImportResult Failed(
        ImportRequest request,
        string targetPath,
        long transferredBytes,
        string diagnostic) =>
        new(
            request.MediaItem,
            targetPath,
            request.MediaItem.Size,
            transferredBytes,
            ImportResultStatus.Failed,
            diagnostic);

    private static ImportResult Cancelled(
        ImportRequest request,
        string targetPath,
        long transferredBytes) =>
        new(
            request.MediaItem,
            targetPath,
            request.MediaItem.Size,
            transferredBytes,
            ImportResultStatus.Cancelled,
            "The import was cancelled.");
}
