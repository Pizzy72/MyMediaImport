namespace MyMediaImport.Core;

public interface IImportFileSystem : ITargetFileContent
{
    ValueTask<Stream> OpenPartialWriteAsync(
        string partialPath,
        CancellationToken cancellationToken = default);

    ValueTask DeleteIfExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the completed file and sets its creation time when supplied.
    /// A null creation time leaves the file-system timestamp unchanged.
    /// </summary>
    ValueTask PublishAsync(
        string partialPath,
        string targetPath,
        bool overwrite,
        DateTimeOffset? creationTime,
        CancellationToken cancellationToken = default);
}
