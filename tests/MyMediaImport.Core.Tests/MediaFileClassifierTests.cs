namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaFileClassifierTests
{
    [TestMethod]
    [DataRow("photo.jpg", MediaKind.Photo, "image/jpeg")]
    [DataRow("photo.JPEG", MediaKind.Photo, "image/jpeg")]
    [DataRow("photo.heic", MediaKind.Photo, "image/heic")]
    [DataRow("photo.HEIF", MediaKind.Photo, "image/heif")]
    [DataRow("clip.mov", MediaKind.Video, "video/quicktime")]
    public void Classify_SupportedExtension_ReturnsClassification(
        string fileName,
        MediaKind expectedKind,
        string expectedMimeType)
    {
        MediaFileClassification? result = MediaFileClassifier.Classify(fileName);

        Assert.IsNotNull(result);
        Assert.AreEqual(expectedKind, result.MediaKind);
        Assert.AreEqual(expectedMimeType, result.MimeType);
    }

    [TestMethod]
    [DataRow("sidecar.aae")]
    [DataRow("image.png")]
    [DataRow("no-extension")]
    public void Classify_UnsupportedExtension_ReturnsNull(string fileName)
    {
        Assert.IsNull(MediaFileClassifier.Classify(fileName));
    }
}
