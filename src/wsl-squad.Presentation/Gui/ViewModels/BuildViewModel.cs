using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class BuildViewModel : BaseViewModel
{
    private readonly IInstanceService _instance;
    private readonly IDialogService _dialogs;
    private readonly List<WslBuildSourceDto> _dockerfiles;
    private readonly List<WslBuildSourceDto> _onboardings;
    private readonly List<WslBuildSourceDto> _recipes;
    private readonly List<WslBuildSourceDto> _images;
    private readonly IReadOnlyList<string> _takenNames;
    private bool _nameOk;
    private bool _userOk;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    [ObservableProperty]
    public partial string User { get; set; }

    [ObservableProperty]
    public partial string UserError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerfile))]
    [NotifyPropertyChangedFor(nameof(ShowImage))]
    [NotifyPropertyChangedFor(nameof(ShowRegistry))]
    [NotifyPropertyChangedFor(nameof(ShowRecipe))]
    [NotifyPropertyChangedFor(nameof(ShowGeneralOptions))]
    public partial int RoadIndex { get; set; }

    [ObservableProperty]
    public partial int DockerfileIndex { get; set; }

    [ObservableProperty]
    public partial int OnboardingIndex { get; set; }

    [ObservableProperty]
    public partial int ImageIndex { get; set; }

    [ObservableProperty]
    public partial int RecipeIndex { get; set; }

    [ObservableProperty]
    public partial string RegistryRef { get; set; }

    [ObservableProperty]
    public partial string ImageError { get; set; }

    [ObservableProperty]
    public partial string RegistryError { get; set; }

    [ObservableProperty]
    public partial string RecipeError { get; set; }

    [ObservableProperty]
    public partial string DockerfileError { get; set; }

    [ObservableProperty]
    public partial bool SaveImage { get; set; }

    [ObservableProperty]
    public partial bool RegisterDocker { get; set; }

    [ObservableProperty]
    public partial bool RunOnboarding { get; set; }

    [ObservableProperty]
    public partial bool CanTrashDockerfile { get; set; }

    [ObservableProperty]
    public partial bool CanTrashOnboarding { get; set; }

    [ObservableProperty]
    public partial bool CanTrashImage { get; set; }

    [ObservableProperty]
    public partial bool CanTrashRecipe { get; set; }

    [ObservableProperty]
    public partial bool CanDownloadDockerfile { get; set; }

    [ObservableProperty]
    public partial bool CanDownloadOnboarding { get; set; }

    [ObservableProperty]
    public partial bool CanDownloadImage { get; set; }

    public BuildViewModel(IInstanceService instance, IDialogService dialogs)
    {
        _instance = instance;
        _dialogs = dialogs;
        _dockerfiles = [];
        _onboardings = [];
        _recipes = [];
        _images = [];
        NameError = string.Empty;
        UserError = string.Empty;
        RegistryRef = string.Empty;
        ImageError = string.Empty;
        RegistryError = string.Empty;
        RecipeError = string.Empty;
        DockerfileError = string.Empty;
        RegisterDocker = true;
        CreateCommand = new RelayCommand(Create, () => _nameOk && _userOk);

        try
        {
            BuildContextDto context = instance.GetBuildContext();
            _dockerfiles = [.. context.Sources.Dockerfiles];
            _onboardings = [.. context.Sources.Onboardings];
            _recipes = [.. context.Sources.Recipes];
            _images = [.. context.Images];
            _takenNames = context.TakenNames;
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Partial build sources loaded: {ex.Message}");
            _takenNames = [];
        }

        User = NameRules.ProposeUserName();
        RunOnboarding = true;
        Name = string.Empty;

        DockerfileIndex = _dockerfiles.Count > 0 ? 0 : -1;
        OnboardingIndex = _onboardings.Count > 0 ? 0 : -1;
        ImageIndex = _images.Count > 0 ? 0 : -1;
        RecipeIndex = _recipes.Count > 0 ? 0 : -1;

        ValidateName();
        ValidateUser();
        SyncLists();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound as an instance property by WPF.")]
    [SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound as an instance property by WPF.")]
    public string Lead => "NEW WSL DISTRIBUTION";

    public bool ShowDockerfile => RoadIndex == 0;

    public bool ShowImage => RoadIndex == 1;

    public bool ShowRegistry => RoadIndex == 2;

    public bool ShowRecipe => RoadIndex == 3;

    public bool ShowGeneralOptions => RoadIndex != 3;

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
        RunOnboarding = value == 0;
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

        WslBuildSourceDto? row = SelectedRow(listName);

        if (title.Length == 0 || row is null || !row.Uploaded)
        {
            return;
        }

        if (_dialogs.Confirm(title, $"Remove '{row.Name ?? string.Empty}'?"))
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
            RecipeError = ex.ToMessage();
        }
    }

    public void EditSource(string listName)
    {
        WslBuildSourceDto? row = SelectedRow(listName);

        if (row is null)
        {
            return;
        }

        try
        {
            _instance.OpenInEditor(row.Path ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Failed to open editor: {ex.Message}");
        }
    }

    public string DownloadName(string listName)
    {
        return SelectedRow(listName)?.SuggestedExportFileName ?? string.Empty;
    }

    public void ExportRecipeTo(string listName, string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        WslBuildSourceDto? row = SelectedRow(listName);

        if (row is null)
        {
            return;
        }

        try
        {
            _instance.ExportRecipe(new ExportRecipeDto
            {
                Path = row.Path ?? string.Empty,
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

        DockerfileError = RoadIndex == 0 && DockerfileIndex < 0 ? "Upload a Dockerfile first." : string.Empty;
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
                        RegistryError = $"Unknown registry image '{reference}' - {verdict.Error.ToMessage(verdict.TechnicalDetail)}";
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Trace.TraceWarning($"[BuildViewModel] Could not verify registry image '{reference}': {ex.Message}");
                }
            }
        }

        if (!_nameOk || !_userOk ||
            DockerfileError.Length > 0 ||
            ImageError.Length > 0 ||
            RegistryError.Length > 0 ||
            RecipeError.Length > 0)
        {
            return;
        }

        WslBuildType buildType;
        string source;

        if (RoadIndex == 2)
        {
            buildType = WslBuildType.Registry;
            source = RegistryRef.Trim();
        }
        else if (RoadIndex == 1)
        {
            buildType = WslBuildType.Image;
            source = _images[ImageIndex].Path ?? string.Empty;
        }
        else if (RoadIndex == 3)
        {
            buildType = WslBuildType.Recipe;
            source = _recipes[RecipeIndex].Path ?? string.Empty;
        }
        else
        {
            buildType = WslBuildType.Dockerfile;
            source = _dockerfiles[DockerfileIndex].Path ?? string.Empty;
        }

        BuildDto dto = BuildDto.FromSource(
            Name.Trim(),
            User.Trim(),
            buildType,
            source,
            RunOnboarding && OnboardingIndex >= 0
                ? _onboardings[OnboardingIndex].Path ?? string.Empty
                : string.Empty,
            SaveImage,
            RegisterDocker);

        Result = dto;
        RequestClose();
    }

    private void ValidateName()
    {
        string name = Name.Trim();
        NameProblem problem = NameRules.ValidateInstanceName(name, _takenNames);

        NameError = problem.ToMessage(name) ?? string.Empty;
        _nameOk = problem == NameProblem.None;
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
        CanTrashImage = Uploaded(_images, ImageIndex);
    }

    private WslBuildSourceDto? SelectedRow(string listName)
    {
        return listName switch
        {
            "Dockerfiles" => Selected(_dockerfiles, DockerfileIndex),
            "Onboardings" => Selected(_onboardings, OnboardingIndex),
            "Recipes" => Selected(_recipes, RecipeIndex),
            _ => Selected(_images, ImageIndex),
        };
    }

    private void RemoveRecipe(string listName)
    {
        WslBuildSourceDto? row = SelectedRow(listName);

        if (row is null || !row.Uploaded)
        {
            return;
        }

        try
        {
            _instance.DeleteRecipe(row.Path ?? string.Empty);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (listName == "Dockerfiles")
        {
            _dockerfiles.RemoveAll(item => item.Path == row.Path);
            DockerfileIndex = _dockerfiles.Count > 0 ? 0 : -1;
        }
        else if (listName == "Onboardings")
        {
            _onboardings.RemoveAll(item => item.Path == row.Path);
            OnboardingIndex = _onboardings.Count > 0 ? 0 : -1;
        }
        else if (listName == "Recipes")
        {
            _recipes.RemoveAll(item => item.Path == row.Path);
            RecipeIndex = _recipes.Count > 0 ? 0 : -1;
        }
        else
        {
            _images.RemoveAll(item => item.Path == row.Path);
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
                _images.Add(row);
                ImageIndex = _images.Count - 1;
            }

            SyncLists();
        }
        catch (InvalidOperationException ex)
        {
            if (listName == "DockerImages")
            {
                ImageError = ex.ToMessage();
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

    private static WslBuildSourceDto? Selected(List<WslBuildSourceDto> rows, int index)
    {
        return index >= 0 && index < rows.Count ? rows[index] : null;
    }

    private static bool Uploaded(List<WslBuildSourceDto> rows, int index)
    {
        return index >= 0 && index < rows.Count && rows[index].Uploaded;
    }
}
