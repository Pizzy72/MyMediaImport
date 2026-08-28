using MyMediaImport.Core;
using System.Windows;
using System.Windows.Threading;

namespace MyMediaImport.App;

public partial class PathTemplateHelpWindow : Window
{
    private readonly DispatcherTimer _copyStatusTimer;

    public PathTemplateHelpWindow()
    {
        InitializeComponent();
        DataContext = PathTemplate.SupportedPlaceholders;
        _copyStatusTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
        _copyStatusTimer.Tick += CopyStatusTimer_OnTick;
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string placeholder })
        {
            return;
        }

        System.Windows.Clipboard.SetText(placeholder);
        CopyStatusText.Visibility = Visibility.Visible;
        _copyStatusTimer.Stop();
        _copyStatusTimer.Start();
    }

    private void CopyStatusTimer_OnTick(object? sender, EventArgs e)
    {
        _copyStatusTimer.Stop();
        CopyStatusText.Visibility = Visibility.Collapsed;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void Window_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }
}
