using MyMediaImport.Core;
using System.IO;
using System.Windows.Media.Imaging;

namespace MyMediaImport.App;

public sealed class ThumbnailLoader
{
    private const int DecodePixelWidth = 192;
    private const int MaximumFallbackImageBytes = 128 * 1024 * 1024;
    private const int MaximumThumbnailBytes = 16 * 1024 * 1024;
    private const int MaximumCachedThumbnails = 64;
    private const int MaximumConcurrentRequests = 3;

    private readonly IMediaSource _mediaSource;
    private readonly ThumbnailMemoryCache _cache = new(MaximumCachedThumbnails);
    private readonly SemaphoreSlim _requestGate = new(MaximumConcurrentRequests);
    private readonly CancellationTokenSource _lifetimeCancellation = new();

    public ThumbnailLoader(IMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        _mediaSource = mediaSource;
    }

    public bool TryGetCached(MediaItem mediaItem, out BitmapSource? image) =>
        _cache.TryGet(mediaItem.Id, out image);

    public async Task<ThumbnailLoadResult> LoadAsync(
        MediaItem mediaItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        if (_cache.TryGet(mediaItem.Id, out BitmapSource? cachedImage))
        {
            return ThumbnailLoadResult.Loaded(cachedImage!);
        }

        using CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token,
                cancellationToken);
        CancellationToken linkedToken = linkedCancellation.Token;

        await _requestGate.WaitAsync(linkedToken);
        try
        {
            if (_cache.TryGet(mediaItem.Id, out cachedImage))
            {
                return ThumbnailLoadResult.Loaded(cachedImage!);
            }

            await using Stream? thumbnailStream =
                await _mediaSource.OpenThumbnailAsync(mediaItem, linkedToken);
            if (thumbnailStream is not null)
            {
                BitmapSource thumbnail = await Task.Run(
                    () => CopyAndDecodeImage(
                        thumbnailStream,
                        MaximumThumbnailBytes,
                        "thumbnail resource",
                        linkedToken),
                    linkedToken);
                _cache.Add(mediaItem.Id, thumbnail);
                return ThumbnailLoadResult.Loaded(thumbnail);
            }

            if (mediaItem.MediaKind != MediaKind.Photo)
            {
                return ThumbnailLoadResult.Unavailable();
            }

            await using Stream sourceStream =
                await _mediaSource.OpenReadAsync(mediaItem, linkedToken);
            BitmapSource fallbackImage = await Task.Run(
                () => CopyAndDecodeImage(
                    sourceStream,
                    MaximumFallbackImageBytes,
                    "source image",
                    linkedToken),
                linkedToken);
            _cache.Add(mediaItem.Id, fallbackImage);
            return ThumbnailLoadResult.Loaded(fallbackImage);
        }
        catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ThumbnailLoadResult.Failed(
                $"Thumbnail could not be loaded: {exception.Message}");
        }
        finally
        {
            _requestGate.Release();
        }
    }

    public void Cancel()
    {
        _lifetimeCancellation.Cancel();
        _cache.Clear();
    }

    private static BitmapSource CopyAndDecodeImage(
        Stream source,
        int maximumBytes,
        string resourceDescription,
        CancellationToken cancellationToken)
    {
        using MemoryStream thumbnailData = new();
        byte[] buffer = new byte[81920];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bytesRead = source.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                break;
            }

            if (thumbnailData.Length + bytesRead > maximumBytes)
            {
                throw new InvalidDataException(
                    $"The {resourceDescription} exceeds the {maximumBytes / (1024 * 1024)} MB safety limit.");
            }

            thumbnailData.Write(buffer, 0, bytesRead);
        }

        thumbnailData.Position = 0;
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.DecodePixelWidth = DecodePixelWidth;
        image.StreamSource = thumbnailData;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
