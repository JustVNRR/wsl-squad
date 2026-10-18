using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Dialogs;

public partial class RootMoveWindow : ChromeWindow
{
    public RootMoveWindow(RootMoveViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) =>
        {
            _ = TxtNewRoot.Focus();
            TxtNewRoot.SelectAll();
        };
    }

}
