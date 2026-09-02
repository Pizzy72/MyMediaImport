namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class PreviewSummaryTests
{
    [TestMethod]
    public void Summary_ContainsLoadedFolderAndCount()
    {
        string summary = MainWindowViewModel.FormatPreviewSummary(
            4, ["Internal Storage", "DCIM", "Camera"]);
        Assert.AreEqual("4 media files · Internal Storage / DCIM / Camera", summary);
    }

    [TestMethod]
    public void Summary_WithoutFolderNamesAllFolders()
    {
        Assert.AreEqual("1 media file · All folders", MainWindowViewModel.FormatPreviewSummary(1, []));
    }

    [TestMethod]
    public void EmptyPreview_StillIdentifiesSourceFolder()
    {
        Assert.AreEqual("0 media files · DCIM", MainWindowViewModel.FormatPreviewSummary(0, ["DCIM"]));
    }
}
