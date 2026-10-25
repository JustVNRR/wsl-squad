using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Presentation.Localization;
using WslSquad.Core.Validation;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RootMoveViewModel : BaseViewModel
{
    private readonly string _current;

    [ObservableProperty]
    public partial string Warning { get; set; }

    [ObservableProperty]
    public partial string NewRoot { get; set; }

    public RootMoveViewModel(string current)
    {
        _current = current.TrimEnd('\\');
        Warning = Localizer.Instance["RootMoveWarning"];
        NewRoot = current;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead => Localizer.Instance.Format("LeadRootMove", _current).ToUpperInvariant();

    public string? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        string chosen = NewRoot.Trim().Trim('"').TrimEnd('\\');

        if (chosen.Length == 0 || string.Equals(chosen, _current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        FleetTargetProblem problem = FleetTargetRules.Test(chosen);

        if (problem != FleetTargetProblem.None)
        {
            Warning = problem.ToMessage(chosen) ?? string.Empty;

            return;
        }

        Result = chosen;
        RequestClose();
    }
}
