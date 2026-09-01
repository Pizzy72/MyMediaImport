using MyMediaImport.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyMediaImport.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IDeviceDiscoveryService _deviceDiscovery;
    private readonly IMediaSourceFactory _mediaSourceFactory;
    private readonly CaptureTimeZoneResolver _captureTimeZoneResolver = new();
    private readonly ImportBatchPlanner _importBatchPlanner;
    private readonly BatchMediaImporter _batchMediaImporter;
    private readonly AppThemeService _themeService;
    private readonly AsyncCommand _loadPreviewCommand;
    private readonly AsyncCommand _importSelectedCommand;
    private readonly RelayCommand _selectAllCommand;
    private readonly RelayCommand _selectNoneCommand;
    private readonly RelayCommand _cancelImportCommand;
    private DeviceOption? _selectedDevice;
    private string? _preferredDeviceId;
    private TimeSelectionMode _selectedTimeSelection = TimeSelectionMode.Today;
    private int _lastDays = 7;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private TimeZoneSelectionMode _selectedTimeZone = TimeZoneSelectionMode.Local;
    private string _fixedOffset = AppUserSettingsDefaults.CreateFixedUtcOffset();
    private string _extensionFilter = "JPG,HEIC,MOV";
    private string _targetTemplate = AppUserSettingsDefaults.CreateTargetTemplate();
    private ExistingFilePolicy _existingFilePolicy = ExistingFilePolicy.Skip;
    private AppTheme _selectedTheme = AppTheme.System;
    private AppFontSize _selectedFontSize = AppFontSize.Medium;
    private bool _isLoadingDevices;
    private bool _isLoadingPreview;
    private bool _isPlanningTargetPaths;
    private bool _isTargetPathDetailsVisible;
    private bool _isImporting;
    private bool _isImportSuccessful;
    private bool _isImportCancelled;
    private bool _isImportFailed;
    private bool _settingsValid;
    private bool _suppressSelectionUpdates;
    private int _targetPlanErrorCount;
    private string _deviceStatus = "Devices have not been loaded yet.";
    private string _validationStatus = "Settings are valid.";
    private string _previewStatus = "Select a device and load the preview.";
    private string _targetPathStatus = "Select media to calculate target paths.";
    private string _importStatus = string.Empty;
    private string _importFileProgress = string.Empty;
    private string _importByteProgress = string.Empty;
    private string _importByteProgressLayoutText = string.Empty;
    private int _importCompletedCount;
    private int _importTotalCount;
    private int _importedCount;
    private int _skippedCount;
    private int _alreadyImportedCount;
    private int _importFailedCount;
    private double _importProgressValue;
    private long? _importExpectedBytes;
    private long _importTransferredBytes;
    private IReadOnlyList<MediaPreviewItemViewModel> _previewItems =
        Array.Empty<MediaPreviewItemViewModel>();
    private CancellationTokenSource? _previewLoadCancellation;
    private CancellationTokenSource? _targetPathPlanCancellation;
    private CancellationTokenSource? _importCancellation;
    private TaskCompletionSource? _importCompletionSource;
    private ThumbnailLoader? _thumbnailLoader;
    private IMediaSource? _previewMediaSource;
    private IReadOnlyList<TargetPathPreviewItemViewModel> _targetPathItems =
        Array.Empty<TargetPathPreviewItemViewModel>();
    private ImportPlan? _currentImportPlan;

    public MainWindowViewModel(
        IDeviceDiscoveryService deviceDiscovery,
        IMediaSourceFactory mediaSourceFactory,
        IImportFileSystem importFileSystem,
        AppThemeService themeService)
        : this(
            deviceDiscovery,
            mediaSourceFactory,
            importFileSystem,
            themeService,
            AppUserSettings.CreateDefault())
    {
    }

    internal MainWindowViewModel(
        IDeviceDiscoveryService deviceDiscovery,
        IMediaSourceFactory mediaSourceFactory,
        IImportFileSystem importFileSystem,
        AppThemeService themeService,
        AppUserSettings settings)
    {
        _deviceDiscovery = deviceDiscovery;
        _mediaSourceFactory = mediaSourceFactory;
        _importBatchPlanner = new(
            new(importFileSystem),
            _captureTimeZoneResolver);
        _batchMediaImporter = new(new(importFileSystem));
        _themeService = themeService;
        RestoreSettings(settings);
        RefreshDevicesCommand = new AsyncCommand(RefreshDevicesAsync);
        _loadPreviewCommand = new(LoadPreviewAsync, CanLoadPreview);
        _importSelectedCommand = new(ImportSelectedAsync, CanImportSelected);
        _selectAllCommand = new(SelectAll, () => PreviewItems.Count > 0 && !IsImporting);
        _selectNoneCommand = new(
            SelectNone,
            () => PreviewItems.Any(item => item.IsSelected) && !IsImporting);
        _cancelImportCommand = new(
            CancelImport,
            () => IsImporting && _importCancellation is { IsCancellationRequested: false });
        ToggleTargetPathDetailsCommand = new RelayCommand(ToggleTargetPathDetails);
        _themeService.Apply(_selectedTheme);
        _themeService.ApplyFontSize(_selectedFontSize);
        ValidateSettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceOption> Devices { get; } = [];

    public IReadOnlyList<SelectionOption<TimeSelectionMode>> TimeSelections { get; } =
    [
        new(TimeSelectionMode.Today, "Today"),
        new(TimeSelectionMode.Yesterday, "Yesterday"),
        new(TimeSelectionMode.LastDays, "Last N days"),
        new(TimeSelectionMode.FromTo, "From / To"),
        new(TimeSelectionMode.All, "All")
    ];

    public IReadOnlyList<SelectionOption<TimeZoneSelectionMode>> TimeZoneSelections { get; } =
    [
        new(TimeZoneSelectionMode.Local, "Local"),
        new(TimeZoneSelectionMode.FixedOffset, "Fixed UTC offset")
    ];

    public IReadOnlyList<ExistingFilePolicy> ExistingFilePolicies { get; } =
        Enum.GetValues<ExistingFilePolicy>();

    public IReadOnlyList<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>();

    public IReadOnlyList<AppFontSize> FontSizes { get; } = Enum.GetValues<AppFontSize>();

    public ICommand RefreshDevicesCommand { get; }

    public ICommand LoadPreviewCommand => _loadPreviewCommand;

    public ICommand ImportSelectedCommand => _importSelectedCommand;

    public ICommand CancelImportCommand => _cancelImportCommand;

    public ICommand SelectAllCommand => _selectAllCommand;

    public ICommand SelectNoneCommand => _selectNoneCommand;

    public ICommand ToggleTargetPathDetailsCommand { get; }

    public DeviceOption? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetField(ref _selectedDevice, value))
            {
                if (value is not null)
                {
                    _preferredDeviceId = value.Id;
                }

                InvalidatePreview("The media source changed. Load the preview again.");
            }
        }
    }

    public TimeSelectionMode SelectedTimeSelection
    {
        get => _selectedTimeSelection;
        set
        {
            if (SetField(ref _selectedTimeSelection, value))
            {
                OnPropertyChanged(nameof(IsLastDaysSelected));
                OnPropertyChanged(nameof(IsFromToSelected));
                PreviewSettingChanged();
            }
        }
    }

    public bool IsLastDaysSelected => SelectedTimeSelection == TimeSelectionMode.LastDays;

    public bool IsFromToSelected => SelectedTimeSelection == TimeSelectionMode.FromTo;

    public int LastDays
    {
        get => _lastDays;
        set
        {
            if (SetField(ref _lastDays, value))
            {
                PreviewSettingChanged();
            }
        }
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetField(ref _fromDate, value))
            {
                PreviewSettingChanged();
            }
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetField(ref _toDate, value))
            {
                PreviewSettingChanged();
            }
        }
    }

    public TimeZoneSelectionMode SelectedTimeZone
    {
        get => _selectedTimeZone;
        set
        {
            if (SetField(ref _selectedTimeZone, value))
            {
                OnPropertyChanged(nameof(IsFixedOffsetSelected));
                TargetPlanningSettingChanged();
            }
        }
    }

    public bool IsFixedOffsetSelected => SelectedTimeZone == TimeZoneSelectionMode.FixedOffset;

    public string FixedOffset
    {
        get => _fixedOffset;
        set
        {
            if (SetField(ref _fixedOffset, value))
            {
                TargetPlanningSettingChanged();
            }
        }
    }

    public string ExtensionFilter
    {
        get => _extensionFilter;
        set
        {
            if (SetField(ref _extensionFilter, value))
            {
                PreviewSettingChanged();
            }
        }
    }

    public string TargetTemplate
    {
        get => _targetTemplate;
        set
        {
            if (SetField(ref _targetTemplate, value))
            {
                ClearImportStatus();
                ValidateSettings();
                ScheduleTargetPathPlan();
            }
        }
    }

    public ExistingFilePolicy ExistingFilePolicy
    {
        get => _existingFilePolicy;
        set
        {
            if (SetField(ref _existingFilePolicy, value))
            {
                ClearImportStatus();
                ScheduleTargetPathPlan();
            }
        }
    }

    public AppTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetField(ref _selectedTheme, value))
            {
                _themeService.Apply(value);
            }
        }
    }

    public AppFontSize SelectedFontSize
    {
        get => _selectedFontSize;
        set
        {
            if (SetField(ref _selectedFontSize, value))
            {
                _themeService.ApplyFontSize(value);
            }
        }
    }

    public bool IsLoadingDevices
    {
        get => _isLoadingDevices;
        private set => SetField(ref _isLoadingDevices, value);
    }

    public bool IsLoadingPreview
    {
        get => _isLoadingPreview;
        private set
        {
            if (SetField(ref _isLoadingPreview, value))
            {
                _loadPreviewCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string DeviceStatus
    {
        get => _deviceStatus;
        private set => SetField(ref _deviceStatus, value);
    }

    public string ValidationStatus
    {
        get => _validationStatus;
        private set => SetField(ref _validationStatus, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        private set => SetField(ref _previewStatus, value);
    }

    public bool IsPlanningTargetPaths
    {
        get => _isPlanningTargetPaths;
        private set
        {
            if (SetField(ref _isPlanningTargetPaths, value))
            {
                _importSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string TargetPathStatus
    {
        get => _targetPathStatus;
        private set => SetField(ref _targetPathStatus, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        private set
        {
            if (SetField(ref _isImporting, value))
            {
                OnPropertyChanged(nameof(IsImportInteractionEnabled));
                _importSelectedCommand.RaiseCanExecuteChanged();
                _cancelImportCommand.RaiseCanExecuteChanged();
                RaisePreviewCommandStateChanged();
            }
        }
    }

    public bool IsImportInteractionEnabled => !IsImporting;

    public bool IsImportSuccessful
    {
        get => _isImportSuccessful;
        private set => SetField(ref _isImportSuccessful, value);
    }

    public bool IsImportCancelled
    {
        get => _isImportCancelled;
        private set => SetField(ref _isImportCancelled, value);
    }

    public bool IsImportFailed
    {
        get => _isImportFailed;
        private set => SetField(ref _isImportFailed, value);
    }

    public string ImportStatus
    {
        get => _importStatus;
        private set
        {
            if (SetField(ref _importStatus, value))
            {
                OnPropertyChanged(nameof(HasImportStatus));
            }
        }
    }

    public bool HasImportStatus => !string.IsNullOrWhiteSpace(ImportStatus);

    public string ImportFileProgress
    {
        get => _importFileProgress;
        private set => SetField(ref _importFileProgress, value);
    }

    public string ImportByteProgress
    {
        get => _importByteProgress;
        private set => SetField(ref _importByteProgress, value);
    }

    public string ImportByteProgressLayoutText
    {
        get => _importByteProgressLayoutText;
        private set => SetField(ref _importByteProgressLayoutText, value);
    }

    public int ImportCompletedCount
    {
        get => _importCompletedCount;
        private set => SetField(ref _importCompletedCount, value);
    }

    public int ImportTotalCount
    {
        get => _importTotalCount;
        private set => SetField(ref _importTotalCount, value);
    }

    public double ImportProgressValue
    {
        get => _importProgressValue;
        private set => SetField(ref _importProgressValue, value);
    }

    public int ImportedCount
    {
        get => _importedCount;
        private set => SetField(ref _importedCount, value);
    }

    public int SkippedCount
    {
        get => _skippedCount;
        private set => SetField(ref _skippedCount, value);
    }

    public int AlreadyImportedCount
    {
        get => _alreadyImportedCount;
        private set => SetField(ref _alreadyImportedCount, value);
    }

    public int ImportFailedCount
    {
        get => _importFailedCount;
        private set => SetField(ref _importFailedCount, value);
    }

    public long ImportTransferredBytes
    {
        get => _importTransferredBytes;
        private set => SetField(ref _importTransferredBytes, value);
    }

    public bool IsTargetPathDetailsVisible
    {
        get => _isTargetPathDetailsVisible;
        private set
        {
            if (SetField(ref _isTargetPathDetailsVisible, value))
            {
                OnPropertyChanged(nameof(TargetPathDetailsToggleText));
            }
        }
    }

    public string TargetPathDetailsToggleText =>
        IsTargetPathDetailsVisible ? "Hide details" : "Show details";

    public IReadOnlyList<MediaPreviewItemViewModel> PreviewItems
    {
        get => _previewItems;
        private set
        {
            if (SetField(ref _previewItems, value))
            {
                OnPropertyChanged(nameof(IsPreviewEmpty));
                OnPropertyChanged(nameof(HasPreviewItems));
                OnPropertyChanged(nameof(SelectedSummary));
                RaisePreviewCommandStateChanged();
            }
        }
    }

    public bool IsPreviewEmpty => PreviewItems.Count == 0;

    public bool HasPreviewItems => PreviewItems.Count > 0;

    public IReadOnlyList<TargetPathPreviewItemViewModel> TargetPathItems
    {
        get => _targetPathItems;
        private set
        {
            if (SetField(ref _targetPathItems, value))
            {
                OnPropertyChanged(nameof(IsTargetPathPreviewEmpty));
                OnPropertyChanged(nameof(HasTargetPathItems));
                OnPropertyChanged(nameof(TargetSelectedCount));
                OnPropertyChanged(nameof(TargetReadyCount));
                OnPropertyChanged(nameof(TargetAlreadyImportedCount));
                OnPropertyChanged(nameof(TargetConflictCount));
                OnPropertyChanged(nameof(TargetTotalSizeText));
            }
        }
    }

    public bool IsTargetPathPreviewEmpty => TargetPathItems.Count == 0;

    public bool HasTargetPathItems => TargetPathItems.Count > 0;

    public int TargetSelectedCount => PreviewItems.Count(item => item.IsSelected);

    public int TargetReadyCount => TargetPathItems.Count(item => item.IsImportReady);

    public int TargetAlreadyImportedCount =>
        TargetPathItems.Count(item => item.IsAlreadyImported);

    public int TargetConflictCount =>
        TargetPathItems.Count(item => item.IsConflictOrError) + _targetPlanErrorCount;

    public string TargetTotalSizeText
    {
        get
        {
            MediaPreviewItemViewModel[] selectedItems = PreviewItems
                .Where(item => item.IsSelected)
                .ToArray();
            long knownSize = selectedItems
                .Where(item => item.SourceMediaItem.Size.HasValue)
                .Sum(item => item.SourceMediaItem.Size!.Value);
            int unknownSizeCount = selectedItems.Count(item => !item.SourceMediaItem.Size.HasValue);
            string knownText = FormatByteSize(knownSize);
            return unknownSizeCount == 0
                ? knownText
                : $"{knownText} plus {unknownSizeCount} file(s) of unknown size";
        }
    }

    public ImportPlan? CurrentImportPlan
    {
        get => _currentImportPlan;
        private set
        {
            if (SetField(ref _currentImportPlan, value))
            {
                _importSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedSummary
    {
        get
        {
            MediaPreviewItemViewModel[] selectedItems = PreviewItems
                .Where(item => item.IsSelected)
                .ToArray();
            if (selectedItems.Length == 0)
            {
                return "0 files selected";
            }

            long? totalSize = selectedItems.All(item => item.MediaItem.Size.HasValue)
                ? selectedItems.Sum(item => item.MediaItem.Size!.Value)
                : null;
            return totalSize is { } knownSize
                ? $"{selectedItems.Length} files selected - {FormatByteSize(knownSize)} total"
                : $"{selectedItems.Length} files selected - total size unavailable";
        }
    }

    public async Task RefreshDevicesAsync()
    {
        IsLoadingDevices = true;
        DeviceStatus = "Loading devices...";

        try
        {
            string? selectedId = SelectedDevice?.Id ?? _preferredDeviceId;
            IReadOnlyList<DeviceOption> discoveredDevices =
                await _deviceDiscovery.GetDevicesAsync();

            Devices.Clear();
            foreach (DeviceOption device in discoveredDevices)
            {
                Devices.Add(device);
            }

            SelectedDevice = Devices.FirstOrDefault(device => device.Id == selectedId)
                ?? Devices.FirstOrDefault();
            DeviceStatus = Devices.Count == 0
                ? "No portable media devices found."
                : $"{Devices.Count} device(s) found.";
        }
        catch (Exception exception)
        {
            DeviceStatus = $"Devices could not be loaded: {exception.Message}";
        }
        finally
        {
            IsLoadingDevices = false;
            _loadPreviewCommand.RaiseCanExecuteChanged();
        }
    }

    internal AppUserSettings CreateUserSettings() => new()
    {
        Theme = SelectedTheme.ToString(),
        TextSize = SelectedFontSize.ToString(),
        TimeSelection = SelectedTimeSelection.ToString(),
        LastDays = LastDays,
        FromDate = FromDate is null ? null : DateOnly.FromDateTime(FromDate.Value),
        ToDate = ToDate is null ? null : DateOnly.FromDateTime(ToDate.Value),
        CaptureTimeZone = SelectedTimeZone.ToString(),
        FixedUtcOffset = FixedOffset,
        ExtensionFilter = ExtensionFilter,
        TargetTemplate = TargetTemplate,
        ExistingFilePolicy = ExistingFilePolicy.ToString(),
        DeviceId = SelectedDevice?.Id ?? _preferredDeviceId
    };

    private void RestoreSettings(AppUserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _selectedTheme = ParseEnum(settings.Theme, AppTheme.System);
        _selectedFontSize = ParseEnum(settings.TextSize, AppFontSize.Medium);
        _selectedTimeSelection = ParseEnum(settings.TimeSelection, TimeSelectionMode.Today);
        _lastDays = settings.LastDays > 0 ? settings.LastDays : 7;
        _fromDate = settings.FromDate?.ToDateTime(TimeOnly.MinValue);
        _toDate = settings.ToDate?.ToDateTime(TimeOnly.MinValue);
        _selectedTimeZone = ParseEnum(
            settings.CaptureTimeZone,
            TimeZoneSelectionMode.Local);
        _fixedOffset = string.IsNullOrWhiteSpace(settings.FixedUtcOffset)
            ? AppUserSettingsDefaults.CreateFixedUtcOffset()
            : settings.FixedUtcOffset;
        _extensionFilter = string.IsNullOrWhiteSpace(settings.ExtensionFilter)
            ? "JPG,HEIC,MOV"
            : settings.ExtensionFilter;
        _targetTemplate = string.IsNullOrWhiteSpace(settings.TargetTemplate)
            ? AppUserSettingsDefaults.CreateTargetTemplate()
            : settings.TargetTemplate;
        _existingFilePolicy = ParseEnum(
            settings.ExistingFilePolicy,
            ExistingFilePolicy.Skip);
        _preferredDeviceId = string.IsNullOrWhiteSpace(settings.DeviceId)
            ? null
            : settings.DeviceId;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out TEnum parsedValue) && Enum.IsDefined(parsedValue)
            ? parsedValue
            : defaultValue;

    public LocalCaptureDateSelectionRequest CreateDateSelectionRequest() =>
        SelectedTimeSelection switch
        {
            TimeSelectionMode.Today => new(Preset: LocalCaptureDatePreset.Today),
            TimeSelectionMode.Yesterday => new(Preset: LocalCaptureDatePreset.Yesterday),
            TimeSelectionMode.LastDays => new(LastDays: LastDays),
            TimeSelectionMode.FromTo => new(
                From: FromDate is null ? null : DateOnly.FromDateTime(FromDate.Value),
                To: ToDate is null ? null : DateOnly.FromDateTime(ToDate.Value)),
            TimeSelectionMode.All => new(Preset: LocalCaptureDatePreset.All),
            _ => throw new ArgumentOutOfRangeException(nameof(SelectedTimeSelection))
        };

    public CaptureTimeZoneSpec CreateTimeZoneSpec() =>
        CaptureTimeZoneSpec.Parse(
            SelectedTimeZone == TimeZoneSelectionMode.Local ? "local" : FixedOffset);

    public MediaExtensionSelectionRule CreateExtensionSelectionRule() =>
        MediaExtensionSelectionRule.Parse(ExtensionFilter);

    public void CancelPreview()
    {
        _previewLoadCancellation?.Cancel();
        _targetPathPlanCancellation?.Cancel();
        _importCancellation?.Cancel();
        _thumbnailLoader?.Cancel();
    }

    public async Task CancelImportAndWaitAsync()
    {
        Task completion = _importCompletionSource?.Task ?? Task.CompletedTask;
        CancelImport();
        await completion;
    }

    public static string FormatByteSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double displaySize = size;
        int unitIndex = 0;
        while (displaySize >= 1024 && unitIndex < units.Length - 1)
        {
            displaySize /= 1024;
            unitIndex++;
        }

        string format = unitIndex == 0 ? "0" : "0.#";
        return $"{displaySize.ToString(format, CultureInfo.CurrentCulture)} {units[unitIndex]}";
    }

    public static string FormatByteProgress(long transferredBytes, long? expectedBytes)
    {
        if (expectedBytes is not > 0)
        {
            return FormatByteSize(transferredBytes);
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double divisor = 1;
        int unitIndex = 0;
        while (expectedBytes.Value / divisor >= 1024 && unitIndex < units.Length - 1)
        {
            divisor *= 1024;
            unitIndex++;
        }

        string format = unitIndex == 0 ? "0" : "0.0";
        string expectedText = (expectedBytes.Value / divisor).ToString(
            format,
            CultureInfo.CurrentCulture);
        string transferredText = (transferredBytes / divisor).ToString(
            format,
            CultureInfo.CurrentCulture);
        transferredText = transferredText.PadLeft(expectedText.Length);
        string unit = units[unitIndex];
        return $"{transferredText} {unit} of {expectedText} {unit}";
    }

    private bool CanLoadPreview() =>
        SelectedDevice is not null && _settingsValid && !IsLoadingPreview && !IsImporting;

    private bool CanImportSelected() =>
        CurrentImportPlan is { Items.Count: > 0 } &&
        _previewMediaSource is not null &&
        !IsPlanningTargetPaths &&
        !IsImporting;

    private async Task ImportSelectedAsync()
    {
        ImportPlan? importPlan = CurrentImportPlan;
        IMediaSource? mediaSource = _previewMediaSource;
        if (importPlan is null || mediaSource is null || !CanImportSelected())
        {
            return;
        }

        CancellationTokenSource request = new();
        _importCancellation = request;
        TaskCompletionSource completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _importCompletionSource = completionSource;
        CancellationToken cancellationToken = request.Token;
        ExistingFilePolicy policy = ExistingFilePolicy;
        ResetImportProgress(importPlan);
        IsImporting = true;
        ImportStatus = "Importing selected media...";
        IProgress<BatchImportProgress> progress = new Progress<BatchImportProgress>(
            value => ApplyImportProgress(importPlan, request, value));

        try
        {
            BatchImportResult result = await Task.Run(
                async () => await _batchMediaImporter.ImportAsync(
                    mediaSource,
                    importPlan,
                    policy,
                    progress,
                    cancellationToken).ConfigureAwait(false));

            if (!ReferenceEquals(_importCancellation, request))
            {
                return;
            }

            ApplyImportResults(importPlan, result.Results);
            DeselectProcessedItems(result.Results);
            bool wasCancelled = result.Results.Any(
                item => item.Status == ImportResultStatus.Cancelled);
            IsImportSuccessful = !wasCancelled && ImportFailedCount == 0;
            if (wasCancelled)
            {
                ApplyCancelledImportState(result.Results);
            }
            else
            {
                IsImportFailed = ImportFailedCount > 0;
                ImportStatus = ImportFailedCount > 0
                    ? "Import completed with errors. Other files were processed where possible."
                    : "Import completed successfully.";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (ReferenceEquals(_importCancellation, request))
            {
                ApplyCancelledImportState([]);
            }
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_importCancellation, request))
            {
                int remainingCount = Math.Max(1, ImportTotalCount - ImportCompletedCount);
                ImportFailedCount = checked(ImportFailedCount + remainingCount);
                IsImportSuccessful = false;
                IsImportCancelled = false;
                IsImportFailed = true;
                ImportStatus = $"Import stopped unexpectedly: {exception.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_importCancellation, request))
            {
                _importCancellation = null;
                IsImporting = false;
            }

            if (ReferenceEquals(_importCompletionSource, completionSource))
            {
                _importCompletionSource = null;
            }

            completionSource.TrySetResult();
            request.Dispose();
        }
    }

    private void CancelImport()
    {
        CancellationTokenSource? request = _importCancellation;
        if (request is null || request.IsCancellationRequested)
        {
            return;
        }

        ImportStatus = "Cancelling import...";
        request.Cancel();
        _cancelImportCommand.RaiseCanExecuteChanged();
    }

    private void ResetImportProgress(ImportPlan importPlan)
    {
        ImportCompletedCount = 0;
        ImportTotalCount = importPlan.Items.Count;
        ImportedCount = 0;
        SkippedCount = 0;
        AlreadyImportedCount = 0;
        ImportFailedCount = 0;
        ImportProgressValue = 0;
        ImportPlanItem[] transferredItems = importPlan.Items
            .Where(item => item.Status is
                ImportPlanStatus.Ready or
                ImportPlanStatus.Renamed or
                ImportPlanStatus.WillOverwrite)
            .ToArray();
        _importExpectedBytes = transferredItems.All(item => item.MediaItem.Size is not null)
            ? transferredItems.Sum(item => item.MediaItem.Size!.Value)
            : null;
        ImportTransferredBytes = 0;
        IsImportSuccessful = false;
        IsImportCancelled = false;
        IsImportFailed = false;
        ImportByteProgress = FormatByteProgress(0, _importExpectedBytes);
        const string cancelledProgressLayoutText = "999.9 TB transferred";
        ImportByteProgressLayoutText = ImportByteProgress.Length >=
            cancelledProgressLayoutText.Length
                ? ImportByteProgress
                : cancelledProgressLayoutText;
        ImportFileProgress = importPlan.Items.Count == 0
            ? string.Empty
            : $"File 1 of {importPlan.Items.Count}: {importPlan.Items[0].MediaItem.Name}";
    }

    private void ApplyImportProgress(
        ImportPlan importPlan,
        CancellationTokenSource request,
        BatchImportProgress progress)
    {
        if (!ReferenceEquals(_importCancellation, request))
        {
            return;
        }

        if (progress.CompletedCount < ImportCompletedCount ||
            progress.TransferredBytes < ImportTransferredBytes)
        {
            return;
        }

        ImportCompletedCount = progress.CompletedCount;
        ImportTransferredBytes = progress.TransferredBytes;
        ImportProgressValue = CalculateImportProgressValue(progress);
        ImportByteProgress = FormatByteProgress(
            progress.TransferredBytes,
            _importExpectedBytes);
        if (progress.Result is null)
        {
            ImportFileProgress =
                $"File {progress.CompletedCount + 1} of {progress.TotalCount}: " +
                progress.CurrentItem.Name;
            return;
        }

        ApplyImportResultToPreview(progress.Result);
        AddImportResultToSummary(progress.Result);
        if (progress.CompletedCount < importPlan.Items.Count)
        {
            ImportPlanItem nextItem = importPlan.Items[progress.CompletedCount];
            ImportFileProgress =
                $"File {progress.CompletedCount + 1} of {progress.TotalCount}: {nextItem.MediaItem.Name}";
        }
        else
        {
            ImportFileProgress =
                $"{progress.CompletedCount} of {progress.TotalCount} files processed.";
        }
    }

    private void ApplyImportResults(
        ImportPlan importPlan,
        IReadOnlyList<ImportResult> results)
    {
        ImportCompletedCount = results.Count;
        ImportProgressValue = results.Count;
        ImportedCount = results.Count(item => item.Status == ImportResultStatus.Succeeded);
        SkippedCount = results.Count(item => item.Status == ImportResultStatus.Skipped);
        AlreadyImportedCount = results.Count(
            item => item.Status == ImportResultStatus.AlreadyImported);
        ImportFailedCount = results.Count(item => item.Status == ImportResultStatus.Failed);
        ImportTransferredBytes = results.Sum(item => item.TransferredBytes);
        ImportByteProgress = FormatByteProgress(
            ImportTransferredBytes,
            _importExpectedBytes);
        foreach (ImportResult result in results)
        {
            ApplyImportResultToPreview(result);
        }

        ImportFileProgress = $"{results.Count} of {importPlan.Items.Count} files processed.";
    }

    private void ApplyCancelledImportState(IReadOnlyList<ImportResult> results)
    {
        IsImportSuccessful = false;
        IsImportCancelled = true;
        IsImportFailed = false;
        int completedFileCount = results.Count(
            result => result.Status != ImportResultStatus.Cancelled);
        ImportResult? cancelledResult = results.FirstOrDefault(
            result => result.Status == ImportResultStatus.Cancelled);
        double partialFileProgress = cancelledResult?.ExpectedSize is > 0
            ? Math.Min(
                1,
                (double)cancelledResult.TransferredBytes /
                cancelledResult.ExpectedSize.Value)
            : 0;
        ImportProgressValue = completedFileCount + partialFileProgress;
        ImportStatus = "Import cancelled";
        ImportFileProgress =
            $"{completedFileCount} of {ImportTotalCount} files completed. " +
            "Completed files were kept; the partial transfer was discarded.";
        ImportByteProgress = $"{FormatByteSize(ImportTransferredBytes)} transferred";
    }

    private void DeselectProcessedItems(IReadOnlyList<ImportResult> results)
    {
        HashSet<string> processedMediaItemIds = results
            .Where(result => result.Status is
                ImportResultStatus.Succeeded or
                ImportResultStatus.Skipped or
                ImportResultStatus.AlreadyImported)
            .Select(result => result.MediaItem.Id)
            .ToHashSet(StringComparer.Ordinal);

        _suppressSelectionUpdates = true;
        try
        {
            foreach (MediaPreviewItemViewModel item in PreviewItems)
            {
                if (processedMediaItemIds.Contains(item.SourceMediaItem.Id))
                {
                    item.IsSelected = false;
                }
            }
        }
        finally
        {
            _suppressSelectionUpdates = false;
        }

        OnSelectionChanged();
    }

    private void AddImportResultToSummary(ImportResult result)
    {
        switch (result.Status)
        {
            case ImportResultStatus.Succeeded:
                ImportedCount++;
                break;
            case ImportResultStatus.Skipped:
                SkippedCount++;
                break;
            case ImportResultStatus.AlreadyImported:
                AlreadyImportedCount++;
                break;
            case ImportResultStatus.Failed:
                ImportFailedCount++;
                break;
            case ImportResultStatus.Cancelled:
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(result), result.Status, "Unknown import result status.");
        }
    }

    private static double CalculateImportProgressValue(BatchImportProgress progress)
    {
        if (progress.Result is not null || progress.CurrentItemExpectedBytes is not > 0)
        {
            return progress.CompletedCount;
        }

        double currentFileFraction = Math.Min(
            1,
            (double)progress.CurrentItemTransferredBytes /
            progress.CurrentItemExpectedBytes.Value);
        return progress.CompletedCount + currentFileFraction;
    }

    private void ApplyImportResultToPreview(ImportResult result)
    {
        MediaPreviewItemViewModel? previewItem = PreviewItems.FirstOrDefault(
            item => item.SourceMediaItem.Id == result.MediaItem.Id);
        previewItem?.ApplyImportResult(result);
    }

    private async Task LoadPreviewAsync()
    {
        DeviceOption? device = SelectedDevice;
        if (device is null || !_settingsValid)
        {
            return;
        }

        ClearImportStatus();
        _previewLoadCancellation?.Cancel();
        _thumbnailLoader?.Cancel();
        ClearTargetPathPreview("Select media to calculate target paths.");
        _previewMediaSource = null;
        CancellationTokenSource cancellation = new();
        _previewLoadCancellation = cancellation;
        CancellationToken cancellationToken = cancellation.Token;
        IsLoadingPreview = true;
        PreviewItems = Array.Empty<MediaPreviewItemViewModel>();
        PreviewStatus = $"Loading media from {device.DisplayLabel}...";

        try
        {
            LocalCaptureDateRange dateRange = CreateDateSelectionRequest()
                .Resolve(DateOnly.FromDateTime(DateTime.Today));
            IMediaSelectionRule selectionRule = new AllOfMediaSelectionRule(
                new LocalCaptureDateRangeSelectionRule(dateRange),
                CreateExtensionSelectionRule());
            CaptureTimeZoneSpec timeZoneSpec = CreateTimeZoneSpec();
            IMediaSource mediaSource = _mediaSourceFactory.Create(device);
            IReadOnlyList<LoadedMediaItem> mediaItems = await Task.Run(
                () => LoadMediaItemsAsync(
                    mediaSource,
                    selectionRule,
                    timeZoneSpec,
                    cancellationToken),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_previewLoadCancellation, cancellation))
            {
                return;
            }

            ThumbnailLoader newThumbnailLoader = new(mediaSource);
            _thumbnailLoader = newThumbnailLoader;
            _previewMediaSource = mediaSource;
            PreviewItems = mediaItems
                .Select(item => new MediaPreviewItemViewModel(
                    item.SourceMediaItem,
                    item.ResolvedMediaItem,
                    newThumbnailLoader,
                    OnSelectionChanged))
                .ToArray();
            PreviewStatus = PreviewItems.Count == 0
                ? "No media matched the current device and filters."
                : $"{PreviewItems.Count} media file(s), newest first. Thumbnails load as needed.";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (ReferenceEquals(_previewLoadCancellation, cancellation))
            {
                PreviewStatus = "Preview loading was cancelled.";
            }
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_previewLoadCancellation, cancellation))
            {
                PreviewItems = Array.Empty<MediaPreviewItemViewModel>();
                PreviewStatus =
                    $"Preview could not be loaded. The device may no longer be available: {exception.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_previewLoadCancellation, cancellation))
            {
                _previewLoadCancellation = null;
                IsLoadingPreview = false;
            }

            cancellation.Dispose();
        }
    }

    private async Task<IReadOnlyList<LoadedMediaItem>> LoadMediaItemsAsync(
        IMediaSource mediaSource,
        IMediaSelectionRule selectionRule,
        CaptureTimeZoneSpec timeZoneSpec,
        CancellationToken cancellationToken)
    {
        List<LoadedMediaItem> mediaItems = new();
        await foreach (MediaItem mediaItem in mediaSource
            .GetMediaItemsAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!selectionRule.IsMatch(mediaItem))
            {
                continue;
            }

            CaptureTimestamp? resolvedCaptureTime = mediaItem.CaptureTime is { } captureTime
                ? _captureTimeZoneResolver.Resolve(captureTime, timeZoneSpec)
                : null;
            mediaItems.Add(new(
                mediaItem,
                mediaItem.WithCaptureTime(resolvedCaptureTime)));
        }

        Dictionary<string, LoadedMediaItem> mediaItemsById = mediaItems
            .ToDictionary(item => item.SourceMediaItem.Id, StringComparer.Ordinal);
        IReadOnlyList<MediaItem> orderedItems = MediaPreviewOrdering.NewestFirst(
            mediaItems.Select(item => item.ResolvedMediaItem));
        return orderedItems
            .Select(item => mediaItemsById[item.Id])
            .ToArray();
    }

    private void TargetPlanningSettingChanged()
    {
        ClearImportStatus();
        ValidateSettings();
        TryUpdateResolvedCaptureTimes();
        ScheduleTargetPathPlan();
    }

    private void TryUpdateResolvedCaptureTimes()
    {
        try
        {
            CaptureTimeZoneSpec timeZoneSpec = CreateTimeZoneSpec();
            CaptureTimestamp?[] resolvedCaptureTimes = PreviewItems
                .Select(item => item.SourceMediaItem.CaptureTime is { } captureTime
                    ? _captureTimeZoneResolver.Resolve(captureTime, timeZoneSpec)
                    : null)
                .ToArray();

            for (int index = 0; index < PreviewItems.Count; index++)
            {
                PreviewItems[index].UpdateResolvedCaptureTime(resolvedCaptureTimes[index]);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            FormatException or
            CaptureTimeResolutionException)
        {
        }
    }

    private void ScheduleTargetPathPlan()
    {
        CancellationTokenSource? previousRequest = _targetPathPlanCancellation;
        _targetPathPlanCancellation = null;
        previousRequest?.Cancel();

        int selectedCount = PreviewItems.Count(item => item.IsSelected);
        if (selectedCount == 0 || _previewMediaSource is null)
        {
            IsPlanningTargetPaths = false;
            CurrentImportPlan = null;
            SetTargetPlanErrorCount(0);
            TargetPathItems = Array.Empty<TargetPathPreviewItemViewModel>();
            TargetPathStatus = selectedCount == 0
                ? "Select media to calculate target paths."
                : "Load the media preview again to calculate target paths.";
            return;
        }

        CancellationTokenSource request = new();
        _targetPathPlanCancellation = request;
        CurrentImportPlan = null;
        SetTargetPlanErrorCount(0);
        TargetPathItems = Array.Empty<TargetPathPreviewItemViewModel>();
        IsPlanningTargetPaths = true;
        TargetPathStatus = "Updating target paths...";
        _ = RefreshTargetPathPlanAfterDelayAsync(request);
    }

    private async Task RefreshTargetPathPlanAfterDelayAsync(
        CancellationTokenSource request)
    {
        CancellationToken cancellationToken = request.Token;
        try
        {
            await Task.Delay(300, cancellationToken);
            if (!ReferenceEquals(_targetPathPlanCancellation, request))
            {
                return;
            }

            IMediaSource mediaSource = _previewMediaSource
                ?? throw new InvalidOperationException("The media source is no longer available.");
            IReadOnlyList<MediaItem> selectedMediaItems = PreviewItems
                .Where(item => item.IsSelected)
                .Select(item => item.SourceMediaItem)
                .ToArray();
            CaptureTimeZoneSpec timeZoneSpec = CreateTimeZoneSpec();
            PathTemplate pathTemplate = new(TargetTemplate);
            ExistingFilePolicy policy = ExistingFilePolicy;

            TargetPathPlanResult result = await Task.Run(
                async () =>
                {
                    ImportPlan importPlan = await _importBatchPlanner.CreatePlanAsync(
                        selectedMediaItems,
                        mediaSource,
                        timeZoneSpec,
                        pathTemplate,
                        policy,
                        cancellationToken).ConfigureAwait(false);
                    TargetPathPreviewItemViewModel[] previewItems = importPlan.Items
                        .Select(item => new TargetPathPreviewItemViewModel(item))
                        .ToArray();
                    return new TargetPathPlanResult(importPlan, previewItems);
                },
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(_targetPathPlanCancellation, request))
            {
                return;
            }

            CurrentImportPlan = result.ImportPlan;
            SetTargetPlanErrorCount(0);
            TargetPathItems = result.PreviewItems;
            TargetPathStatus = "Target paths are current. No files have been copied.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_targetPathPlanCancellation, request))
            {
                CurrentImportPlan = null;
                TargetPathItems = Array.Empty<TargetPathPreviewItemViewModel>();
                SetTargetPlanErrorCount(PreviewItems.Count(item => item.IsSelected));
                TargetPathStatus = $"Target paths could not be calculated: {exception.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_targetPathPlanCancellation, request))
            {
                _targetPathPlanCancellation = null;
                IsPlanningTargetPaths = false;
            }

            request.Dispose();
        }
    }

    private void ClearTargetPathPreview(string status)
    {
        CancellationTokenSource? request = _targetPathPlanCancellation;
        _targetPathPlanCancellation = null;
        request?.Cancel();
        IsPlanningTargetPaths = false;
        CurrentImportPlan = null;
        SetTargetPlanErrorCount(0);
        TargetPathItems = Array.Empty<TargetPathPreviewItemViewModel>();
        TargetPathStatus = status;
    }

    private void ToggleTargetPathDetails() =>
        IsTargetPathDetailsVisible = !IsTargetPathDetailsVisible;

    private void SetTargetPlanErrorCount(int value)
    {
        if (_targetPlanErrorCount == value)
        {
            return;
        }

        _targetPlanErrorCount = value;
        OnPropertyChanged(nameof(TargetConflictCount));
    }

    private void PreviewSettingChanged()
    {
        ValidateSettings();
        InvalidatePreview("Preview filters changed. Load the preview again.");
    }

    private void InvalidatePreview(string status)
    {
        ClearImportStatus();
        CancellationTokenSource? cancellation = _previewLoadCancellation;
        _previewLoadCancellation = null;
        cancellation?.Cancel();
        _thumbnailLoader?.Cancel();
        _thumbnailLoader = null;
        _previewMediaSource = null;
        ClearTargetPathPreview("Select media to calculate target paths.");
        IsLoadingPreview = false;
        PreviewItems = Array.Empty<MediaPreviewItemViewModel>();
        PreviewStatus = status;
        _loadPreviewCommand.RaiseCanExecuteChanged();
    }

    private void SelectAll() => SetAllSelections(true);

    private void SelectNone() => SetAllSelections(false);

    private void SetAllSelections(bool selected)
    {
        _suppressSelectionUpdates = true;
        try
        {
            foreach (MediaPreviewItemViewModel item in PreviewItems)
            {
                item.IsSelected = selected;
            }
        }
        finally
        {
            _suppressSelectionUpdates = false;
        }

        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        if (_suppressSelectionUpdates)
        {
            return;
        }

        if (!IsImporting)
        {
            ClearImportStatus();
        }

        OnPropertyChanged(nameof(SelectedSummary));
        OnPropertyChanged(nameof(TargetSelectedCount));
        OnPropertyChanged(nameof(TargetTotalSizeText));
        _selectNoneCommand.RaiseCanExecuteChanged();
        ScheduleTargetPathPlan();
    }

    private void ClearImportStatus()
    {
        ImportStatus = string.Empty;
        IsImportSuccessful = false;
        IsImportCancelled = false;
        IsImportFailed = false;
    }

    private void RaisePreviewCommandStateChanged()
    {
        _loadPreviewCommand.RaiseCanExecuteChanged();
        _selectAllCommand.RaiseCanExecuteChanged();
        _selectNoneCommand.RaiseCanExecuteChanged();
    }

    private void ValidateSettings()
    {
        _settingsValid = false;
        try
        {
            LocalCaptureDateSelectionRequest request = CreateDateSelectionRequest();
            request.Resolve(DateOnly.FromDateTime(DateTime.Today));
            CreateTimeZoneSpec();
            CreateExtensionSelectionRule();
            _settingsValid = true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException)
        {
            ValidationStatus = exception.Message;
        }

        if (_settingsValid)
        {
            try
            {
                _ = new PathTemplate(TargetTemplate);
                ValidationStatus = "Settings are valid.";
            }
            catch (Exception exception) when (
                exception is ArgumentException or FormatException)
            {
                ValidationStatus = exception.Message;
            }
        }

        _loadPreviewCommand.RaiseCanExecuteChanged();
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

    private sealed record LoadedMediaItem(
        MediaItem SourceMediaItem,
        MediaItem ResolvedMediaItem);

    private sealed record TargetPathPlanResult(
        ImportPlan ImportPlan,
        IReadOnlyList<TargetPathPreviewItemViewModel> PreviewItems);
}
