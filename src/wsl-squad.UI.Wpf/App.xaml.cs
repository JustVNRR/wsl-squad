using System.IO;
using System.Windows;

using wsl_squad.Core.Services;
using wsl_squad.Presentation.Gui.ViewModels;
using wsl_squad.UI.Wpf.Services;

namespace wsl_squad.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!WslService.IsAvailable())
        {
            _ = MessageBox.Show(
                "WSL 2 is required but was not detected." + Environment.NewLine + Environment.NewLine +
                "Install it from an elevated PowerShell:" + Environment.NewLine +
                "    wsl --install" + Environment.NewLine + Environment.NewLine +
                "Restart Windows if prompted, then start WSL Squad again." + Environment.NewLine + Environment.NewLine +
                "https://learn.microsoft.com/windows/wsl/install",
                "WSL Squad",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);

            return;
        }

        string assets = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wsl-squad");
        _ = Directory.CreateDirectory(assets);

        CatalogBootstrap.Ensure(assets);
        FontFaceResolver.LoadCatalog(assets);

        WpfThemeService theme = new(assets);
        theme.ApplyPalette();
        theme.ApplyFace();

        ServiceFactory factory = new();
        string root = factory.ResolveRoot(assets, assets);

        MainWindowViewModel viewModel = new(
            factory.CreateInstanceService(root, assets, assets),
            factory.CreateFleetService(root, assets, assets),
            theme,
            factory.CreatePackCatalogService(assets, assets),
            new WpfDialogs(),
            new WpfBuildLauncher(),
            factory.CreateLoggerService());

        MainWindow window = new() { DataContext = viewModel };
        MainWindow = window;

        viewModel.RefreshCommand.Execute(null);
        window.Show();
    }
}
