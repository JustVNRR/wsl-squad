using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Instance;

public partial class ArchiveWindow : ChromeWindow
{
    public ArchiveWindow(ArchiveViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
    }

}
