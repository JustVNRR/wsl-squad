using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf;

public partial class MainWindow : Views.ChromeWindow
{
    private MainWindowViewModel? _viewModel;
    private bool _recreating;
    private bool _refitQueued;
    private string? _lastRowSignature;

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
                string signature = RowSignature();

                if (_lastRowSignature is not null &&
                    !string.Equals(signature, _lastRowSignature, StringComparison.Ordinal))
                {
                    RecreateWindow();
                }
                else
                {
                    _lastRowSignature = signature;
                }
            }));
    }

    private void RecreateWindow()
    {
        if (_recreating)
        {
            return;
        }

        _recreating = true;
        MainWindow replacement = new()
        {
            DataContext = DataContext,
            AnimateOnShow = false,
            Opacity = 1,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        Application.Current.MainWindow = replacement;
        replacement.Show();
        Close();
    }

    private void OnLookChanged(object? sender, EventArgs e)
    {
        RecreateWindow();
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
        _lastRowSignature = RowSignature();
        FitCeiling();
        LstInstances.Focus();
    }

    private string RowSignature()
    {
        return _viewModel is null
            ? string.Empty
            : string.Join(
                '\n',
                _viewModel.Rows.Select(row =>
                    $"{row.Name}|{row.Size}|{row.IsArchive}|{string.Join(",", row.Commands.Select(command => command.Kind))}"));
    }

    private void FitCeiling()
    {
        MaxHeight = Math.Max(360, SystemParameters.WorkArea.Height - 40);
    }
}
