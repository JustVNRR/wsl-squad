using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Fleet;

public sealed class MigratePresenter : BasePresenter<IFleetView>
{
    private readonly IFleetService _service;

    public MigratePresenter(IFleetService service, IFleetView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run(string? target)
    {
        string root;

        try
        {
            root = _service.GetRoot();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Migrate : {exception.Message}");
        }

        if (string.IsNullOrEmpty(target))
        {
            while (true)
            {
                string? answer = View.AskTarget(root);

                if (string.IsNullOrEmpty(answer))
                {
                    View.ShowCancelled("moved");

                    return ActionResult.Ok("Migrate : cancelled");
                }

                answer = answer.Trim().Trim('"').TrimEnd('\\');

                if (answer == root.TrimEnd('\\'))
                {
                    View.ShowSameFolder();

                    continue;
                }

                string? problem = ReferenceHelpers.TestFleetTarget(answer);

                if (problem is not null)
                {
                    View.ShowTargetProblem(problem);

                    continue;
                }

                target = answer;

                break;
            }
        }

        View.ShowLead();

        try
        {
            Core.Dtos.MigrateResponseDto response = _service.Migrate(target);

            View.Show(response);

            return ActionResult.Ok($"Migrate : {response.Target}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Migrate : {exception.Message}");
        }
    }
}
