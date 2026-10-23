using wsl_squad.Presentation.Gui.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Dialogs;

public partial class ErrorWindow : ChromeWindow
{
    public ErrorWindow(ErrorViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;
    }

}
