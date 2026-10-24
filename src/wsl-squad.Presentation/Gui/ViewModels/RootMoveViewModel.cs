using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Services;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RootMoveViewModel : BaseViewModel
{
    private readonly string _current;

    [ObservableProperty]
    private string _warning;

    [ObservableProperty]
    private string _newRoot;

    public RootMoveViewModel(string current)
    {
        _current = current.TrimEnd('\\');
        Lead = $"Move the fleet from '{current}'".ToUpperInvariant();
        _warning = "Every instance will be archived and unregistered before migration.";
        _newRoot = current;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public string? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        string chosen = NewRoot.Trim().Trim('"').TrimEnd('\\');

        if (chosen.Length == 0 || string.Equals(chosen, _current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        FleetTargetProblem problem = ReferenceHelpers.TestFleetTarget(chosen);

        if (problem != FleetTargetProblem.None)
        {
            Warning = problem.ToMessage(chosen) ?? string.Empty;

            return;
        }

        Result = chosen;
        RequestClose();
    }
}
