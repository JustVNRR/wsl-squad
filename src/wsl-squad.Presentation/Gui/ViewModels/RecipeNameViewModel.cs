using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Validation;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RecipeNameViewModel : BaseViewModel
{
    private IReadOnlyList<string> Taken { get; }

    private string? SourceName { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    public RecipeNameViewModel(
        string? suggestedName = null,
        IReadOnlyList<string>? taken = null,
        string? sourceName = null)
    {
        Taken = taken ?? [];
        SourceName = sourceName;
        NameError = string.Empty;
        Name = suggestedName ?? string.Empty;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound as an instance property by WPF.")]
    [SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound as an instance property by WPF.")]
    public string Lead => string.IsNullOrEmpty(SourceName)
        ? Localizer.Instance["LeadRecipe"]
        : Localizer.Instance.Format("LeadRecipeDuplicate", SourceName).ToUpperInvariant();

    public string? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnNameChanged(string value)
    {
        NameError = string.Empty;
    }

    private void Confirm()
    {
        string chosen = Name.Trim();

        if (!NameRules.IsValid(chosen))
        {
            NameError = Localizer.Instance["ErrorRecipeName"];

            return;
        }

        if (Taken.Contains(chosen, StringComparer.OrdinalIgnoreCase))
        {
            NameError = Localizer.Instance.Format("ErrorRecipeDuplicateExists", chosen);

            return;
        }

        Result = chosen;
        RequestClose();
    }
}
