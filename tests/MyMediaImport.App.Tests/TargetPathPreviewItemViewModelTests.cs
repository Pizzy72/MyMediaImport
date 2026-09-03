using MyMediaImport.Core;

namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class TargetPathPreviewItemViewModelTests
{
    [TestMethod]
    [DataRow(ImportResultStatus.Succeeded, "Imported", true, false, false, true)]
    [DataRow(ImportResultStatus.Skipped, "Skipped", false, false, false, true)]
    [DataRow(ImportResultStatus.AlreadyImported, "Already imported", false, true, false, true)]
    [DataRow(ImportResultStatus.Failed, "Failed", false, false, true, false)]
    [DataRow(ImportResultStatus.Cancelled, "Cancelled", false, false, true, false)]
    public void Constructor_RepresentsActualImportResult(
        ImportResultStatus resultStatus,
        string expectedStatus,
        bool isImported,
        bool isAlreadyImported,
        bool isError,
        bool canRevealInExplorer)
    {
        MediaItem mediaItem = new(
            "item-1",
            "IMG_1234.JPG",
            5,
            CaptureTimestamp.FromKnownTime(new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero)),
            MediaKind.Photo,
            "image/jpeg");
        ImportResult result = new(
            mediaItem,
            @"E:\Pictures\2026\IMG_1234_02.jpg",
            5,
            5,
            resultStatus,
            "Diagnostic");

        TargetPathPreviewItemViewModel viewModel = new(result);

        Assert.AreEqual("IMG_1234.JPG", viewModel.SourceName);
        Assert.AreEqual<long?>(5, viewModel.ExpectedSize);
        Assert.AreEqual(@"E:\Pictures\2026\IMG_1234_02.jpg", viewModel.TargetPath);
        Assert.AreEqual(expectedStatus, viewModel.Status);
        Assert.AreEqual("Diagnostic", viewModel.Diagnostic);
        Assert.AreEqual(isImported, viewModel.IsImportReady);
        Assert.AreEqual(isAlreadyImported, viewModel.IsAlreadyImported);
        Assert.AreEqual(isError, viewModel.IsConflictOrError);
        Assert.AreEqual(canRevealInExplorer, viewModel.CanRevealInExplorer);
    }
}
