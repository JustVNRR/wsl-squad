using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Cli.Ports;

namespace wsl_squad.Presentation.Cli.Presenters.Fleet;

public sealed class NavigationPresenter
{
    private readonly BrowsePresenter _browse;
    private readonly SettingsPresenter _settings;
    private readonly IFleetView _view;
    private readonly Stack<NavigationState> _stack = new();

    public NavigationPresenter(BrowsePresenter browse, SettingsPresenter settings, IFleetView view)
    {
        _browse = browse;
        _settings = settings;
        _view = view;
    }

    public ActionResult Run(string kind)
    {
        _stack.Clear();
        _stack.Push(new NavigationState(kind));

        while (_stack.Count > 0)
        {
            _view.Clear();
            ActionResult? result = Step();

            if (result is not null)
            {
                return result;
            }
        }

        return ActionResult.Ok("Navigate : cancelled");
    }

    private ActionResult? Step()
    {
        NavigationState top = _stack.Peek();

        switch (top.Kind)
        {
            case "instances":
            {
                WslIndexDto? row = _browse.AskInstances();

                if (row is null)
                {
                    return Pop(_browse, "Instances");
                }

                _stack.Push(new NavigationState("instance-actions", row));
                return null;
            }

            case "archives":
            {
                WslIndexDto? row = _browse.AskArchives();

                if (row is null)
                {
                    return Pop(_browse, "Archives");
                }

                _stack.Push(new NavigationState("archive-actions", row));
                return null;
            }

            case "instance-actions":
            case "archive-actions":
            {
                PresenterAction? action = top.Row is null ? null : _browse.AskAction(top.Row);

                if (action is null)
                {
                    _ = _stack.Pop();
                    return null;
                }

                _view.Clear();
                ActionResult result = action.Execute(top.Row?.Name ?? string.Empty);

                return IsCancelled(result) ? null : result;
            }

            case "settings":
            {
                PresenterAction? item = _settings.AskMenu();

                if (item is null)
                {
                    return Pop(_settings, "Settings");
                }

                if (item.Key == "availables packs")
                {
                    _stack.Push(new NavigationState("packs"));
                    return null;
                }

                _view.Clear();
                ActionResult result = item.Execute(string.Empty);

                return IsCancelled(result) ? null : result;
            }

            case "packs":
            {
                WslPackDto? pack = _settings.AskPack();

                if (pack is null)
                {
                    return Pop(_settings, "Settings");
                }

                _stack.Push(new NavigationState("pack", Pack: pack));
                return null;
            }

            case "pack":
            {
                string? choice = top.Pack is null ? null : _settings.AskPackAction(top.Pack);

                if (choice is null)
                {
                    _ = _stack.Pop();
                    return null;
                }

                _view.Clear();
                ActionResult? outcome = choice switch
                {
                    "edit" => _settings.Edit(top.Pack!),
                    "duplicate" => _settings.Duplicate(top.Pack!),
                    "delete" => _settings.Delete(top.Pack!),
                    _ => null,
                };

                return outcome;
            }

            default:
                return ActionResult.Ok("Navigate : cancelled");
        }
    }

    private ActionResult? Pop(object presenter, string label)
    {
        _ = _stack.Pop();

        if (_stack.Count > 0)
        {
            return null;
        }

        string miss;
        string error;

        if (presenter is BrowsePresenter browse)
        {
            miss = browse.Miss;
            error = browse.Error;
        }
        else if (presenter is SettingsPresenter settings)
        {
            miss = settings.Miss;
            error = settings.Error;
        }
        else
        {
            return ActionResult.Ok($"{label} : cancelled");
        }

        if (miss == "none")
        {
            return ActionResult.Nok($"{label} : none");
        }

        return miss == "failure"
            ? ActionResult.Nok($"{label} : {error}")
            : ActionResult.Ok($"{label} : cancelled");
    }

    private static bool IsCancelled(ActionResult result)
    {
        return result.Status == ActionResultStatus.Ok &&
               result.Message.EndsWith(": cancelled", StringComparison.Ordinal);
    }

    private sealed record NavigationState(
        string Kind,
        WslIndexDto? Row = null,
        WslPackDto? Pack = null);
}
