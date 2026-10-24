using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Instance;

public partial class DeleteArchiveWindow : ChromeWindow
{
    public DeleteArchiveWindow(DeleteArchiveViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => TxtName.Focus();
    }

}
