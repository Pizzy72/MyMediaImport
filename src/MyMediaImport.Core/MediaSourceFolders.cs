namespace MyMediaImport.Core;

public static class MediaSourceFolders
{
    public static async ValueTask<IMediaSource> OpenAsync(
        IMediaSource source,
        IReadOnlyList<string> pathSegments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pathSegments);
        cancellationToken.ThrowIfCancellationRequested();
        if (pathSegments.Count == 0)
        {
            return source;
        }

        if (source is not IFolderMediaSource folders)
        {
            throw new InvalidOperationException("This media source does not support folder selection.");
        }

        MediaSourceFolder? current = null;
        foreach (string segment in pathSegments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(segment);
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<MediaSourceFolder> children = await folders.GetFoldersAsync(
                current?.Id, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            MediaSourceFolder[] matches = children
                .Where(folder => string.Equals(folder.Name, segment, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The saved source folder '{string.Join(" / ", pathSegments)}' " +
                    "is missing or ambiguous. Choose a folder again or select All folders.");
            }

            current = matches[0];
        }

        return folders.OpenFolder(current!);
    }
}
