using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class ManagePacksWindow : ChromeWindow
{
    public ManagePacksWindow(ManagePacksViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
    }

}
