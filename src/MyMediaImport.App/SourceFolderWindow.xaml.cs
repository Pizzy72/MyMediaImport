using MyMediaImport.Core;
using System.Windows;
using System.Windows.Controls;

namespace MyMediaImport.App;

public partial class SourceFolderWindow : Window
{
    private readonly IFolderMediaSource _source;
    private readonly string[] _initialPath;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _deviceRequests = new(1);
    private readonly CancellationToken _token;

    public SourceFolderWindow(IFolderMediaSource source, IReadOnlyList<string> initialPath)
    {
        InitializeComponent();
        _source = source;
        _initialPath = initialPath.ToArray();
        _token = _cancellation.Token;
    }

    public IReadOnlyList<string> SelectedPath { get; private set; } = Array.Empty<string>();

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        TreeViewItem root = CreateNode(null, []);
        FolderTree.Items.Add(root);
        TreeViewItem current = root;
        foreach (string segment in _initialPath)
        {
            if (!await PopulateAsync(current) || _token.IsCancellationRequested)
            {
                return;
            }

            current.IsExpanded = true;
            TreeViewItem[] matches = current.Items.OfType<TreeViewItem>()
                .Where(item => item.Tag is FolderNode node && node.Folder?.Name == segment)
                .ToArray();
            if (matches.Length != 1)
            {
                StatusText.Text = "The saved folder was not found or is ambiguous. Please choose a folder explicitly.";
                return;
            }

            current = matches[0];
        }

        if (_token.IsCancellationRequested)
        {
            return;
        }

        current.IsSelected = true;
        current.BringIntoView();
        await PopulateAsync(current);
        if (!_token.IsCancellationRequested)
        {
            current.IsExpanded = true;
        }
    }

    private TreeViewItem CreateNode(MediaSourceFolder? folder, string[] path)
    {
        TreeViewItem item = new()
        {
            Header = folder?.Name ?? "All folders",
            Tag = new FolderNode(folder, path)
        };
        item.Items.Add(new TreeViewItem { Header = "Expand to load folders", IsEnabled = false });
        item.Expanded += async (sender, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, item))
            {
                await PopulateAsync(item);
            }
        };
        return item;
    }

    private async Task<bool> PopulateAsync(TreeViewItem item)
    {
        FolderNode node = (FolderNode)item.Tag;
        if (node.IsLoaded)
        {
            return true;
        }

        if (node.IsLoading || _token.IsCancellationRequested)
        {
            return false;
        }

        node.IsLoading = true;
        StatusText.Text = $"Loading folders in {item.Header}...";
        try
        {
            await _deviceRequests.WaitAsync(_token);
            IReadOnlyList<MediaSourceFolder> children;
            try
            {
                children = await _source.GetFoldersAsync(node.Folder?.Id, _token);
            }
            finally
            {
                _deviceRequests.Release();
            }

            _token.ThrowIfCancellationRequested();
            item.Items.Clear();
            foreach (MediaSourceFolder folder in children)
            {
                item.Items.Add(CreateNode(folder, [.. node.Path, folder.Name]));
            }

            node.IsLoaded = true;
            StatusText.Text = "Only folders are shown. Files are loaded in the media preview.";
            return true;
        }
        catch (OperationCanceledException) when (_token.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            if (!_token.IsCancellationRequested)
            {
                StatusText.Text = $"Folders could not be loaded: {exception.Message} Collapse and expand to retry.";
            }

            return false;
        }
        finally
        {
            node.IsLoading = false;
        }
    }

    private void FolderTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem { Tag: FolderNode node })
        {
            SelectedPath = node.Path;
            SelectedFolderText.Text = node.Path.Length == 0
                ? "All folders on this device"
                : string.Join(" / ", node.Path);
            ChooseButton.IsEnabled = true;
        }
    }

    private void ChooseButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Window_OnClosed(object? sender, EventArgs e)
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private sealed class FolderNode(MediaSourceFolder? folder, string[] path)
    {
        public MediaSourceFolder? Folder { get; } = folder;
        public string[] Path { get; } = path;
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
    }
}
