using System.Windows.Media.Imaging;

namespace MyMediaImport.App;

public sealed record ThumbnailLoadResult(
    ThumbnailLoadState State,
    BitmapSource? Image,
    string? Diagnostic)
{
    public static ThumbnailLoadResult Loaded(BitmapSource image) =>
        new(ThumbnailLoadState.Loaded, image, null);

    public static ThumbnailLoadResult Unavailable() =>
        new(ThumbnailLoadState.Unavailable, null, "Thumbnail not available");

    public static ThumbnailLoadResult Failed(string diagnostic) =>
        new(ThumbnailLoadState.Failed, null, diagnostic);
}
