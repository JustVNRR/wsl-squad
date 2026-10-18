using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Registers an instance with Docker Desktop (reference <c>RegisterDockerPresenter.psm1</c>).</summary>
public sealed class RegisterDockerPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public RegisterDockerPresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
        _picker = new SelectInstancePresenter(view);
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

            return ActionResult.Nok($"RegisterDocker : {exception.Message}");
        }

        List<WslIndexDto> live = [.. rows.Where(row => !row.IsArchive)];
        List<WslIndexDto> candidates = [.. live.Where(row => row.Docker == "no")];
        string name;

        if (!string.IsNullOrEmpty(target))
        {
            WslIndexDto? row = live.Find(item => item.Name == target);

            if (row is null)
            {
                ShowFailure($"'{target}' is not an instance of this machine.");

                return ActionResult.Nok("RegisterDocker : no instance");
            }

            if (row.Docker == "yes")
            {
                View.ShowAlreadyRegistered(target);

                return ActionResult.Nok($"RegisterDocker : {target}");
            }

            if (row.Docker != "no")
            {
                View.ShowSettingsUnreadable();

                return ActionResult.Nok($"RegisterDocker : {target}");
            }

            name = target;
        }
        else
        {
            if (candidates.Count == 0)
            {
                if (live.Count > 0 && live.TrueForAll(row => row.Docker == "unknown"))
                {
                    View.ShowSettingsUnreadable();
                }
                else
                {
                    View.ShowNoCandidates();
                }

                return ActionResult.Nok("RegisterDocker : none to register");
            }

            name = _picker.Pick(string.Empty, candidates);

            if (name.Length == 0)
            {
                return ActionResult.Ok("RegisterDocker : cancelled");
            }
        }

        if (View.AskRegister(name) != true)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("RegisterDocker : cancelled");
        }

        bool restart = View.AskRestartDocker() == true;

        try
        {
            RegisterDockerResponseDto response = _service.RegisterDocker(new RegisterDockerDto
            {
                InstanceName = name,
                Restart = restart,
            });
            View.Show(response);

            return ActionResult.Ok($"RegisterDocker : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"RegisterDocker : {name} - {exception.Message}");
        }
    }
}
