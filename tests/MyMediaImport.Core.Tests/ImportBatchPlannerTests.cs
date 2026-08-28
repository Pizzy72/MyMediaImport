namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class ImportBatchPlannerTests
{
    [TestMethod]
    public async Task CreatePlanAsync_ResolvesTimezoneBeforeUtcPathGeneration()
    {
        MediaItem item = new(
            "item",
            "photo.jpg",
            10,
            CaptureTimestamp.FromLocalTime(new DateTime(2026, 8, 23, 14, 3, 33)),
            MediaKind.Photo,
            "image/jpeg");
        EmptyTargetLookup lookup = new();
        ImportBatchPlanner planner = new(
            new ImportPlanner(lookup),
            new CaptureTimeZoneResolver());

        ImportPlan plan = await planner.CreatePlanAsync(
            [item],
            CaptureTimeZoneSpec.Parse("+02:00"),
            new PathTemplate(
                @"C:\Import\{captureUtc:yyyy-MM-dd_HHmmss'Z'}.{ext}"),
            ExistingFilePolicy.Skip, TestContext.CancellationToken);

        Assert.AreEqual(ImportPlanStatus.Ready, plan.Items[0].Status);
        Assert.AreEqual(
            @"C:\Import\2026-08-23_120333Z.jpg",
            plan.Items[0].TargetPath);
        Assert.AreEqual(TimeSpan.FromHours(2),
            plan.Items[0].MediaItem.CaptureTime!.Offset);
    }

    private sealed class EmptyTargetLookup : ITargetFileLookup
    {
        public ValueTask<bool> ExistsAsync(
            string fullPath,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);
    }

    public TestContext TestContext { get; set; }
}
