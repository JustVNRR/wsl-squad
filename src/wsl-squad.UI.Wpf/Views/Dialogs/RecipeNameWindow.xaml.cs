using System.ComponentModel;
using System.Windows;

using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Dialogs;

public partial class RecipeNameWindow : ChromeWindow
{
    public RecipeNameWindow(RecipeNameViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => _ = TxtRecipeName.Focus();
    }


    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecipeNameViewModel.NameError))
        {
            string error = ((RecipeNameViewModel)sender!).NameError;
            GuiNameError.Visibility = error.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
