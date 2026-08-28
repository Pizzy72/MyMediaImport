namespace MyMediaImport.Core;

public interface ITargetFileContent : ITargetFileLookup
{
    ValueTask<long> GetSizeAsync(
        string fullPath,
        CancellationToken cancellationToken = default);

    ValueTask<Stream> OpenReadAsync(
        string fullPath,
        CancellationToken cancellationToken = default);
}
