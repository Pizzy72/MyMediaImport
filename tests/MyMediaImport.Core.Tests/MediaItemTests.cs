namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaItemTests
{
    [TestMethod]
    public void WithCaptureTime_PreservesSourcePath()
    {
        const string path = "Internal Storage / DCIM / Camera / photo.jpg";
        MediaItem item = new("id", "photo.jpg", 42, null, MediaKind.Photo, "image/jpeg", path);
        MediaItem resolved = item.WithCaptureTime(CaptureTimestamp.FromKnownTime(DateTimeOffset.UtcNow));
        Assert.AreEqual(path, resolved.SourcePath);
        Assert.AreEqual(item.Id, resolved.Id);
    }

    [TestMethod]
    public void Constructor_PreservesDomainValues()
    {
        DateTimeOffset captureTime = new(
            2026, 8, 22, 17, 30, 23, TimeSpan.FromHours(2));

        CaptureTimestamp timestamp = CaptureTimestamp.FromKnownTime(captureTime);
        MediaItem item = new(
            "item-1", "IMG_1234.heic", 42, timestamp, MediaKind.Photo, "image/heic");

        Assert.AreEqual("item-1", item.Id);
        Assert.AreEqual("IMG_1234.heic", item.Name);
        Assert.AreEqual(42, item.Size);
        Assert.AreEqual(captureTime, item.CaptureTime!.ResolvedTime);
        Assert.AreEqual(MediaKind.Photo, item.MediaKind);
        Assert.AreEqual("image/heic", item.MimeType);
        Assert.IsNull(item.SourcePath);
    }

    [TestMethod]
    public void Constructor_RejectsNegativeSize()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new MediaItem("item-1", "photo.jpg", -1, null, MediaKind.Photo, "image/jpeg"));
    }
}
