using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

using Microsoft.Win32;

using wsl_squad.Presentation.ViewModels;
using wsl_squad.UI.Wpf.Services;
using wsl_squad.UI.Wpf.Views.Dialogs;

namespace wsl_squad.UI.Wpf.Views;

public partial class SettingsWindow : ChromeWindow
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => DialogResult = true;
        BtnGuiRootEdit.Click += (_, _) => _viewModel.EditRoot();
        BtnGuiFontUpload.Click += (_, _) => UploadFont();
        LstGuiFonts.SelectionChanged += (_, _) =>
        {
            if (LstGuiFonts.SelectedIndex >= 0)
            {
                _viewModel.FontIndex = LstGuiFonts.SelectedIndex;
            }
        };
        LstGuiSizes.SelectionChanged += (_, _) =>
        {
            if (LstGuiSizes.SelectedIndex >= 0)
            {
                _viewModel.SizeIndex = LstGuiSizes.SelectedIndex;
            }
        };
        LstGuiColours.SelectionChanged += (_, _) =>
        {
            if (LstGuiColours.SelectedIndex >= 0)
            {
                _viewModel.ColourIndex = LstGuiColours.SelectedIndex;
            }
        };
        LstGuiPacks.SelectionChanged += (_, _) =>
        {
            if (LstGuiPacks.SelectedIndex >= 0)
            {
                _viewModel.PackIndex = LstGuiPacks.SelectedIndex;
            }
        };
    }

    private void UploadFont()
    {
        OpenFileDialog dialog = new()
        {
            Title = "A font file for the window",
            Filter = "Fonts (*.ttf;*.otf)|*.ttf;*.otf",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _ = _viewModel.UploadFont(dialog.FileName);
        }
    }

    private void OnUploadTheme(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "A theme file for the windows",
            Filter = "Themes (*.xaml)|*.xaml",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string name = Path.GetFileNameWithoutExtension(dialog.FileName);

        if (!LoadsAsTheme(dialog.FileName))
        {
            _viewModel.FailTheme("The chosen file is not a theme.");

            return;
        }

        _viewModel.ClearThemeError();

        if (_viewModel.HasTheme(name))
        {
            ConfirmViewModel confirm = new(
                "Replace theme",
                $"Replace the theme '{name}' with the chosen file?",
                null);

            if (DialogHost.Show(new ConfirmWindow(confirm)) != true)
            {
                return;
            }
        }

        _viewModel.UploadTheme(dialog.FileName);
    }

    private void OnExportTheme(object sender, RoutedEventArgs e)
    {
        string name = _viewModel.SelectedColour;
        SaveFileDialog dialog = new()
        {
            Title = $"Save '{_viewModel.ThemeFileFor(name)}'",
            FileName = _viewModel.ThemeFileFor(name),
            Filter = "All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.ExportTheme(new ExportThemeDto(_viewModel.ThemePathFor(name), dialog.FileName));
        }
    }

    private void OnDeleteTheme(object sender, RoutedEventArgs e)
    {
        string name = _viewModel.SelectedColour;

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        ConfirmViewModel confirm = new("Delete theme", $"Remove '{name}'?", null);

        if (DialogHost.Show(new ConfirmWindow(confirm)) == true)
        {
            _viewModel.DeleteTheme(name);
        }
    }

    private void OnOpenTheme(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenTheme(_viewModel.SelectedColour);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.ConfigError):
                ShowError(GuiConfigError, _viewModel.ConfigError);
                break;
            case nameof(SettingsViewModel.ThemeError):
                ShowError(GuiThemeError, _viewModel.ThemeError);
                break;
            case nameof(SettingsViewModel.CatalogueError):
                ShowError(GuiCatalogueError, _viewModel.CatalogueError);
                break;
            case nameof(SettingsViewModel.FontIndex):
            case nameof(SettingsViewModel.SizeIndex):
                FontFamily = FontFaceResolver.Resolve(_viewModel.SelectedFont);
                FontSize = _viewModel.SelectedSize;
                break;
            default:
                break;
        }
    }

    private static bool LoadsAsTheme(string path)
    {
        try
        {
            using XmlReader reader = XmlReader.Create(path);
            object? loaded = XamlReader.Load(reader);

            return loaded is ResourceDictionary;
        }
        catch (XamlParseException)
        {
            return false;
        }
        catch (XmlException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void ShowError(TextBlock block, string message)
    {
        block.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
}
