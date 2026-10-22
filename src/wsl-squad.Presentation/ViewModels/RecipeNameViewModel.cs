using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation.ViewModels;

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
