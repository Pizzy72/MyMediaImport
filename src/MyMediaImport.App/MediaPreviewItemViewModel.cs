using MyMediaImport.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace MyMediaImport.App;

public sealed class MediaPreviewItemViewModel : INotifyPropertyChanged
{
    private readonly ThumbnailLoader _thumbnailLoader;
    private readonly Action _selectionChanged;
    private ThumbnailLoadState _thumbnailState;
    private bool _isSelected;
    private ImportResultStatus? _lastImportStatus;
    private string? _importDiagnostic;

    public MediaPreviewItemViewModel(
        MediaItem sourceMediaItem,
        MediaItem resolvedMediaItem,
        ThumbnailLoader thumbnailLoader,
        Action selectionChanged)
    {
        ArgumentNullException.ThrowIfNull(sourceMediaItem);
        ArgumentNullException.ThrowIfNull(resolvedMediaItem);
        ArgumentNullException.ThrowIfNull(thumbnailLoader);
        ArgumentNullException.ThrowIfNull(selectionChanged);
        SourceMediaItem = sourceMediaItem;
        MediaItem = resolvedMediaItem;
        _thumbnailLoader = thumbnailLoader;
        _selectionChanged = selectionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MediaItem SourceMediaItem { get; }

    public MediaItem MediaItem { get; private set; }

    public string Name => MediaItem.Name;

    public string NameToolTip => string.IsNullOrWhiteSpace(SourceMediaItem.SourcePath)
        ? Name
        : SourceMediaItem.SourcePath;

    public string MediaKindText => MediaItem.MediaKind switch
    {
        MediaKind.Photo => "Photo",
        MediaKind.Video => "Video",
        _ => "Media"
    };

    public string CaptureTimeText => MediaItem.CaptureTime?.ResolvedTime is { } captureTime
        ? captureTime.ToString("yyyy-MM-dd HH:mm:ss zzz")
        : MediaItem.CaptureTime is { } unresolvedCaptureTime
            ? unresolvedCaptureTime.LocalTime.ToString("yyyy-MM-dd HH:mm:ss")
            : "Capture time unavailable";

    public string SizeText => MediaItem.Size is { } size
        ? MainWindowViewModel.FormatByteSize(size)
        : "Size unavailable";

    public BitmapSource? Thumbnail
    {
        get
        {
            if (_thumbnailLoader.TryGetCached(MediaItem, out BitmapSource? image))
            {
                _thumbnailState = ThumbnailLoadState.Loaded;
                return image;
            }

            if (_thumbnailState == ThumbnailLoadState.Loaded)
            {
                _thumbnailState = ThumbnailLoadState.NotLoaded;
            }

            EnsureThumbnailLoading();
            return null;
        }
    }

    public string ThumbnailStatus => _thumbnailState switch
    {
        ThumbnailLoadState.NotLoaded or ThumbnailLoadState.Loading => "Loading thumbnail...",
        ThumbnailLoadState.Unavailable => "Thumbnail not available",
        ThumbnailLoadState.Failed => "Thumbnail failed",
        _ => string.Empty
    };

    public string? ThumbnailDiagnostic { get; private set; }

    public bool HasImportResult => _lastImportStatus.HasValue;

    public string ImportStatusText => _lastImportStatus switch
    {
        ImportResultStatus.Succeeded => "Imported",
        ImportResultStatus.Skipped => "Skipped",
        ImportResultStatus.AlreadyImported => "Already imported",
        ImportResultStatus.Failed => "Import failed",
        ImportResultStatus.Cancelled => "Cancelled",
        _ => string.Empty
    };

    public string? ImportDiagnostic => _importDiagnostic;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetField(ref _isSelected, value))
            {
                _selectionChanged();
            }
        }
    }

    public void UpdateResolvedCaptureTime(CaptureTimestamp? captureTime)
    {
        MediaItem = SourceMediaItem.WithCaptureTime(captureTime);
        OnPropertyChanged(nameof(MediaItem));
        OnPropertyChanged(nameof(CaptureTimeText));
    }

    public void ApplyImportResult(ImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _lastImportStatus = result.Status;
        _importDiagnostic = result.Diagnostic;
        OnPropertyChanged(nameof(HasImportResult));
        OnPropertyChanged(nameof(ImportStatusText));
        OnPropertyChanged(nameof(ImportDiagnostic));
    }

    private void EnsureThumbnailLoading()
    {
        if (_thumbnailState != ThumbnailLoadState.NotLoaded)
        {
            return;
        }

        _thumbnailState = ThumbnailLoadState.Loading;
        OnPropertyChanged(nameof(ThumbnailStatus));
        _ = LoadThumbnailAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        try
        {
            ThumbnailLoadResult result = await _thumbnailLoader.LoadAsync(MediaItem);
            _thumbnailState = result.State;
            ThumbnailDiagnostic = result.Diagnostic;
            OnPropertyChanged(nameof(Thumbnail));
            OnPropertyChanged(nameof(ThumbnailStatus));
            OnPropertyChanged(nameof(ThumbnailDiagnostic));
        }
        catch (OperationCanceledException)
        {
            _thumbnailState = ThumbnailLoadState.NotLoaded;
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
