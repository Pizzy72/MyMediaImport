using System.Configuration;
using System.Data;
using System.Windows;

namespace MyMediaImport.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName =
        @"Local\MyMediaImport.App.7A4154F2-CCCF-4CB8-96D2-35D36D81A64E";

    private SingleInstanceService? _singleInstanceService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SingleInstanceService singleInstanceService = new(SingleInstanceMutexName);
        if (!singleInstanceService.TryAcquire())
        {
            singleInstanceService.ActivateExistingInstance();
            singleInstanceService.Dispose();
            Shutdown();
            return;
        }

        _singleInstanceService = singleInstanceService;
        MainWindow mainWindow = new();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceService?.Dispose();
        _singleInstanceService = null;
        base.OnExit(e);
    }
}

