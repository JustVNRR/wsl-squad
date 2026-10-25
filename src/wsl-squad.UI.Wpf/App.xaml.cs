using System.IO;
using System.Windows;

using WslSquad.Core.Repositories;
using WslSquad.Core.Services;
using WslSquad.Presentation.Gui.ViewModels;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Wpf.Services;
using WslSquad.UI.Wpf.Views;

namespace WslSquad.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!WslService.IsAvailable())
        {
            MessageBox.Show(
                Localizer.Instance["StartupWslMissing"],
                Localizer.Instance["AppName"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);

            return;
        }

        string assets = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wsl-squad");
        Directory.CreateDirectory(assets);

        CatalogBootstrap.Ensure(assets);
        Localizer.Instance.LoadLanguages(Path.Combine(assets, "languages"));
        Localizer.Instance.UsePreferences(new ConfigRepository(assets));
        FontFaceResolver.LoadCatalog(assets);

        WpfThemeService theme = new(assets);
        theme.ApplyPalette();
        theme.ApplyFace();

        ServiceFactory factory = new();
        IFleetService fleet = factory.CreateFleetService(assets);
        fleet.GetRoot();

        MainWindowViewModel viewModel = new(
            factory.CreateInstanceService(assets),
            fleet,
            theme,
            factory.CreatePackCatalogService(assets),
            new WpfDialogs(),
            factory.CreateLoggerService());

        MainWindow window = new() { DataContext = viewModel };
        MainWindow = window;

        viewModel.RefreshCommand.Execute(null);
        window.Show();
    }
}
