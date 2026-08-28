namespace MyMediaImport.Core;

public interface ITargetFileLookup
{
    ValueTask<bool> ExistsAsync(
        string fullPath,
        CancellationToken cancellationToken = default);
}
