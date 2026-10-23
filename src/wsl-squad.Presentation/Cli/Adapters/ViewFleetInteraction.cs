using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Adapters;

public sealed class ViewFleetInteraction : IFleetInteraction
{
    private readonly IFleetView _view;

    public ViewFleetInteraction(IFleetView view)
    {
        _view = view;
    }

    public string? AskRoot(string currentRoot)
    {
        return _view.AskTarget(currentRoot);
    }

    public void ShowSameFolder()
    {
        _view.ShowSameFolder();
    }

    public void ShowTargetProblem(string problem)
    {
        _view.ShowTargetProblem(problem);
    }

    public void ShowLead()
    {
        _view.ShowLead();
    }
}
