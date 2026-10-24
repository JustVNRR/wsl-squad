using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Dialogs;

public partial class RecipeNameWindow : ChromeWindow
{
    public RecipeNameWindow(RecipeNameViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => TxtRecipeName.Focus();
    }
}
