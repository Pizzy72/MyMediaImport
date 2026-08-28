namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class ApplicationVersionTests
{
    [TestMethod]
    public void CurrentIsSemanticVersionWithoutBuildMetadata()
    {
        Assert.DoesNotContain('+', ApplicationVersion.Current);
        StringAssert.Matches(
            ApplicationVersion.Current,
            new System.Text.RegularExpressions.Regex(
                @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$"));
    }

    [TestMethod]
    public void WindowTitleIncludesCurrentVersion()
    {
        Assert.AreEqual(
            $"MyMediaImport {ApplicationVersion.Current}",
            ApplicationVersion.WindowTitle);
    }
}
