using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Instance;

public partial class RemoveWindow : ChromeWindow
{
    public RemoveWindow(RemoveViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => ChkArchive.Focus();
    }

}
