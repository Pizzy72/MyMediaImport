namespace MyMediaImport.Core;

public sealed class MediaContentComparer(ITargetFileContent targetFileContent)
{
    private const int ComparisonBufferSize = 128 * 1024;

    public async ValueTask<bool> IsIdenticalAsync(
        IMediaSource mediaSource,
        MediaItem mediaItem,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        ArgumentNullException.ThrowIfNull(mediaItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        if (mediaItem.Size is { } expectedSize &&
            await targetFileContent.GetSizeAsync(targetPath, cancellationToken) != expectedSize)
        {
            return false;
        }

        await using Stream source = await mediaSource.OpenReadAsync(mediaItem, cancellationToken);
        await using Stream target = await targetFileContent.OpenReadAsync(targetPath, cancellationToken);
        byte[] sourceBuffer = new byte[ComparisonBufferSize];
        byte[] targetBuffer = new byte[ComparisonBufferSize];

        while (true)
        {
            int sourceCount = await ReadBlockAsync(source, sourceBuffer, cancellationToken);
            int targetCount = await ReadBlockAsync(target, targetBuffer, cancellationToken);
            if (sourceCount != targetCount)
            {
                return false;
            }

            if (sourceCount == 0)
            {
                return true;
            }

            if (!sourceBuffer.AsSpan(0, sourceCount)
                    .SequenceEqual(targetBuffer.AsSpan(0, targetCount)))
            {
                return false;
            }
        }
    }

    private static async ValueTask<int> ReadBlockAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int count = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (count == 0)
            {
                break;
            }

            total += count;
        }

        return total;
    }
}
