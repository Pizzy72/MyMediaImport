namespace MyMediaImport.Core;

public sealed record MediaItem
{
    public MediaItem(
        string id,
        string name,
        long? size,
        CaptureTimestamp? captureTime,
        MediaKind mediaKind,
        string? mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Id = id;
        Name = name;
        Size = size;
        CaptureTime = captureTime;
        MediaKind = mediaKind;
        MimeType = mimeType;
    }

    public string Id { get; }

    public string Name { get; }

    public long? Size { get; }

    public CaptureTimestamp? CaptureTime { get; }

    public MediaKind MediaKind { get; }

    public string? MimeType { get; }

    public MediaItem WithCaptureTime(CaptureTimestamp? captureTime) =>
        new(Id, Name, Size, captureTime, MediaKind, MimeType);
}
