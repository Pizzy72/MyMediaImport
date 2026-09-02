namespace MyMediaImport.Core;

public sealed record MediaItem
{
    public MediaItem(
        string id,
        string name,
        long? size,
        CaptureTimestamp? captureTime,
        MediaKind mediaKind,
        string? mimeType,
        string? sourcePath = null)
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
        SourcePath = sourcePath;
    }

    public string Id { get; }

    public string Name { get; }

    public long? Size { get; }

    public CaptureTimestamp? CaptureTime { get; }

    public MediaKind MediaKind { get; }

    public string? MimeType { get; }

    /// <summary>A display-only source path, including the filename; not a local filesystem path.</summary>
    public string? SourcePath { get; }

    public MediaItem WithCaptureTime(CaptureTimestamp? captureTime) =>
        new(Id, Name, Size, captureTime, MediaKind, MimeType, SourcePath);
}
