using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Instance;

public partial class RestoreWindow : ChromeWindow
{
    public RestoreWindow(RestoreViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) =>
        {
            TxtName.Focus();
            TxtName.SelectAll();
        };
    }
}
