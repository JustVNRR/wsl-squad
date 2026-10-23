using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class UnregisterDockerPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;

    public UnregisterDockerPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run(string? target)
    {
        IReadOnlyList<WslIndexDto> rows;

        try
        {
            rows = _service.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"UnregisterDocker : {exception.Message}");
        }

        List<WslIndexDto> live = [.. rows.Where(row => !row.IsArchive)];
        List<WslIndexDto> candidates = [.. live.Where(row => DockerRules.IsCandidate(row, register: false))];
        string name;

        if (!string.IsNullOrEmpty(target))
        {
            WslIndexDto? row = live.Find(item => item.Name == target);

            if (row is null)
            {
                ShowFailure($"'{target}' is not an instance of this machine.");

                return ActionResult.Nok("UnregisterDocker : no instance");
            }

            DockerTargetStatus status = DockerRules.Status(row.Docker, register: false);

            if (status == DockerTargetStatus.NotRegistered)
            {
                View.ShowNotRegistered(target);

                return ActionResult.Nok($"UnregisterDocker : {target}");
            }

            if (status != DockerTargetStatus.Eligible)
            {
                View.ShowSettingsUnreadable();

                return ActionResult.Nok($"UnregisterDocker : {target}");
            }

            name = target;
        }
        else
        {
            if (candidates.Count == 0)
            {
                if (live.Count > 0 &&
                    live.TrueForAll(row => DockerRules.Status(row.Docker, register: false) == DockerTargetStatus.Unknown))
                {
                    View.ShowSettingsUnreadable();
                }
                else
                {
                    View.ShowNoCandidates();
                }

                return ActionResult.Nok("UnregisterDocker : none to unregister");
            }

            name = View.AskOffered(candidates) ?? string.Empty;

            if (name.Length == 0)
            {
                return ActionResult.Ok("UnregisterDocker : cancelled");
            }
        }

        if (View.AskUnregister(name) != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("UnregisterDocker : cancelled");
        }

        bool restart = View.AskRestartDocker() == true;

        try
        {
            UnregisterDockerResponseDto response = _service.UnregisterDocker(new UnregisterDockerDto
            {
                InstanceName = name,
                Restart = restart,
            });
            View.Show(response);

            return ActionResult.Ok($"UnregisterDocker : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"UnregisterDocker : {name} - {exception.Message}");
        }
    }
}
