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
    public partial string OnboardingError { get; set; }

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
        OnboardingError = string.Empty;
        RegisterDocker = true;
        CreateCommand = new RelayCommand(Create, () => _nameOk && _userOk);
        UploadDockerfileCommand = new RelayCommand(PickDockerfile);
        UploadOnboardingCommand = new RelayCommand(PickOnboarding);
        UploadImageCommand = new RelayCommand(PickImage);
        DownloadDockerfileCommand = new RelayCommand(() => Download(WslCatalogSection.Dockerfiles));
        DownloadOnboardingCommand = new RelayCommand(() => Download(WslCatalogSection.Onboardings));
        DownloadImageCommand = new RelayCommand(() => Download(WslCatalogSection.DockerImages));
        EditSourceCommand = new RelayCommand<WslCatalogSection>(EditSource);
        DeleteSelectedCommand = new RelayCommand<WslCatalogSection>(DeleteSelected);
        NewRecipeCommand = new RelayCommand(NewRecipe);

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
    public string Lead => Localizer.Instance["LeadBuild"];

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

    public IRelayCommand UploadDockerfileCommand { get; }

    public IRelayCommand UploadOnboardingCommand { get; }

    public IRelayCommand UploadImageCommand { get; }

    public IRelayCommand DownloadDockerfileCommand { get; }

    public IRelayCommand DownloadOnboardingCommand { get; }

    public IRelayCommand DownloadImageCommand { get; }

    public IRelayCommand<WslCatalogSection> EditSourceCommand { get; }

    public IRelayCommand<WslCatalogSection> DeleteSelectedCommand { get; }

    public IRelayCommand NewRecipeCommand { get; }

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
        AddRecipe(WslCatalogSection.Dockerfiles, source);
    }

    public void AddOnboarding(string source)
    {
        AddRecipe(WslCatalogSection.Onboardings, source);
    }

    public void AddImage(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        AddRecipe(WslCatalogSection.DockerImages, source);
    }

    private void PickDockerfile()
    {
        string? source = _dialogs.PickOpenFile(
            Localizer.Instance["FileDialogDockerfile"],
            Localizer.Instance["FileFilterDockerfiles"]);

        if (source is not null)
        {
            AddDockerfile(source);
        }
    }

    private void PickOnboarding()
    {
        string? source = _dialogs.PickOpenFile(
            Localizer.Instance["FileDialogOnboarding"],
            Localizer.Instance["FileFilterShell"]);

        if (source is not null)
        {
            AddOnboarding(source);
        }
    }

    private void PickImage()
    {
        string? source = _dialogs.PickOpenFile(
            Localizer.Instance["FileDialogImage"],
            Localizer.Instance["FileFilterTar"]);

        if (source is not null)
        {
            AddImage(source);
        }
    }

    private void Download(WslCatalogSection kind)
    {
        string proposed = DownloadName(kind);

        if (proposed.Length == 0)
        {
            return;
        }

        string? target = _dialogs.PickSaveFile(
            Localizer.Instance.Format("FileDialogSave", proposed), proposed);

        if (target is not null)
        {
            ExportRecipeTo(kind, target);
        }
    }

    public void DeleteSelected(WslCatalogSection kind)
    {
        string title = kind switch
        {
            WslCatalogSection.Dockerfiles => Localizer.Instance["DeleteDockerfileTitle"],
            WslCatalogSection.Onboardings => Localizer.Instance["DeleteOnboardingTitle"],
            WslCatalogSection.DockerImages => Localizer.Instance["DeleteImageTitle"],
            WslCatalogSection.Recipes => Localizer.Instance["DeleteRecipeTitle"],
            _ => string.Empty,
        };

        WslBuildSourceDto? row = SelectedRow(kind);

        if (title.Length == 0 || row is null || !row.Uploaded)
        {
            return;
        }

        if (_dialogs.Confirm(
                title,
                Localizer.Instance.Format("RemoveFileQuestion", row.Name ?? string.Empty)))
        {
            RemoveRecipe(kind);
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
            RecipeError = string.Empty;
            SyncLists();
            RecipeIndex = _recipes.Count - 1;
        }
        catch (InvalidOperationException ex)
        {
            RecipeError = ex.ToMessage();
        }
    }

    public void EditSource(WslCatalogSection kind)
    {
        WslBuildSourceDto? row = SelectedRow(kind);

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

    public string DownloadName(WslCatalogSection kind)
    {
        return SelectedRow(kind)?.SuggestedExportFileName ?? string.Empty;
    }

    public void ExportRecipeTo(WslCatalogSection kind, string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        WslBuildSourceDto? row = SelectedRow(kind);

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

            if (kind == WslCatalogSection.Onboardings)
            {
                OnboardingError = string.Empty;
            }
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            if (kind == WslCatalogSection.Onboardings)
            {
                OnboardingError = ex.ToMessage();
            }

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

        DockerfileError = RoadIndex == 0 && DockerfileIndex < 0
            ? Localizer.Instance["ErrorDockerfileMissing"]
            : string.Empty;
        ImageError = RoadIndex == 1 && ImageIndex < 0
            ? Localizer.Instance["ErrorImageMissing"]
            : string.Empty;
        RecipeError = RoadIndex == 3 && RecipeIndex < 0
            ? Localizer.Instance["ErrorRecipeMissing"]
            : string.Empty;
        RegistryError = string.Empty;

        if (RoadIndex == 2)
        {
            string reference = RegistryRef.Trim();

            if (reference.Length == 0)
            {
                RegistryError = Localizer.Instance["ErrorRegistryEmpty"];
            }
            else
            {
                try
                {
                    IsRegistryImageValidResponseDto verdict = _instance.IsRegistryImageValid(reference);

                    if (!verdict.Valid)
                    {
                        RegistryError = Localizer.Instance.Format(
                            "ErrorRegistryUnknown",
                            reference,
                            verdict.Error.ToMessage(verdict.TechnicalDetail));
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
            : Localizer.Instance["ErrorUserName"];
        CreateCommand.NotifyCanExecuteChanged();
    }

    private void SyncLists()
    {
        int dockerfileIndex = DockerfileIndex;
        int onboardingIndex = OnboardingIndex;
        int imageIndex = ImageIndex;
        int recipeIndex = RecipeIndex;

        Fill(DockerfileNames, _dockerfiles.Select(row => row.Name));
        Fill(OnboardingNames, _onboardings.Select(row => row.Name));
        Fill(ImageNames, _images.Select(row => row.Name));
        Fill(RecipeNames, _recipes.Select(row => row.Name));

        DockerfileIndex = dockerfileIndex >= 0 && dockerfileIndex < _dockerfiles.Count ? dockerfileIndex : -1;
        OnboardingIndex = onboardingIndex >= 0 && onboardingIndex < _onboardings.Count ? onboardingIndex : -1;
        ImageIndex = imageIndex >= 0 && imageIndex < _images.Count ? imageIndex : -1;
        RecipeIndex = recipeIndex >= 0 && recipeIndex < _recipes.Count ? recipeIndex : -1;
        SyncButtons();
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

    private WslBuildSourceDto? SelectedRow(WslCatalogSection kind)
    {
        return kind switch
        {
            WslCatalogSection.Dockerfiles => Selected(_dockerfiles, DockerfileIndex),
            WslCatalogSection.Onboardings => Selected(_onboardings, OnboardingIndex),
            WslCatalogSection.DockerImages => Selected(_images, ImageIndex),
            WslCatalogSection.Recipes => Selected(_recipes, RecipeIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private void RemoveRecipe(WslCatalogSection kind)
    {
        WslBuildSourceDto? row = SelectedRow(kind);

        if (row is null || !row.Uploaded)
        {
            return;
        }

        try
        {
            _instance.DeleteRecipe(row.Path ?? string.Empty);

            if (kind == WslCatalogSection.Onboardings)
            {
                OnboardingError = string.Empty;
            }
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            if (kind == WslCatalogSection.Onboardings)
            {
                OnboardingError = ex.ToMessage();
            }

            return;
        }

        switch (kind)
        {
            case WslCatalogSection.Dockerfiles:
                _dockerfiles.RemoveAll(item => item.Path == row.Path);
                break;
            case WslCatalogSection.Onboardings:
                _onboardings.RemoveAll(item => item.Path == row.Path);
                break;
            case WslCatalogSection.DockerImages:
                _images.RemoveAll(item => item.Path == row.Path);
                break;
            case WslCatalogSection.Recipes:
                _recipes.RemoveAll(item => item.Path == row.Path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        SyncLists();

        switch (kind)
        {
            case WslCatalogSection.Dockerfiles:
                DockerfileIndex = _dockerfiles.Count > 0 ? 0 : -1;
                break;
            case WslCatalogSection.Onboardings:
                OnboardingIndex = _onboardings.Count > 0 ? 0 : -1;
                break;
            case WslCatalogSection.DockerImages:
                ImageIndex = _images.Count > 0 ? 0 : -1;
                break;
            case WslCatalogSection.Recipes:
                RecipeIndex = _recipes.Count > 0 ? 0 : -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private void AddRecipe(WslCatalogSection kind, string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        (string uploadKind, string fileName) = kind switch
        {
            WslCatalogSection.Dockerfiles => ("dockerfiles", "Dockerfile"),
            WslCatalogSection.Onboardings => ("onboardings", "onboarding.sh"),
            WslCatalogSection.DockerImages => ("dockerimages", Path.GetFileName(source)),
            WslCatalogSection.Recipes => throw new ArgumentOutOfRangeException(nameof(kind)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        try
        {
            WslBuildSourceDto row = _instance.UploadRecipe(new UploadRecipeDto
            {
                Kind = uploadKind,
                Source = source,
                FileName = fileName,
            });

            switch (kind)
            {
                case WslCatalogSection.Dockerfiles:
                    _dockerfiles.Add(row);
                    break;
                case WslCatalogSection.Onboardings:
                    _onboardings.Add(row);
                    OnboardingError = string.Empty;
                    break;
                case WslCatalogSection.DockerImages:
                    _images.Add(row);
                    break;
                case WslCatalogSection.Recipes:
                    throw new ArgumentOutOfRangeException(nameof(kind));
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            SyncLists();

            switch (kind)
            {
                case WslCatalogSection.Dockerfiles:
                    DockerfileIndex = _dockerfiles.Count - 1;
                    break;
                case WslCatalogSection.Onboardings:
                    OnboardingIndex = _onboardings.Count - 1;
                    break;
                case WslCatalogSection.DockerImages:
                    ImageIndex = _images.Count - 1;
                    break;
                case WslCatalogSection.Recipes:
                    throw new ArgumentOutOfRangeException(nameof(kind));
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            if (kind == WslCatalogSection.DockerImages)
            {
                ImageError = ex.ToMessage();
            }
            else if (kind == WslCatalogSection.Onboardings)
            {
                OnboardingError = ex.ToMessage();
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
