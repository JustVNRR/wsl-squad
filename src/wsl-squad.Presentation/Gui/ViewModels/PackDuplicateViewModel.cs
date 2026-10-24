using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Validation;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class PackDuplicateViewModel : BaseViewModel
{
    private readonly IReadOnlyList<string> _taken;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    public PackDuplicateViewModel(string source, string blurb, IReadOnlyList<string> taken)
    {
        _taken = taken;
        NameError = string.Empty;
        Name = $"{source}-copy";
        Description = blurb;
        Lead = $"Duplicate the pack '{source}'".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public (string Name, string Description)? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnNameChanged(string value)
    {
        NameError = string.Empty;
    }

    private void Confirm()
    {
        string chosen = Name.Trim();
        string problem = chosen switch
        {
            _ when !NameRules.IsValid(chosen) =>
                "Letters, digits, '.', '_' and '-' only, starting with a letter or a digit.",
            _ when _taken.Contains(chosen, StringComparer.OrdinalIgnoreCase) =>
                $"A pack named '{chosen}' already exists.",
            _ => string.Empty,
        };

        if (problem.Length > 0)
        {
            NameError = problem;

            return;
        }

        Result = (chosen, Description.Trim());
        RequestClose();
    }
}
