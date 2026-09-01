namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class ByteProgressFormattingTests
{
    [TestMethod]
    public void FormatByteProgress_UsesExpectedSizeUnitAndStableWidth()
    {
        long expectedBytes = 495L * 1024 * 1024;

        string earlyProgress = MainWindowViewModel.FormatByteProgress(
            128L * 1024,
            expectedBytes);
        string laterProgress = MainWindowViewModel.FormatByteProgress(
            297L * 1024 * 1024,
            expectedBytes);

        Assert.Contains("MB of", earlyProgress);
        Assert.DoesNotContain("KB", earlyProgress);
        Assert.AreEqual(earlyProgress.Length, laterProgress.Length);
        Assert.AreEqual(
            earlyProgress.IndexOf(" MB of", StringComparison.Ordinal),
            laterProgress.IndexOf(" MB of", StringComparison.Ordinal));
    }
}
