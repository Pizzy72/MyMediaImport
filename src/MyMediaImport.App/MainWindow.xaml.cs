using MyMediaImport.Core;
using MyMediaImport.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace MyMediaImport.App;

public partial class MainWindow : Window
{
    private readonly AppThemeService _themeService;
    private readonly MainWindowViewModel _viewModel;
    private readonly IUserSettingsStore _settingsStore;
    private readonly AppUserSettings _loadedSettings;
    private WindowPlacementSettings? _windowSettings;
    private PathTemplateHelpWindow? _pathTemplateHelpWindow;
    private bool _isWaitingForImportCancellation;
    private bool _canClose;

    public MainWindow()
    {
        InitializeComponent();
        _settingsStore = JsonUserSettingsStore.CreateForCurrentUser();
        AppUserSettings settings = _settingsStore.Load();
        _loadedSettings = settings;
        _windowSettings = settings.MainWindow;
        IDeviceDiscoveryService deviceDiscovery = new PortableDeviceDiscoveryService(
            new WpdPortableDeviceDiscovery());
        IMediaSourceFactory mediaSourceFactory = new WpdMediaSourceFactory();
        IImportFileSystem importFileSystem = new LocalImportFileSystem();
        _themeService = new AppThemeService();
        _viewModel = new MainWindowViewModel(
            deviceDiscovery,
            mediaSourceFactory,
            importFileSystem,
            _themeService,
            settings);
        DataContext = _viewModel;
        SourceInitialized += MainWindow_OnSourceInitialized;
    }

    private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowSettingsHelper.Apply(this, _loadedSettings.MainWindow);
        _themeService.ApplyTitleBar(this, _viewModel.SelectedTheme);
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshDevicesAsync();

    private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (_canClose || !_viewModel.IsImporting)
        {
            _windowSettings = WindowSettingsHelper.Capture(this, _windowSettings);
            return;
        }

        e.Cancel = true;
        if (_isWaitingForImportCancellation)
        {
            return;
        }

        _isWaitingForImportCancellation = true;
        await _viewModel.CancelImportAndWaitAsync();
        _canClose = true;
        Close();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _viewModel.CancelPreview();
        try
        {
            AppUserSettings settings = _viewModel.CreateUserSettings() with
            {
                MainWindow = _windowSettings
            };
            _settingsStore.Save(settings);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            // Closing the application must not be prevented by a settings write failure.
        }
    }

    private void TargetTemplateHelpButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_pathTemplateHelpWindow is not null)
        {
            _pathTemplateHelpWindow.Activate();
            return;
        }

        PathTemplateHelpWindow helpWindow = new()
        {
            Owner = this
        };
        _pathTemplateHelpWindow = helpWindow;
        helpWindow.Closed += (_, _) => _pathTemplateHelpWindow = null;
        helpWindow.SourceInitialized += (_, _) =>
            _themeService.ApplyTitleBar(helpWindow, _viewModel.SelectedTheme);
        helpWindow.Show();
    }

    private void ChooseSourceFolder_OnClick(object sender, RoutedEventArgs e)
    {
        IFolderMediaSource? source = _viewModel.CreateFolderSource();
        if (source is null)
        {
            System.Windows.MessageBox.Show(this,
                "Select a device that supports folder browsing first.",
                "Source folder", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SourceFolderWindow dialog = new(source, _viewModel.SourceFolderSegments) { Owner = this };
        dialog.SourceInitialized += (_, _) =>
            _themeService.ApplyTitleBar(dialog, _viewModel.SelectedTheme);
        if (dialog.ShowDialog() == true)
        {
            _viewModel.SelectSourceFolder(dialog.SelectedPath);
        }
    }
}
