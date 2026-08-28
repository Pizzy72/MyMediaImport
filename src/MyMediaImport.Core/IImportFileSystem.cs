namespace MyMediaImport.Core;

public interface IImportFileSystem : ITargetFileContent
{
    ValueTask<Stream> OpenPartialWriteAsync(
        string partialPath,
        CancellationToken cancellationToken = default);

    ValueTask DeleteIfExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    ValueTask PublishAsync(
        string partialPath,
        string targetPath,
        bool overwrite,
        CancellationToken cancellationToken = default);
}
