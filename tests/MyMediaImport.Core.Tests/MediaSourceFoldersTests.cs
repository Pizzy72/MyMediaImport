namespace MyMediaImport.Core.Tests;

[TestClass]
public sealed class MediaSourceFoldersTests
{
    [TestMethod]
    public async Task EmptyPath_KeepsSourceAndDoesNotBrowse()
    {
        FolderSource source = new();
        IMediaSource result = await MediaSourceFolders.OpenAsync(source, [], TestContext.CancellationToken);
        Assert.AreSame(source, result);
        Assert.HasCount(0, source.Requests);
    }

    [TestMethod]
    public async Task CameraPath_OnlyResolvesAncestors_NotSiblingContents()
    {
        FolderSource source = new();
        await MediaSourceFolders.OpenAsync(source, ["Internal Storage", "DCIM", "Camera"], TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { "root", "storage", "dcim" }, source.Requests);
        Assert.AreEqual("camera", source.OpenedId);
    }

    [TestMethod]
    public async Task MissingFolder_DoesNotFallBackToWholeDevice()
    {
        FolderSource source = new();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await MediaSourceFolders.OpenAsync(source, ["Internal Storage", "Missing"], TestContext.CancellationToken));
        Assert.IsNull(source.OpenedId);
    }

    [TestMethod]
    public async Task DuplicateNames_AreRejected()
    {
        FolderSource source = new();
        source.Children["dcim"] = [new("one", "Camera"), new("two", "Camera")];
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await MediaSourceFolders.OpenAsync(source, ["Internal Storage", "DCIM", "Camera"], TestContext.CancellationToken));
        Assert.IsNull(source.OpenedId);
    }

    [TestMethod]
    public async Task ReconnectedDevice_UsesFreshObjectId()
    {
        FolderSource source = new();
        source.Children["dcim"] = [new("new-id", "Camera")];
        await MediaSourceFolders.OpenAsync(source, ["Internal Storage", "DCIM", "Camera"], TestContext.CancellationToken);
        Assert.AreEqual("new-id", source.OpenedId);
    }

    [TestMethod]
    public async Task Cancellation_DoesNotBrowse()
    {
        FolderSource source = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await MediaSourceFolders.OpenAsync(source, ["Internal Storage"], cancellation.Token));
        Assert.HasCount(0, source.Requests);
    }

    private sealed class FolderSource : IFolderMediaSource
    {
        public Dictionary<string, IReadOnlyList<MediaSourceFolder>> Children { get; } = new()
        {
            ["root"] = [new("storage", "Internal Storage")],
            ["storage"] = [new("dcim", "DCIM"), new("music", "Music"), new("downloads", "Download")],
            ["dcim"] = [new("camera", "Camera")]
        };
        public List<string> Requests { get; } = [];
        public string? OpenedId { get; private set; }

        public ValueTask<IReadOnlyList<MediaSourceFolder>> GetFoldersAsync(
            string? parentFolderId = null, CancellationToken cancellationToken = default)
        {
            string id = parentFolderId ?? "root";
            Requests.Add(id);
            return ValueTask.FromResult(Children[id]);
        }

        public IMediaSource OpenFolder(MediaSourceFolder folder)
        {
            OpenedId = folder.Id;
            return this;
        }

        public async IAsyncEnumerable<MediaItem> GetMediaItemsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<Stream> OpenReadAsync(MediaItem mediaItem, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Stream?> OpenThumbnailAsync(MediaItem mediaItem, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    public TestContext TestContext { get; set; }
}
