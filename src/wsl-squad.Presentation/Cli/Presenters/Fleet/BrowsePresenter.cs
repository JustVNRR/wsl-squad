using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Localization;

namespace wsl_squad.Presentation.Cli.Presenters.Fleet;

public sealed class BrowsePresenter : BasePresenter<IFleetView>
{
    private readonly IInstanceService _service;
    private readonly IReadOnlyDictionary<WslCommandKind, PresenterAction> _actions;

    public BrowsePresenter(
        IInstanceService service,
        IFleetView view,
        IReadOnlyDictionary<WslCommandKind, PresenterAction> actions)
        : base(view)
    {
        _service = service;
        _actions = actions;
    }

    public string Miss { get; private set; } = string.Empty;

    public string Error { get; private set; } = string.Empty;

    public WslIndexDto? AskInstances()
    {
        Miss = string.Empty;
        Error = string.Empty;

        IReadOnlyList<WslIndexDto> rows;
        IReadOnlyList<string> running;
        IReadOnlyList<WslOrphanDto> orphans;

        try
        {
            rows = _service.GetAll();
            running = _service.GetRunningNames();
            orphans = _service.GetOrphans();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());
            Miss = "failure";
            Error = exception.Message;

            return null;
        }

        List<WslIndexDto> instances = [.. rows.Where(row => !row.IsArchive)];

        if (instances.Count == 0)
        {
            View.ShowNoInstances(orphans);
            Miss = "none";

            return null;
        }

        string? picked = View.PickInstance(instances, orphans, running);

        if (picked is null)
        {
            Miss = "cancelled";

            return null;
        }

        return instances.Find(row => row.Name == picked);
    }

    public WslIndexDto? AskArchives()
    {
        Miss = string.Empty;
        Error = string.Empty;

        IReadOnlyList<WslIndexDto> rows;

        try
        {
            rows = _service.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());
            Miss = "failure";
            Error = exception.Message;

            return null;
        }

        List<WslIndexDto> archives = [.. rows.Where(row => row.IsArchive)];

        if (archives.Count == 0)
        {
            View.ShowNoArchives();
            Miss = "none";

            return null;
        }

        string? picked = View.PickArchive(archives);

        if (picked is null)
        {
            Miss = "cancelled";

            return null;
        }

        return archives.Find(row => row.Name == picked);
    }

    public PresenterAction? AskAction(WslIndexDto row)
    {
        ArgumentNullException.ThrowIfNull(row);

        List<PresenterAction> actions = [];

        foreach (WslCommandDto command in row.Commands)
        {
            if (_actions.TryGetValue(command.Kind, out PresenterAction? action))
            {
                actions.Add(action);
            }
        }

        return actions.Count == 0
            ? null
            : View.PickAction($"Availables actions for '{row.Name}'", actions);
    }
}
