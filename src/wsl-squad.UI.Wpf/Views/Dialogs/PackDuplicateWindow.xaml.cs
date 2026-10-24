using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views.Dialogs;

public partial class PackDuplicateWindow : ChromeWindow
{
    public PackDuplicateWindow(PackDuplicateViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) =>
        {
            TxtDupName.Focus();
            TxtDupName.SelectAll();
        };
    }
}
