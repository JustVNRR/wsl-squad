using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ManagePacksPresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public ManagePacksPresenter(IInstanceService service, IInstanceView view)
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

            return ActionResult.Nok($"ManagePacks : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("ManagePacks : no instance")
                : ActionResult.Ok("ManagePacks : cancelled");
        }

        GetPacksResponseDto packs;

        try
        {
            packs = _service.GetPacks(name);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"ManagePacks : {name} - {exception.Message}");
        }

        HashSet<string> installed = new(packs.Installed, StringComparer.Ordinal);
        List<PackChoice> choices = [];
        List<int> checkedIndexes = [];
        HashSet<string> taken = new(StringComparer.Ordinal);

        foreach (WslPackDto pack in packs.Candidate)
        {
            choices.Add(new PackChoice(pack.Name ?? string.Empty, pack.Description ?? string.Empty));
            _ = taken.Add(pack.Name ?? string.Empty);
        }

        foreach (string removable in packs.Removable)
        {
            if (!taken.Add(removable))
            {
                continue;
            }

            checkedIndexes.Add(choices.Count);
            choices.Add(new PackChoice(removable, string.Empty));
        }

        if (choices.Count == 0)
        {
            View.ShowNothingToChoose(name);

            return ActionResult.Ok("ManagePacks : cancelled");
        }

        IReadOnlyList<string>? chosen = View.AskPacks(name, choices, checkedIndexes);

        if (chosen is null)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("ManagePacks : cancelled");
        }

        HashSet<string> ticked = new(chosen, StringComparer.Ordinal);
        List<string> toAdd = [.. ticked.Where(pack => !installed.Contains(pack))];
        List<string> toRemove = [.. packs.Removable.Where(pack => !ticked.Contains(pack))];

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            View.ShowNothingToDo(name);

            return ActionResult.Ok("ManagePacks : cancelled");
        }

        View.ShowPlan(toAdd, toRemove);

        try
        {
            ManagePacksResponseDto response = _service.ManagePacks(new ManagePacksDto
            {
                InstanceName = name,
                ToAdd = toAdd,
                ToRemove = toRemove,
            });
            View.Show(response);

            return ActionResult.Ok($"ManagePacks : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"ManagePacks : {name} - {exception.Message}");
        }
    }
}
