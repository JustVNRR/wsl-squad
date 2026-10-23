using wsl_squad.Presentation.Gui.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class CompactWindow : ChromeWindow
{
    public CompactWindow(CompactViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => _ = ChkArchive.Focus();
    }

}
