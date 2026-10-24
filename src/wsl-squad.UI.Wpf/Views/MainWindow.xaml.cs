using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views;

public partial class MainWindow : ChromeWindow
{
    private MainWindowViewModel? _viewModel;
    private bool _refitQueued;

    public MainWindow()
    {
        InitializeComponent();
        Dress();

        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
        PreviewKeyDown += OnPreviewKeyDown;
        ContentRendered += OnContentRendered;
        DpiChanged += (_, _) => FitCeiling();
        MouseLeftButtonUp += (_, _) => FitCeiling();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.LookChanged -= OnLookChanged;
            _viewModel.Rows.CollectionChanged -= OnRowsChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.LookChanged += OnLookChanged;
            _viewModel.Rows.CollectionChanged += OnRowsChanged;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.LookChanged -= OnLookChanged;
            _viewModel.Rows.CollectionChanged -= OnRowsChanged;
        }
    }

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        QueueRefit();
    }

    private void OnLookChanged(object? sender, EventArgs e)
    {
        Dress();
        QueueRefit();
    }

    private void QueueRefit()
    {
        if (_refitQueued)
        {
            return;
        }

        _refitQueued = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(() =>
            {
                _refitQueued = false;
                RefitWindow();
            }));
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && Keyboard.FocusedElement is not TextBox)
        {
            Close();
        }
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        FitCeiling();
        LstInstances.Focus();
    }

    private void RefitWindow()
    {
        SizeToContent = SizeToContent.Manual;
        Width = double.NaN;
        Height = double.NaN;
        LstInstances.Width = double.NaN;

        Grid.SetIsSharedSizeScope(LstInstances, false);
        Grid.SetIsSharedSizeScope(LstInstances, true);

        LstInstances.InvalidateMeasure();
        InvalidateMeasure();
        UpdateLayout();

        SizeToContent = SizeToContent.WidthAndHeight;
        UpdateLayout();
        FitCeiling();
        KeepOnScreen();
    }

    private void KeepOnScreen()
    {
        if (!TryGetWorkArea(this, out Rect area))
        {
            area = SystemParameters.WorkArea;
        }

        if (ActualWidth > 0 && Left + ActualWidth > area.Right)
        {
            Left = Math.Max(area.Left, area.Right - ActualWidth);
        }

        if (ActualHeight > 0 && Top + ActualHeight > area.Bottom)
        {
            Top = Math.Max(area.Top, area.Bottom - ActualHeight);
        }
    }

    private void FitCeiling()
    {
        Rect area = TryGetWorkArea(this, out Rect monitor) ? monitor : SystemParameters.WorkArea;
        MaxHeight = Math.Max(360, area.Height - 40);
    }
}
