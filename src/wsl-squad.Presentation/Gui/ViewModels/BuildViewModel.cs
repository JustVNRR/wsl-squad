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
    private readonly List<WslBuildSourceDto> _recipes;
    private readonly IReadOnlyList<string> _takenNames;
    private bool _nameOk;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    [ObservableProperty]
    public partial int RecipeIndex { get; set; }

    [ObservableProperty]
    public partial string RecipeError { get; set; }

    [ObservableProperty]
    public partial bool SaveImage { get; set; }

    [ObservableProperty]
    public partial bool CanDeleteRecipe { get; set; }

    public BuildViewModel(IInstanceService instance, IDialogService dialogs)
    {
        _instance = instance;
        _dialogs = dialogs;
        _recipes = [];
        NameError = string.Empty;
        RecipeError = string.Empty;
        SaveImage = true;
        CreateCommand = new RelayCommand(Create, () => _nameOk && RecipeIndex >= 0);
        NewRecipeCommand = new RelayCommand(NewRecipe);
        EditRecipeCommand = new RelayCommand(EditRecipe, () => RecipeIndex >= 0);
        DeleteRecipeCommand = new RelayCommand(DeleteRecipe, () => CanDeleteRecipe);

        try
        {
            BuildContextDto context = instance.GetBuildContext();
            _recipes = [.. context.Recipes];
            _takenNames = context.TakenNames;
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[BuildViewModel] Could not load the recipes: {ex.Message}");
            _takenNames = [];
        }

        Name = string.Empty;
        RecipeIndex = _recipes.Count > 0 ? 0 : -1;
        ValidateName();
        SyncList();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound as an instance property by WPF.")]
    [SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound as an instance property by WPF.")]
    public string Lead => Localizer.Instance["LeadBuild"];

    public ObservableCollection<string> RecipeNames { get; } = [];

    public BuildDto? Result { get; private set; }

    public IRelayCommand CreateCommand { get; }

    public IRelayCommand NewRecipeCommand { get; }

    public IRelayCommand EditRecipeCommand { get; }

    public IRelayCommand DeleteRecipeCommand { get; }

    partial void OnNameChanged(string value)
    {
        ValidateName();
    }

    partial void OnRecipeIndexChanged(int value)
    {
        UpdateRecipeCommands();
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
            SyncList();
            RecipeIndex = _recipes.Count - 1;
        }
        catch (InvalidOperationException ex)
        {
            RecipeError = ex.ToMessage();
        }
    }

    public void EditRecipe()
    {
        WslBuildSourceDto? row = SelectedRecipe();

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
            Trace.TraceWarning($"[BuildViewModel] Failed to open the recipe: {ex.Message}");
        }
    }

    public void DeleteRecipe()
    {
        WslBuildSourceDto? row = SelectedRecipe();

        if (row is null || !row.Uploaded)
        {
            return;
        }

        if (!_dialogs.Confirm(
                Localizer.Instance["DeleteRecipeTitle"],
                Localizer.Instance.Format("RemoveFileQuestion", row.Name ?? string.Empty)))
        {
            return;
        }

        try
        {
            _instance.DeleteRecipe(row.Path ?? string.Empty);
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            RecipeError = ex.ToMessage();

            return;
        }

        _recipes.RemoveAll(item => item.Path == row.Path);
        SyncList();
        RecipeIndex = _recipes.Count > 0 ? Math.Min(RecipeIndex, _recipes.Count - 1) : -1;
    }

    private void Create()
    {
        ValidateName();
        RecipeError = RecipeIndex >= 0 && RecipeIndex < _recipes.Count
            ? string.Empty
            : Localizer.Instance["ErrorRecipeMissing"];

        if (!_nameOk || RecipeError.Length > 0)
        {
            return;
        }

        WslBuildSourceDto recipe = _recipes[RecipeIndex];
        Result = BuildDto.FromRecipe(
            Name.Trim(),
            recipe.Path ?? string.Empty,
            SaveImage);

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

    private void SyncList()
    {
        int index = RecipeIndex;
        RecipeNames.Clear();

        foreach (WslBuildSourceDto recipe in _recipes)
        {
            RecipeNames.Add(recipe.Name ?? string.Empty);
        }

        int selected = index >= 0 && index < _recipes.Count ? index : 0;
        RecipeIndex = _recipes.Count > 0 ? selected : -1;
        UpdateRecipeCommands();
    }

    private void UpdateRecipeCommands()
    {
        CanDeleteRecipe = SelectedRecipe()?.Uploaded == true;
        CreateCommand.NotifyCanExecuteChanged();
        EditRecipeCommand.NotifyCanExecuteChanged();
        DeleteRecipeCommand.NotifyCanExecuteChanged();
    }

    private WslBuildSourceDto? SelectedRecipe()
    {
        return RecipeIndex >= 0 && RecipeIndex < _recipes.Count ? _recipes[RecipeIndex] : null;
    }
}
