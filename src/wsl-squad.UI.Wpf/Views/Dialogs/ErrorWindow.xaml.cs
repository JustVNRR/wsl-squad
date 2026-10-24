using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Dialogs;

public partial class ErrorWindow : ChromeWindow
{
    public ErrorWindow(ErrorViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;
    }

}
