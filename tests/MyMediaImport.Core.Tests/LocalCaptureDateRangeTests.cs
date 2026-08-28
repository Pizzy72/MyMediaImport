namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class LocalCaptureDateRangeTests
{
    private static readonly DateOnly CurrentDate = new(2026, 8, 23);

    [TestMethod]
    public void Resolve_Today_UsesFixedCurrentCalendarDay()
    {
        LocalCaptureDateRange range = Resolve(preset: LocalCaptureDatePreset.Today);

        Assert.AreEqual(CurrentDate, range.From);
        Assert.AreEqual(CurrentDate, range.To);
    }

    [TestMethod]
    public void Resolve_Yesterday_UsesPreviousCalendarDay()
    {
        LocalCaptureDateRange range = Resolve(preset: LocalCaptureDatePreset.Yesterday);

        Assert.AreEqual(new DateOnly(2026, 8, 22), range.From);
        Assert.AreEqual(range.From, range.To);
    }

    [TestMethod]
    public void Resolve_LastOneDay_SelectsOnlyToday()
    {
        LocalCaptureDateRange range = Resolve(lastDays: 1);

        Assert.AreEqual(CurrentDate, range.From);
        Assert.AreEqual(CurrentDate, range.To);
    }

    [TestMethod]
    public void Resolve_LastThreeDays_SelectsCalendarDaysIncludingToday()
    {
        LocalCaptureDateRange range = Resolve(lastDays: 3);

        Assert.AreEqual(new DateOnly(2026, 8, 21), range.From);
        Assert.AreEqual(CurrentDate, range.To);
    }

    [TestMethod]
    public void Resolve_From_HasNoUpperBoundary()
    {
        DateOnly from = new(2026, 8, 20);
        LocalCaptureDateRange range = Resolve(from: from);

        Assert.AreEqual(from, range.From);
        Assert.IsNull(range.To);
    }

    [TestMethod]
    public void Resolve_To_HasNoLowerBoundary()
    {
        DateOnly to = new(2026, 8, 23);
        LocalCaptureDateRange range = Resolve(to: to);

        Assert.IsNull(range.From);
        Assert.AreEqual(to, range.To);
    }

    [TestMethod]
    public void Resolve_FromAndTo_AreInclusive()
    {
        LocalCaptureDateRange range = Resolve(
            from: new DateOnly(2026, 8, 20),
            to: new DateOnly(2026, 8, 23));

        Assert.IsTrue(range.Contains(new DateOnly(2026, 8, 20)));
        Assert.IsTrue(range.Contains(new DateOnly(2026, 8, 23)));
        Assert.IsFalse(range.Contains(new DateOnly(2026, 8, 19)));
        Assert.IsFalse(range.Contains(new DateOnly(2026, 8, 24)));
    }

    [TestMethod]
    public void Resolve_All_HasNoBoundaries()
    {
        LocalCaptureDateRange range = Resolve(preset: LocalCaptureDatePreset.All);

        Assert.IsNull(range.From);
        Assert.IsNull(range.To);
        Assert.IsTrue(range.Contains(new DateOnly(1900, 1, 1)));
        Assert.IsTrue(range.Contains(new DateOnly(2100, 12, 31)));
    }

    [TestMethod]
    public void ParseIsoDate_RejectsInvalidOrCultureDependentDate()
    {
        Assert.ThrowsExactly<FormatException>(() =>
            LocalCaptureDateRange.ParseIsoDate("23.08.2026"));
        Assert.ThrowsExactly<FormatException>(() =>
            LocalCaptureDateRange.ParseIsoDate("2026-02-30"));
    }

    [TestMethod]
    public void Resolve_LastZeroDays_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Resolve(lastDays: 0));
    }

    [TestMethod]
    public void Resolve_MixedSelectionModes_AreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Resolve(
            preset: LocalCaptureDatePreset.Today,
            from: new DateOnly(2026, 8, 20)));
        Assert.ThrowsExactly<ArgumentException>(() => Resolve(
            lastDays: 3,
            to: new DateOnly(2026, 8, 23)));
    }

    [TestMethod]
    public void Resolve_MissingSelectionMode_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Resolve());
    }

    [TestMethod]
    public void Resolve_FromAfterTo_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Resolve(
            from: new DateOnly(2026, 8, 24),
            to: new DateOnly(2026, 8, 23)));
    }

    [TestMethod]
    public void SelectionRule_UsesLocalCaptureDateAndExcludesMissingTime()
    {
        LocalCaptureDateRange range = new(
            new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 23));
        LocalCaptureDateRangeSelectionRule rule = new(range);

        Assert.IsTrue(rule.IsMatch(CreateItem(new DateTime(2026, 8, 20, 0, 0, 0))));
        Assert.IsTrue(rule.IsMatch(CreateItem(new DateTime(2026, 8, 23, 23, 59, 59))));
        Assert.IsFalse(rule.IsMatch(CreateItem(new DateTime(2026, 8, 24, 0, 0, 0))));
        Assert.IsFalse(rule.IsMatch(new MediaItem(
            "missing", "missing.jpg", 1, null, MediaKind.Photo, "image/jpeg")));
    }

    private static LocalCaptureDateRange Resolve(
        LocalCaptureDatePreset? preset = null,
        int? lastDays = null,
        DateOnly? from = null,
        DateOnly? to = null) =>
        new LocalCaptureDateSelectionRequest(preset, lastDays, from, to).Resolve(CurrentDate);

    private static MediaItem CreateItem(DateTime localCaptureTime) =>
        new(
            localCaptureTime.ToString("O"),
            "photo.jpg",
            1,
            CaptureTimestamp.FromLocalTime(localCaptureTime),
            MediaKind.Photo,
            "image/jpeg");
}
