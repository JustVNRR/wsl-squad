using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.UI.Wpf.Views;

public partial class SettingsWindow : ChromeWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => DialogResult = true;
    }
}
