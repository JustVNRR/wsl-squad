using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

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
            _ = TxtName.Focus();
            TxtName.SelectAll();
        };
    }

}
