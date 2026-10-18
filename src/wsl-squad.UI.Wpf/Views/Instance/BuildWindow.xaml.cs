using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class BuildWindow : ChromeWindow
{
    private readonly BuildViewModel _viewModel;

    public BuildWindow(BuildViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => DialogResult = true;
        ContentRendered += (_, _) => _ = TxtName.Focus();
    }

    private void OnRoadSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: >= 0 } box && DataContext is BuildViewModel viewModel)
        {
            viewModel.RoadIndex = box.SelectedIndex;
        }
    }

    private void OnDockerfileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: >= 0 } box && DataContext is BuildViewModel viewModel)
        {
            viewModel.DockerfileIndex = box.SelectedIndex;
        }
    }

    private void OnImageSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: >= 0 } box && DataContext is BuildViewModel viewModel)
        {
            viewModel.ImageIndex = box.SelectedIndex;
        }
    }

    private void OnRecipeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: >= 0 } box && DataContext is BuildViewModel viewModel)
        {
            viewModel.RecipeIndex = box.SelectedIndex;
        }
    }

    private void OnOnboardingSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedIndex: >= 0 } box && DataContext is BuildViewModel viewModel)
        {
            viewModel.OnboardingIndex = box.SelectedIndex;
        }
    }

    private void OnUploadDockerfile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "A Dockerfile for the build",
            Filter = "Dockerfiles (Dockerfile*)|Dockerfile*|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.AddDockerfile(dialog.FileName);
        }
    }

    private void OnUploadOnboarding(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "An onboarding script for the build",
            Filter = "Shell scripts (*.sh)|*.sh|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.AddOnboarding(dialog.FileName);
        }
    }

    private void OnUploadImage(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "A Docker image for the build",
            Filter = "Docker images (*.tar)|*.tar|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.AddImage(dialog.FileName);
        }
    }

    private void OnDownloadDockerfile(object sender, RoutedEventArgs e)
    {
        Download("Dockerfiles");
    }

    private void OnDownloadOnboarding(object sender, RoutedEventArgs e)
    {
        Download("Onboardings");
    }

    private void OnDownloadImage(object sender, RoutedEventArgs e)
    {
        Download("DockerImages");
    }

    private void Download(string kind)
    {
        string proposed = _viewModel.DownloadName(kind);

        if (proposed.Length == 0)
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = $"Save '{proposed}'",
            FileName = proposed,
            Filter = "All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.ExportRecipeTo(kind, dialog.FileName);
        }
    }

    private void OnEditDockerfile(object sender, RoutedEventArgs e)
    {
        _viewModel.EditSource("Dockerfiles");
    }

    private void OnEditOnboarding(object sender, RoutedEventArgs e)
    {
        _viewModel.EditSource("Onboardings");
    }

    private void OnEditRecipe(object sender, RoutedEventArgs e)
    {
        _viewModel.EditSource("Recipes");
    }

    private void OnDeleteSource(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string listName })
        {
            _viewModel.DeleteSelected(listName);
        }
    }

    private void OnNewRecipe(object sender, RoutedEventArgs e)
    {
        _viewModel.NewRecipe();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BuildViewModel.NameError):
                ShowError(GuiNameError, _viewModel.NameError);
                break;
            case nameof(BuildViewModel.UserError):
                ShowError(GuiUserError, _viewModel.UserError);
                break;
            case nameof(BuildViewModel.ImageError):
                ShowError(GuiImageError, _viewModel.ImageError);
                break;
            case nameof(BuildViewModel.RegistryError):
                ShowError(GuiRegistryError, _viewModel.RegistryError);
                break;
            case nameof(BuildViewModel.RecipeError):
                ShowError(GuiRecipeError, _viewModel.RecipeError);
                break;
            default:
                break;
        }
    }

    private static void ShowError(TextBlock block, string message)
    {
        block.Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
}
