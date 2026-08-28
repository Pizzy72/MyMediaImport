using MyMediaImport.Core;

namespace MyMediaImport.Windows;

public sealed class LocalImportFileSystem : IImportFileSystem
{
    public ValueTask<bool> ExistsAsync(
        string fullPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(File.Exists(fullPath));
    }

    public ValueTask<long> GetSizeAsync(
        string fullPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new FileInfo(fullPath).Length);
    }

    public ValueTask<Stream> OpenReadAsync(
        string fullPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return ValueTask.FromResult(stream);
    }

    public ValueTask<Stream> OpenPartialWriteAsync(
        string partialPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string directory = Path.GetDirectoryName(partialPath)
            ?? throw new ArgumentException("The partial path has no parent directory.", nameof(partialPath));
        Directory.CreateDirectory(directory);

        Stream stream = new FileStream(
            partialPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return ValueTask.FromResult(stream);
    }

    public ValueTask DeleteIfExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask PublishAsync(
        string partialPath,
        string targetPath,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!overwrite)
        {
            File.Move(partialPath, targetPath, overwrite: false);
        }
        else if (File.Exists(targetPath))
        {
            File.Replace(partialPath, targetPath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(partialPath, targetPath, overwrite: false);
        }

        return ValueTask.CompletedTask;
    }
}
