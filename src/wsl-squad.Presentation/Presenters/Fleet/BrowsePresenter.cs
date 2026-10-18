using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Fleet;

/// <summary>Picks a fleet row and one of its actions (reference <c>BrowsePresenter.psm1</c>).</summary>
public sealed class BrowsePresenter : BasePresenter<IFleetView>
{
    private readonly IInstanceService _service;

    public BrowsePresenter(
        IInstanceService service,
        IFleetView view,
        IReadOnlyList<PresenterAction> liveActions,
        PresenterAction registerAction,
        PresenterAction unregisterAction,
        IReadOnlyList<PresenterAction> archiveActions)
        : base(view)
    {
        _service = service;
        LiveActions = liveActions;
        RegisterAction = registerAction;
        UnregisterAction = unregisterAction;
        ArchiveActions = archiveActions;
    }

    public IReadOnlyList<PresenterAction> LiveActions { get; }

    public PresenterAction RegisterAction { get; }

    public PresenterAction UnregisterAction { get; }

    public IReadOnlyList<PresenterAction> ArchiveActions { get; }

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
            ShowFailure(exception.Message);
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
            ShowFailure(exception.Message);
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

        if (row.IsArchive)
        {
            return View.PickAction($"Availables actions for '{row.Name}'", ArchiveActions);
        }

        PresenterAction docker = row.Docker == "yes" ? UnregisterAction : RegisterAction;
        List<PresenterAction> actions = [];

        foreach (PresenterAction action in LiveActions)
        {
            if (action.Key == "unregister")
            {
                actions.Add(docker);
            }

            actions.Add(action);
        }

        return View.PickAction($"Availables actions for '{row.Name}'", actions);
    }
}
