using System.Collections.ObjectModel;
using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Abstractions;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class BuildViewModel : BaseViewModel
{
    private readonly IInstanceService _instance;
    private readonly IDialogService _dialogs;
    private readonly List<WslBuildSourceDto> _dockerfiles;
    private readonly List<WslBuildSourceDto> _onboardings;
    private readonly List<WslBuildSourceDto> _recipes;
    private readonly List<WslDockerImageDto> _images;
    private bool _nameOk;
    private bool _userOk;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _nameError = string.Empty;

    [ObservableProperty]
    private string _user;

    [ObservableProperty]
    private string _userError = string.Empty;

    [ObservableProperty]
    private int _roadIndex;

    [ObservableProperty]
    private int _dockerfileIndex = -1;

    [ObservableProperty]
    private int _onboardingIndex = -1;

    [ObservableProperty]
    private int _imageIndex = -1;

    [ObservableProperty]
    private int _recipeIndex = -1;

    [ObservableProperty]
    private string _registryRef = string.Empty;

    [ObservableProperty]
    private string _imageError = string.Empty;

    [ObservableProperty]
    private string _registryError = string.Empty;

    [ObservableProperty]
    private string _recipeError = string.Empty;

    [ObservableProperty]
    private bool _saveImage;

    [ObservableProperty]
    private bool _registerDocker = true;

    [ObservableProperty]
    private bool _runOnboarding = true;

    [ObservableProperty]
    private bool _showDockerfile = true;

    [ObservableProperty]
    private bool _showImage;

    [ObservableProperty]
    private bool _showRegistry;

    [ObservableProperty]
    private bool _showRecipe;

    [ObservableProperty]
    private bool _showGeneralOptions = true;

    [ObservableProperty]
    private bool _canTrashDockerfile;

    [ObservableProperty]
    private bool _canTrashOnboarding;

    [ObservableProperty]
    private bool _canTrashImage;

    [ObservableProperty]
    private bool _canTrashRecipe;

    [ObservableProperty]
    private bool _canDownloadDockerfile;

    [ObservableProperty]
    private bool _canDownloadOnboarding;

    [ObservableProperty]
    private bool _canDownloadImage;

    public BuildViewModel(IInstanceService instance, IDialogService dialogs)
    {
        _instance = instance;
        _dialogs = dialogs;
        _dockerfiles = [];
        _onboardings = [];
        _recipes = [];
        _images = [];

        try
        {
            GetBuildSourcesResponseDto sources = instance.GetBuildSources();
            _dockerfiles = [.. sources.Dockerfiles];
            _onboardings = [.. sources.Onboardings];
            _recipes = [.. sources.Recipes];
            _images = [.. instance.GetAllSavedImages()];
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Partial build sources loaded: {ex.Message}");
        }

        _dockerfileIndex = _dockerfiles.Count > 0 ? 0 : -1;
        _onboardingIndex = _onboardings.Count > 0 ? 0 : -1;
        _imageIndex = _images.Count > 0 ? 0 : -1;
        _recipeIndex = _recipes.Count > 0 ? 0 : -1;

        _user = NameRules.ProposeUserName();

        CreateCommand = new RelayCommand(Create, () => _nameOk && _userOk);

        ValidateName();
        ValidateUser();
        SyncLists();
    }

    public string Lead => "NEW WSL DISTRIBUTION";

    public ObservableCollection<string> DockerfileNames { get; } = [];

    public ObservableCollection<string> OnboardingNames { get; } = [];

    public ObservableCollection<string> ImageNames { get; } = [];

    public ObservableCollection<string> RecipeNames { get; } = [];

    public BuildDto? Result { get; private set; }

    public IRelayCommand CreateCommand { get; }

    partial void OnNameChanged(string value)
    {
        ValidateName();
    }

    partial void OnUserChanged(string value)
    {
        ValidateUser();
    }

    partial void OnRunOnboardingChanged(bool value)
    {
        ValidateUser();
    }

    partial void OnRoadIndexChanged(int value)
    {
        RoadMoved();
    }

    partial void OnDockerfileIndexChanged(int value)
    {
        SyncButtons();
    }

    partial void OnOnboardingIndexChanged(int value)
    {
        SyncButtons();
    }

    partial void OnImageIndexChanged(int value)
    {
        SyncButtons();
    }

    partial void OnRecipeIndexChanged(int value)
    {
        SyncButtons();
    }

    public void AddDockerfile(string source)
    {
        AddRecipe("dockerfiles", source, "Dockerfile", "Dockerfiles");
    }

    public void AddOnboarding(string source)
    {
        AddRecipe("onboardings", source, "onboarding.sh", "Onboardings");
    }

    public void AddImage(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        AddRecipe("dockerimages", source, Path.GetFileName(source), "DockerImages");
    }

    public void DeleteSelected(string listName)
    {
        string title = listName switch
        {
            "Dockerfiles" => "Delete Dockerfile",
            "Onboardings" => "Delete onboarding shell",
            "DockerImages" => "Delete Docker image",
            "Recipes" => "Delete recipe",
            _ => string.Empty,
        };

        object? row = SelectedRow(listName);

        if (title.Length == 0 || row is null || !IsUploaded(row))
        {
            return;
        }

        if (_dialogs.Confirm(title, $"Remove '{NameOf(row)}'?"))
        {
            RemoveRecipe(listName);
        }
    }

    public void NewRecipe()
    {
        string? chosen = _dialogs.ShowRecipeName();

        if (chosen is null)
        {
            return;
        }

        try
        {
            WslBuildSourceDto row = _instance.CreateRecipe(new NewRecipeDto { Name = chosen });
            _recipes.Add(row);
            RecipeIndex = _recipes.Count - 1;
            RecipeError = string.Empty;
            SyncLists();
        }
        catch (InvalidOperationException ex)
        {
            RecipeError = ex.Message;
        }
    }

    public void EditSource(string listName)
    {
        object? row = SelectedRow(listName);

        if (row is null)
        {
            return;
        }

        try
        {
            _instance.OpenInEditor(PathOf(row));
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Failed to open editor: {ex.Message}");
        }
    }

    public string DownloadName(string listName)
    {
        object? row = SelectedRow(listName);

        if (row is null)
        {
            return string.Empty;
        }

        string path = PathOf(row);
        string baseName = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
        int fingerprint = baseName.LastIndexOf('-');

        if (fingerprint >= 0 && baseName.Length - fingerprint - 1 == 8)
        {
            baseName = baseName[..fingerprint];
        }

        return baseName + Path.GetExtension(path);
    }

    public void ExportRecipeTo(string listName, string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        object? row = SelectedRow(listName);

        if (row is null)
        {
            return;
        }

        try
        {
            _instance.ExportRecipe(new ExportRecipeDto
            {
                Path = PathOf(row),
                Target = target,
            });
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Failed to export recipe to '{target}': {ex.Message}");
        }
    }

    private void Create()
    {
        ValidateName();
        if (RunOnboarding)
        {
            ValidateUser();
        }
        else
        {
            _userOk = true;
        }

        ImageError = RoadIndex == 1 && ImageIndex < 0 ? "Upload a Docker image first." : string.Empty;
        RecipeError = RoadIndex == 3 && RecipeIndex < 0 ? "Create a recipe first with the + button." : string.Empty;
        RegistryError = string.Empty;

        if (RoadIndex == 2)
        {
            string reference = RegistryRef.Trim();

            if (reference.Length == 0)
            {
                RegistryError = "Name the image first, e.g. alpine:latest.";
            }
            else
            {
                try
                {
                    IsRegistryImageValidResponseDto verdict = _instance.IsRegistryImageValid(reference);

                    if (!verdict.Valid)
                    {
                        RegistryError = $"Unknown registry image '{reference}' - {verdict.Why}";
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceWarning($"[BuildViewModel] Could not verify registry image '{reference}': {ex.Message}");
                }
            }
        }

        if (!_nameOk || !_userOk ||
            ImageError.Length > 0 || RegistryError.Length > 0 || RecipeError.Length > 0)
        {
            return;
        }

        WslBuildType buildType;
        string? buildPath;
        string? context = null;

        if (RoadIndex == 2)
        {
            buildType = WslBuildType.Registry;
            buildPath = RegistryRef.Trim();
        }
        else if (RoadIndex == 1)
        {
            buildType = WslBuildType.Image;
            buildPath = _images[ImageIndex].Path;
        }
        else if (RoadIndex == 3)
        {
            buildType = WslBuildType.Recipe;
            context = _recipes[RecipeIndex].Path;
            buildPath = Path.Combine(context ?? string.Empty, "Dockerfile");
        }
        else
        {
            buildType = WslBuildType.Dockerfile;
            buildPath = _dockerfiles[DockerfileIndex].Path;
            context = Path.GetDirectoryName(buildPath) ?? string.Empty;
        }

        bool recipe = buildType == WslBuildType.Recipe;
        BuildDto dto = new()
        {
            Name = Name.Trim(),
            User = !recipe && RunOnboarding ? User.Trim() : string.Empty,
            BuildType = buildType,
            BuildPath = buildPath,
            Context = context,
            Onboarding = !recipe && RunOnboarding && OnboardingIndex >= 0 ? _onboardings[OnboardingIndex].Path : string.Empty,
            KeepImage = !recipe && SaveImage,
            RegisterDocker = !recipe && RegisterDocker,
        };

        Result = dto;
        RequestClose();
    }

    private void ValidateName()
    {
        string problem = string.Empty;

        if (Name.Length == 0)
        {
            problem = "Give a name.";
        }
        else if (!NameRules.IsValid(Name))
        {
            problem = "Letters, digits, '.', '_' and '-' only.";
        }
        else
        {
            try
            {
                if (_instance.GetAll().Any(row => !row.IsArchive && row.Name == Name))
                {
                    problem = $"An instance named '{Name}' already exists.";
                }
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning($"[BuildViewModel] Fleet check skipped for '{Name}': {ex.Message}");
            }
        }

        NameError = problem;
        _nameOk = problem.Length == 0;
        CreateCommand.NotifyCanExecuteChanged();
    }

    private void ValidateUser()
    {
        if (!RunOnboarding)
        {
            _userOk = true;
            UserError = string.Empty;
            CreateCommand.NotifyCanExecuteChanged();

            return;
        }

        _userOk = NameRules.IsValidUser(User);
        UserError = _userOk
            ? string.Empty
            : "Lowercase letters, digits, '_' and '-' only, starting with a letter.";
        CreateCommand.NotifyCanExecuteChanged();
    }

    private void RoadMoved()
    {
        RunOnboarding = RoadIndex == 0;
        ShowDockerfile = RoadIndex == 0;
        ShowImage = RoadIndex == 1;
        ShowRegistry = RoadIndex == 2;
        ShowRecipe = RoadIndex == 3;
        ShowGeneralOptions = RoadIndex != 3;
        ValidateUser();
    }

    private void SyncLists()
    {
        Fill(DockerfileNames, _dockerfiles.Select(row => row.Name));
        Fill(OnboardingNames, _onboardings.Select(row => row.Name));
        Fill(ImageNames, _images.Select(row => row.Name));
        Fill(RecipeNames, _recipes.Select(row => row.Name));
        SyncButtons();

        OnPropertyChanged(nameof(DockerfileIndex));
        OnPropertyChanged(nameof(OnboardingIndex));
        OnPropertyChanged(nameof(ImageIndex));
        OnPropertyChanged(nameof(RecipeIndex));
    }

    private void SyncButtons()
    {
        CanDownloadDockerfile = DockerfileIndex >= 0;
        CanDownloadOnboarding = OnboardingIndex >= 0;
        CanDownloadImage = ImageIndex >= 0;
        CanTrashDockerfile = Uploaded(_dockerfiles, DockerfileIndex);
        CanTrashOnboarding = Uploaded(_onboardings, OnboardingIndex);
        CanTrashRecipe = Uploaded(_recipes, RecipeIndex);
        CanTrashImage = ImageIndex >= 0 && ImageIndex < _images.Count && _images[ImageIndex].Uploaded;
    }

    private object? SelectedRow(string listName)
    {
        return listName switch
        {
            "Dockerfiles" => Selected(_dockerfiles, DockerfileIndex),
            "Onboardings" => Selected(_onboardings, OnboardingIndex),
            "Recipes" => Selected(_recipes, RecipeIndex),
            _ => ImageIndex >= 0 && ImageIndex < _images.Count ? _images[ImageIndex] : null,
        };
    }

    private void RemoveRecipe(string listName)
    {
        object? row = SelectedRow(listName);

        if (row is null || !IsUploaded(row))
        {
            return;
        }

        try
        {
            _instance.DeleteRecipe(PathOf(row));
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (listName == "Dockerfiles")
        {
            _ = _dockerfiles.RemoveAll(item => item.Path == PathOf(row));
            DockerfileIndex = _dockerfiles.Count > 0 ? 0 : -1;
        }
        else if (listName == "Onboardings")
        {
            _ = _onboardings.RemoveAll(item => item.Path == PathOf(row));
            OnboardingIndex = _onboardings.Count > 0 ? 0 : -1;
        }
        else if (listName == "Recipes")
        {
            _ = _recipes.RemoveAll(item => item.Path == PathOf(row));
            RecipeIndex = _recipes.Count > 0 ? 0 : -1;
        }
        else
        {
            _ = _images.RemoveAll(item => item.Path == PathOf(row));
            ImageIndex = _images.Count > 0 ? 0 : -1;
        }

        SyncLists();
    }

    private void AddRecipe(string kind, string source, string fileName, string listName)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        try
        {
            WslBuildSourceDto row = _instance.UploadRecipe(new UploadRecipeDto
            {
                Kind = kind,
                Source = source,
                FileName = fileName,
            });

            if (listName == "Dockerfiles")
            {
                _dockerfiles.Add(row);
                DockerfileIndex = _dockerfiles.Count - 1;
            }
            else if (listName == "Onboardings")
            {
                _onboardings.Add(row);
                OnboardingIndex = _onboardings.Count - 1;
            }
            else
            {
                _images.Add(new WslDockerImageDto
                {
                    Name = row.Name,
                    Path = row.Path,
                    Uploaded = row.Uploaded,
                });
                ImageIndex = _images.Count - 1;
            }

            SyncLists();
        }
        catch (InvalidOperationException ex)
        {
            if (listName == "DockerImages")
            {
                ImageError = ex.Message;
            }
            else
            {
                Trace.TraceWarning($"[BuildViewModel] Failed to upload '{source}' ({kind}): {ex.Message}");
            }
        }
    }

    private static void Fill(ObservableCollection<string> target, IEnumerable<string?> values)
    {
        target.Clear();

        foreach (string? value in values)
        {
            target.Add(value ?? string.Empty);
        }
    }

    private static T? Selected<T>(List<T> rows, int index)
        where T : class
    {
        return index >= 0 && index < rows.Count ? rows[index] : null;
    }

    private static bool Uploaded(List<WslBuildSourceDto> rows, int index)
    {
        return index >= 0 && index < rows.Count && rows[index].Uploaded;
    }

    private static bool IsUploaded(object row)
    {
        return row switch
        {
            WslBuildSourceDto source => source.Uploaded,
            WslDockerImageDto image => image.Uploaded,
            _ => false,
        };
    }

    private static string NameOf(object row)
    {
        return row switch
        {
            WslBuildSourceDto source => source.Name ?? string.Empty,
            WslDockerImageDto image => image.Name ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static string PathOf(object row)
    {
        return row switch
        {
            WslBuildSourceDto source => source.Path ?? string.Empty,
            WslDockerImageDto image => image.Path ?? string.Empty,
            _ => string.Empty,
        };
    }

}
