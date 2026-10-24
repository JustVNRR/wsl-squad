using System.ComponentModel;
using System.Windows;

using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Dialogs;

public partial class PackDuplicateWindow : ChromeWindow
{
    public PackDuplicateWindow(PackDuplicateViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) =>
        {
            _ = TxtDupName.Focus();
            TxtDupName.SelectAll();
        };
    }


    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackDuplicateViewModel.NameError))
        {
            string error = ((PackDuplicateViewModel)sender!).NameError;
            GuiNameError.Visibility = error.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
