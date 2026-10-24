using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Instance;

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
