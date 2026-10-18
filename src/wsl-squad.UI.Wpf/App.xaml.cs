using System.IO;
using System.Windows;

using wsl_squad.Core.Services;
using wsl_squad.Presentation.Drawing;
using wsl_squad.Presentation.ViewModels;
using wsl_squad.UI.Wpf.Services;

namespace wsl_squad.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
            factory.CreateInstanceService(root, assets, assets, IconPainter.Draw),
            factory.CreateFleetService(root, assets, assets, IconPainter.Draw),
            theme,
            factory.CreatePackCatalogService(assets, assets),
            new WpfDialogs(),
            factory.CreateLoggerService());

        MainWindow window = new() { DataContext = viewModel };
        MainWindow = window;

        viewModel.RefreshCommand.Execute(null);
        window.Show();
    }
}
