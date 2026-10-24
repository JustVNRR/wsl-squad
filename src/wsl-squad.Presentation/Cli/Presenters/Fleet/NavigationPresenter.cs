using WslSquad.Core.Dtos;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters.Fleet;

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

    public IWorkflowResult Run(string kind)
    {
        _stack.Clear();
        _stack.Push(new NavigationState(kind));

        while (_stack.Count > 0)
        {
            _view.Clear();
            IWorkflowResult? result = Step();

            if (result is not null)
            {
                return result;
            }
        }

        return WorkflowResults.Cancelled("Navigate");
    }

    private IWorkflowResult? Step()
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
                    IWorkflowResult result = action.Execute(top.Row?.Name ?? string.Empty);

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
                    IWorkflowResult result = item.Execute(string.Empty);

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
                    IWorkflowResult? outcome = choice switch
                    {
                        "edit" => _settings.Edit(top.Pack!),
                        "duplicate" => _settings.Duplicate(top.Pack!),
                        "delete" => _settings.Delete(top.Pack!),
                        _ => null,
                    };

                    return outcome;
                }

            default:
                return WorkflowResults.Cancelled("Navigate");
        }
    }

    private WorkflowOutcome? Pop(object presenter, string label)
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
            return WorkflowResults.Cancelled(label);
        }

        if (miss == "none")
        {
            return WorkflowResults.Failed(label, "none");
        }

        return miss == "failure"
            ? WorkflowResults.Failed(label, error)
            : WorkflowResults.Cancelled(label);
    }

    private static bool IsCancelled(IWorkflowResult result)
    {
        return result.Status == WorkflowStatus.Cancelled;
    }

    private sealed record NavigationState(
        string Kind,
        WslIndexDto? Row = null,
        WslPackDto? Pack = null);
}
