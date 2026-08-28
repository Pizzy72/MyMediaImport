namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class CaptureTimeZoneResolverTests
{
    private static readonly TimeZoneInfo TestLocalTimeZone = CreateTestLocalTimeZone();
    private readonly CaptureTimeZoneResolver _resolver = new(TestLocalTimeZone);

    [TestMethod]
    public void Resolve_LocalDuringSummerTime_UsesDaylightOffset()
    {
        DateTimeOffset result = _resolver.Resolve(
            new DateTime(2026, 8, 23, 14, 43, 28), CaptureTimeZoneSpec.Local);

        Assert.AreEqual(TimeSpan.FromHours(2), result.Offset);
        Assert.AreEqual(new DateTime(2026, 8, 23, 14, 43, 28), result.DateTime);
    }

    [TestMethod]
    public void Resolve_LocalDuringWinterTime_UsesStandardOffset()
    {
        DateTimeOffset result = _resolver.Resolve(
            new DateTime(2026, 1, 23, 14, 43, 28), CaptureTimeZoneSpec.Local);

        Assert.AreEqual(TimeSpan.FromHours(1), result.Offset);
    }

    [TestMethod]
    public void Resolve_FixedPositiveOffset_UsesExactOffset()
    {
        DateTimeOffset result = _resolver.Resolve(
            new DateTime(2026, 8, 23, 8, 43, 28), CaptureTimeZoneSpec.Parse("+02:00"));

        Assert.AreEqual(TimeSpan.FromHours(2), result.Offset);
    }

    [TestMethod]
    public void Resolve_FixedNegativeOffset_UsesExactOffset()
    {
        DateTimeOffset result = _resolver.Resolve(
            new DateTime(2026, 8, 23, 8, 43, 28), CaptureTimeZoneSpec.Parse("-05:00"));

        Assert.AreEqual(TimeSpan.FromHours(-5), result.Offset);
    }

    [TestMethod]
    public void Resolve_ProducesCorrectUtcTime()
    {
        DateTimeOffset result = _resolver.Resolve(
            new DateTime(2026, 8, 23, 8, 43, 28), CaptureTimeZoneSpec.Parse("-04:00"));

        Assert.AreEqual(
            new DateTimeOffset(2026, 8, 23, 12, 43, 28, TimeSpan.Zero),
            result.ToUniversalTime());
    }

    [TestMethod]
    [DataRow("UTC")]
    [DataRow("+2:00")]
    [DataRow("02:00")]
    [DataRow("+14:01")]
    [DataRow("-05:60")]
    public void Parse_InvalidSyntax_ThrowsFormatException(string value)
    {
        Assert.ThrowsExactly<FormatException>(() => CaptureTimeZoneSpec.Parse(value));
    }

    [TestMethod]
    public void Resolve_LocalAmbiguousTime_ThrowsResolutionException()
    {
        Assert.ThrowsExactly<CaptureTimeResolutionException>(() =>
            _resolver.Resolve(new DateTime(2026, 10, 25, 2, 30, 0), CaptureTimeZoneSpec.Local));
    }

    [TestMethod]
    public void Resolve_LocalInvalidTime_ThrowsResolutionException()
    {
        Assert.ThrowsExactly<CaptureTimeResolutionException>(() =>
            _resolver.Resolve(new DateTime(2026, 3, 29, 2, 30, 0), CaptureTimeZoneSpec.Local));
    }

    private static TimeZoneInfo CreateTestLocalTimeZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday);
        TimeZoneInfo.AdjustmentRule adjustmentRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1),
            new DateTime(2030, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "Test/CentralEurope",
            TimeSpan.FromHours(1),
            "Test Central Europe",
            "Test Standard Time",
            "Test Daylight Time",
            [adjustmentRule]);
    }
}
