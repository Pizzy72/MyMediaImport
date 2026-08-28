namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class LocalCaptureDateSelectionRuleTests
{
    [TestMethod]
    public void IsMatch_SelectsLocalCaptureCalendarDay()
    {
        LocalCaptureDateSelectionRule rule = new(new DateOnly(2026, 8, 23));

        Assert.IsTrue(rule.IsMatch(CreateItem(
            "today", new DateTime(2026, 8, 23, 23, 30, 0))));
        Assert.IsFalse(rule.IsMatch(CreateItem(
            "yesterday", new DateTime(2026, 8, 22, 23, 59, 59))));
        Assert.IsFalse(rule.IsMatch(new MediaItem(
            "missing", "missing.jpg", 1, null, MediaKind.Photo, "image/jpeg")));
    }

    [TestMethod]
    public void IsMatch_UsesWallClockDateRatherThanUtcDate()
    {
        LocalCaptureDateSelectionRule rule = new(new DateOnly(2026, 8, 23));
        CaptureTimestamp timestamp = CaptureTimestamp.FromKnownTime(
            new DateTimeOffset(2026, 8, 23, 1, 0, 0, TimeSpan.FromHours(2)));
        MediaItem item = new(
            "item", "photo.jpg", 1, timestamp, MediaKind.Photo, "image/jpeg");

        Assert.IsTrue(rule.IsMatch(item));
        Assert.AreEqual(new DateOnly(2026, 8, 22),
            DateOnly.FromDateTime(timestamp.ResolvedTime!.Value.UtcDateTime));
    }

    private static MediaItem CreateItem(string id, DateTime localTime) =>
        new(
            id,
            $"{id}.jpg",
            1,
            CaptureTimestamp.FromLocalTime(localTime),
            MediaKind.Photo,
            "image/jpeg");
}
