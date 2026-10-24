using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;
using wsl_squad.Presentation.Localization;

namespace wsl_squad.Presentation.Cli.Presenters.Fleet;

public sealed class SettingsPresenter : BasePresenter<IFleetView>
{
    private readonly IPackCatalogService _catalogue;

    public SettingsPresenter(
        IPackCatalogService catalogue,
        IFleetView view,
        IReadOnlyList<PresenterAction> actions)
        : base(view)
    {
        _catalogue = catalogue;
        Actions = actions;
    }

    public IReadOnlyList<PresenterAction> Actions { get; }

    public string Miss { get; private set; } = string.Empty;

    public string Error { get; private set; } = string.Empty;

    public PresenterAction? AskMenu()
    {
        Miss = string.Empty;
        Error = string.Empty;

        return View.PickAction("Settings", Actions);
    }

    public WslPackDto? AskPack()
    {
        Miss = string.Empty;
        Error = string.Empty;

        IReadOnlyList<WslPackDto> packs;

        try
        {
            packs = _catalogue.GetAllPacks();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());
            Miss = "failure";
            Error = exception.Message;

            return null;
        }

        if (packs.Count == 0)
        {
            View.ShowNoPacks();
            Miss = "none";

            return null;
        }

        WslPackDto? picked = View.PickPack(packs);

        if (picked is null)
        {
            Miss = "cancelled";
        }

        return picked;
    }

    public string? AskPackAction(WslPackDto pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return View.PickPackAction(pack.Name ?? string.Empty);
    }

    public IWorkflowResult? Edit(WslPackDto pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        try
        {
            _catalogue.OpenPackFolder(pack.Name ?? string.Empty);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed("Settings", exception.ToMessage());
        }

        View.ShowOpening(pack.Name ?? string.Empty);

        return WorkflowResults.Success(pack.Name ?? string.Empty);
    }

    public IWorkflowResult? Duplicate(WslPackDto pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        string? newName = View.AskName(pack.Name ?? string.Empty);

        if (string.IsNullOrEmpty(newName))
        {
            return null;
        }

        try
        {
            _ = _catalogue.CreatePack(new CreatePackDto
            {
                SourceName = pack.Name,
                NewName = newName,
                Description = pack.Description,
            });
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed("Settings", exception.ToMessage());
        }

        View.ShowDuplicated(newName);

        return WorkflowResults.Success(newName);
    }

    public IWorkflowResult? Delete(WslPackDto pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        if (pack.Dependents.Count > 0)
        {
            View.ShowDependents(pack.Name ?? string.Empty, pack.Dependents);
        }

        bool? confirmed = View.Confirm($"Remove the pack '{pack.Name}'?", defaultNo: true);

        if (confirmed != true)
        {
            return null;
        }

        try
        {
            _catalogue.DeletePack(pack.Name ?? string.Empty);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed("Settings", exception.ToMessage());
        }

        View.ShowRemoved(pack.Name ?? string.Empty);

        return WorkflowResults.Success(pack.Name ?? string.Empty);
    }
}
