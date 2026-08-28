namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaPreviewOrderingTests
{
    [TestMethod]
    public void NewestFirst_OrdersResolvedCaptureTimesByUtcInstant()
    {
        MediaItem earlier = CreateItem(
            "earlier",
            CaptureTimestamp.FromKnownTime(
                new(2026, 8, 24, 12, 0, 0, TimeSpan.FromHours(2))));
        MediaItem later = CreateItem(
            "later",
            CaptureTimestamp.FromKnownTime(
                new(2026, 8, 24, 11, 30, 0, TimeSpan.Zero)));

        IReadOnlyList<MediaItem> ordered = MediaPreviewOrdering.NewestFirst([earlier, later]);

        CollectionAssert.AreEqual(new[] { later, earlier }, ordered.ToArray());
    }

    [TestMethod]
    public void NewestFirst_PlacesMissingTimesLastAndPreservesTheirOrder()
    {
        MediaItem firstMissing = CreateItem("first-missing", null);
        MediaItem known = CreateItem(
            "known",
            CaptureTimestamp.FromLocalTime(new(2026, 8, 24, 12, 0, 0)));
        MediaItem secondMissing = CreateItem("second-missing", null);

        IReadOnlyList<MediaItem> ordered = MediaPreviewOrdering.NewestFirst(
            [firstMissing, known, secondMissing]);

        CollectionAssert.AreEqual(
            new[] { known, firstMissing, secondMissing },
            ordered.ToArray());
    }

    [TestMethod]
    public void NewestFirst_PreservesOrderForEqualCaptureTimes()
    {
        CaptureTimestamp captureTime = CaptureTimestamp.FromLocalTime(
            new(2026, 8, 24, 12, 0, 0));
        MediaItem first = CreateItem("first", captureTime);
        MediaItem second = CreateItem("second", captureTime);

        IReadOnlyList<MediaItem> ordered = MediaPreviewOrdering.NewestFirst([first, second]);

        CollectionAssert.AreEqual(new[] { first, second }, ordered.ToArray());
    }

    private static MediaItem CreateItem(string id, CaptureTimestamp? captureTime) =>
        new(id, $"{id}.jpg", 10, captureTime, MediaKind.Photo, "image/jpeg");
}
