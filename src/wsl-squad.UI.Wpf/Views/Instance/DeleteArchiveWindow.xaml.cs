using wsl_squad.Presentation.Gui.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class DeleteArchiveWindow : ChromeWindow
{
    public DeleteArchiveWindow(DeleteArchiveViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => _ = TxtName.Focus();
    }

}
