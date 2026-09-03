using MyMediaImport.Windows;

namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class LocalImportFileSystemTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PublishAsync_SetsCaptureCreationTimeAndPreservesContentAndWriteTime(bool overwrite)
    {
        string directory = Path.Combine(Path.GetTempPath(), "MyMediaImport-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string targetPath = Path.Combine(directory, "photo.jpg");
        string partialPath = targetPath + ".partial";
        byte[] content = [1, 2, 3, 4, 5];
        DateTimeOffset captureTime = new(2026, 8, 22, 17, 30, 23, TimeSpan.FromHours(2));
        DateTime writeTime = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        try
        {
            if (overwrite)
            {
                await File.WriteAllBytesAsync(targetPath, new byte[] { 9 }, TestContext.CancellationToken);
                File.SetCreationTimeUtc(targetPath, captureTime.UtcDateTime.AddYears(-1));
            }

            await File.WriteAllBytesAsync(partialPath, content, TestContext.CancellationToken);
            File.SetLastWriteTimeUtc(partialPath, writeTime);
            LocalImportFileSystem fileSystem = new();

            await fileSystem.PublishAsync(partialPath, targetPath, overwrite, captureTime, TestContext.CancellationToken);

            Assert.AreEqual(captureTime.UtcDateTime, File.GetCreationTimeUtc(targetPath));
            Assert.AreEqual(writeTime, File.GetLastWriteTimeUtc(targetPath));
            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(targetPath, TestContext.CancellationToken));
            Assert.IsFalse(File.Exists(partialPath));
        }
        finally
        {
            File.Delete(partialPath);
            File.Delete(targetPath);
            Directory.Delete(directory);
        }
    }

    public TestContext TestContext { get; set; }
}
