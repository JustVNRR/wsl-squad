using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Validation;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class PackDuplicateViewModel : BaseViewModel
{
    private string Source { get; }
    private IReadOnlyList<string> Taken { get; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    public PackDuplicateViewModel(string source, string blurb, IReadOnlyList<string> taken)
    {
        Source = source;
        Taken = taken;
        NameError = string.Empty;
        Name = $"{source}-copy";
        Description = blurb;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead => Localizer.Instance.Format("LeadPackDuplicate", Source).ToUpperInvariant();

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
                Localizer.Instance["ErrorRecipeName"],
            _ when Taken.Contains(chosen, StringComparer.OrdinalIgnoreCase) =>
                Localizer.Instance.Format("ErrorPackDuplicateExists", chosen),
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
