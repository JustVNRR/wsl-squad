using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class DockerRegisterWindow : ChromeWindow
{
    public DockerRegisterWindow(DockerRegisterViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
    }

}
