namespace MyMediaImport.Core;

/// <summary>Optional folder navigation for a media source. IDs are source-specific, not file paths.</summary>
public interface IFolderMediaSource : IMediaSource
{
    ValueTask<IReadOnlyList<MediaSourceFolder>> GetFoldersAsync(
        string? parentFolderId = null,
        CancellationToken cancellationToken = default);

    IMediaSource OpenFolder(MediaSourceFolder folder);
}
