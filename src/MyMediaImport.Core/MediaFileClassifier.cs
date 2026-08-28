namespace MyMediaImport.Core;

public static class MediaFileClassifier
{
    public static MediaFileClassification? Classify(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.GetExtension(fileName).ToUpperInvariant() switch
        {
            ".JPG" or ".JPEG" => new(MediaKind.Photo, "image/jpeg"),
            ".HEIC" => new(MediaKind.Photo, "image/heic"),
            ".HEIF" => new(MediaKind.Photo, "image/heif"),
            ".MOV" => new(MediaKind.Video, "video/quicktime"),
            _ => null
        };
    }
}
