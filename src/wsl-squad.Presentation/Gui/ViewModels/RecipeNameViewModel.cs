using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Validation;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RecipeNameViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _nameError = string.Empty;

    public RecipeNameViewModel()
    {
        ConfirmCommand = new RelayCommand(Confirm);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bound as an instance property by WPF.")]
    [SuppressMessage("Major Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Bound as an instance property by WPF.")]
    public string Lead => "Create a recipe";

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
            NameError = "Letters, digits, '.', '_' and '-' only, starting with a letter or a digit.";

            return;
        }

        Result = chosen;
        RequestClose();
    }
}
