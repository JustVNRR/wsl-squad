using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Workflows;

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
