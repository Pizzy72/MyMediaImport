using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Threading.Channels;
using MyMediaImport.Core;

namespace MyMediaImport.Windows;

[SupportedOSPlatform("windows")]
public sealed class WpdMediaSource : IMediaSource
{
    private const string RootObjectId = "DEVICE";
    private const int ChannelCapacity = 64;
    private const uint ReadAccessMode = 0;

    public WpdMediaSource(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        DeviceId = deviceId;
    }

    public string DeviceId { get; }

    public async IAsyncEnumerable<MediaItem> GetMediaItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource producerCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<MediaItem> channel = Channel.CreateBounded<MediaItem>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });
        Task producer = Task.Run(
            () => ProduceMediaItems(channel.Writer, producerCancellation.Token),
            CancellationToken.None);

        try
        {
            await foreach (MediaItem mediaItem in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return mediaItem;
            }

            await producer.ConfigureAwait(false);
        }
        finally
        {
            producerCancellation.Cancel();
            try
            {
                await producer.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (producerCancellation.IsCancellationRequested)
            {
            }
        }
    }

    public async ValueTask<Stream> OpenReadAsync(
        MediaItem mediaItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        return await Task.Run(
            () => OpenResource(mediaItem.Id, WpdMediaKeys.DefaultResource, required: true),
            cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The WPD default resource is unavailable.");
    }

    public async ValueTask<Stream?> OpenThumbnailAsync(
        MediaItem mediaItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        return await Task.Run(
            () => OpenResource(mediaItem.Id, WpdMediaKeys.ThumbnailResource, required: false),
            cancellationToken).ConfigureAwait(false);
    }

    private void ProduceMediaItems(
        ChannelWriter<MediaItem> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            using WpdSession session = WpdSession.Open(DeviceId);
            Stack<string> containers = new();
            containers.Push(RootObjectId);

            while (containers.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string parentObjectId = containers.Pop();

                foreach (string objectId in EnumerateChildren(session.Content, parentObjectId))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WpdObjectMetadata metadata = ReadMetadata(session, objectId);

                    if (metadata.ContentType is { } contentType &&
                        (contentType == WpdMediaKeys.FolderContentType ||
                         contentType == WpdMediaKeys.FunctionalObjectContentType))
                    {
                        containers.Push(objectId);
                    }

                    if (metadata.OriginalFileName is not { } fileName)
                    {
                        continue;
                    }

                    MediaFileClassification? classification = MediaFileClassifier.Classify(fileName);
                    if (classification is null)
                    {
                        continue;
                    }

                    long? size = metadata.Size is { } unsignedSize
                        ? (long)Math.Min(unsignedSize, long.MaxValue)
                        : null;
                    DateTime? captureTime = metadata.DateCreated ?? metadata.DateModified;
                    MediaItem mediaItem = new(
                        objectId,
                        fileName,
                        size,
                        captureTime is { } localTime
                            ? CaptureTimestamp.FromLocalTime(localTime)
                            : null,
                        classification.MediaKind,
                        classification.MimeType);
                    writer.WriteAsync(mediaItem, cancellationToken).AsTask().GetAwaiter().GetResult();
                }
            }

            writer.TryComplete();
        }
        catch (Exception exception)
        {
            writer.TryComplete(exception);
        }
    }

    private Stream? OpenResource(
        string objectId,
        WpdInterop.PropertyKey resourceKey,
        bool required)
    {
        WpdSession session = WpdSession.Open(DeviceId);
        try
        {
            if (!required && !HasResource(session.Resources, objectId, resourceKey))
            {
                session.Dispose();
                return null;
            }

            session.Resources.GetStream(
                objectId,
                ref resourceKey,
                ReadAccessMode,
                out _,
                out IStream? stream);
            return new WpdReadStream(stream, session);
        }
        catch (NotImplementedException) when (!required)
        {
            session.Dispose();
            return null;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    private static bool HasResource(
        WpdInterop.IPortableDeviceResources resources,
        string objectId,
        WpdInterop.PropertyKey requestedResource)
    {
        WpdInterop.IPortableDeviceKeyCollection? keys = null;
        try
        {
            resources.GetSupportedResources(objectId, out keys);
            keys.GetCount(out uint count);
            for (uint index = 0; index < count; index++)
            {
                keys.GetAt(index, out WpdInterop.PropertyKey key);
                if (key == requestedResource)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            WpdSession.ReleaseComObject(keys);
        }
    }

    private static WpdObjectMetadata ReadMetadata(WpdSession session, string objectId)
    {
        WpdInterop.IPortableDeviceValues? values = null;
        try
        {
            session.Properties.GetValues(objectId, session.MetadataKeys, out values);
            return new WpdObjectMetadata(
                GetString(values, WpdMediaKeys.OriginalFileName),
                GetGuid(values, WpdMediaKeys.ContentType),
                GetUnsignedInteger(values, WpdMediaKeys.Size),
                GetDate(values, WpdMediaKeys.DateCreated),
                GetDate(values, WpdMediaKeys.DateModified));
        }
        finally
        {
            WpdSession.ReleaseComObject(values);
        }
    }

    private static IEnumerable<string> EnumerateChildren(
        WpdInterop.IPortableDeviceContent content,
        string parentObjectId)
    {
        WpdInterop.IEnumPortableDeviceObjectIds? enumerator = null;
        nint pointerArray = Marshal.AllocCoTaskMem(IntPtr.Size);
        try
        {
            content.EnumObjects(0, parentObjectId, null, out enumerator);
            while (true)
            {
                Marshal.WriteIntPtr(pointerArray, IntPtr.Zero);
                uint fetched = 0;
                int result = enumerator.Next(1, pointerArray, ref fetched);
                if (result < 0)
                {
                    Marshal.ThrowExceptionForHR(result);
                }

                if (fetched == 0)
                {
                    yield break;
                }

                nint objectIdPointer = Marshal.ReadIntPtr(pointerArray);
                try
                {
                    string? objectId = Marshal.PtrToStringUni(objectIdPointer);
                    if (!string.IsNullOrWhiteSpace(objectId))
                    {
                        yield return objectId;
                    }
                }
                finally
                {
                    if (objectIdPointer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(objectIdPointer);
                    }
                }
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(pointerArray);
            WpdSession.ReleaseComObject(enumerator);
        }
    }

    private static string? GetString(
        WpdInterop.IPortableDeviceValues values,
        WpdInterop.PropertyKey key) =>
        ReadValue<string?>(values, key, value => value.ValueType switch
        {
            8 => Marshal.PtrToStringBSTR(value.Pointer),
            31 => Marshal.PtrToStringUni(value.Pointer),
            _ => null
        });

    private static Guid? GetGuid(
        WpdInterop.IPortableDeviceValues values,
        WpdInterop.PropertyKey key) =>
        ReadValue<Guid?>(values, key, value =>
            value.ValueType == 72 && value.Pointer != IntPtr.Zero
                ? Marshal.PtrToStructure<Guid>(value.Pointer)
                : null);

    private static ulong? GetUnsignedInteger(
        WpdInterop.IPortableDeviceValues values,
        WpdInterop.PropertyKey key) =>
        ReadValue<ulong?>(values, key, value => value.ValueType switch
        {
            19 or 23 => value.UInt32,
            21 => value.UInt64,
            _ => null
        });

    private static DateTime? GetDate(
        WpdInterop.IPortableDeviceValues values,
        WpdInterop.PropertyKey key) =>
        ReadValue<DateTime?>(values, key, value => value.ValueType == 7
            ? DateTime.SpecifyKind(DateTime.FromOADate(value.Double), DateTimeKind.Unspecified)
            : null);

    private static T ReadValue<T>(
        WpdInterop.IPortableDeviceValues values,
        WpdInterop.PropertyKey key,
        Func<WpdInterop.PropVariant, T> convert)
    {
        try
        {
            values.GetValue(ref key, out WpdInterop.PropVariant value);
            try
            {
                return convert(value);
            }
            finally
            {
                WpdInterop.PropVariantClear(ref value);
            }
        }
        catch (COMException)
        {
            return default!;
        }
    }

    private sealed record WpdObjectMetadata(
        string? OriginalFileName,
        Guid? ContentType,
        ulong? Size,
        DateTime? DateCreated,
        DateTime? DateModified);
}
