using System.ComponentModel;
using System.Windows;

using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class DuplicateWindow : ChromeWindow
{
    public DuplicateWindow(DuplicateViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) =>
        {
            _ = TxtName.Focus();
            TxtName.SelectAll();
        };
    }


    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DuplicateViewModel.NameError))
        {
            string error = ((DuplicateViewModel)sender!).NameError;
            GuiNameError.Visibility = error.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
