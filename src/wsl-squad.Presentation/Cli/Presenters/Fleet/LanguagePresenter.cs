using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Fleet;

public sealed class LanguagePresenter
{
    private readonly IFleetView _view;

    public LanguagePresenter(IFleetView view)
    {
        _view = view;
    }

    public IWorkflowResult Run()
    {
        UiLanguage? chosen = _view.PickLanguage(Localizer.Instance.Languages);

        if (chosen is null)
        {
            return WorkflowResults.Cancelled("Language");
        }

        Localizer.Instance.SetLanguage(chosen.Code);
        _view.ShowLanguage(chosen.DisplayName);

        return WorkflowResults.Success(chosen.Code);
    }
}
