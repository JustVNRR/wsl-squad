using WslSquad.Core.Dtos;
using WslSquad.Presentation.Cli.Presenters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli.Interaction;

namespace WslSquad.UI.Cli.Views;

public sealed class CliFleetView : CliViewBase, IFleetView
{
    public CliFleetView(ConsoleHost host)
        : base(host)
    {
    }

    public void Clear()
    {
        Host.Clear();
    }

    public UiLanguage? PickLanguage(IReadOnlyList<UiLanguage> languages)
    {
        return Pick(Localizer.Instance["Language"], languages, language => language.DisplayName);
    }

    public void ShowLanguage(string displayName)
    {
        Write(string.Empty);
        Write($"==> {Localizer.Instance.Format("LanguageChanged", displayName)}", ConsoleTone.Success);
    }

    public string? PickInstance(
        IReadOnlyList<WslIndexDto> instances,
        IReadOnlyList<WslOrphanDto> orphans,
        IReadOnlyList<string> running)
    {
        ShowOrphans(orphans);
        int stateWidth = Math.Max(
            Localizer.Instance["CliRunning"].Length,
            Localizer.Instance["CliStopped"].Length) + 2;

        WslIndexDto? picked = Pick(
            Localizer.Instance["CliOurInstances"],
            instances,
            row =>
            {
                string state = running.Contains(row.Name ?? string.Empty)
                    ? Localizer.Instance["CliRunning"]
                    : Localizer.Instance["CliStopped"];

                return $"{row.Name,-30} {state.PadRight(stateWidth)} {Size(row.Size),10}";
            });

        return picked?.Name;
    }

    public void ShowNoInstances(IReadOnlyList<WslOrphanDto> orphans)
    {
        ShowOrphans(orphans);
        Write(string.Empty);
        Write(Localizer.Instance["CliNoInstance"], ConsoleTone.Error);
        Write(Localizer.Instance["CliBuildOne"], ConsoleTone.Hint);
    }

    public string? PickArchive(IReadOnlyList<WslIndexDto> archives)
    {
        WslIndexDto? picked = Pick(
            Localizer.Instance["CliOurArchives"],
            archives,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowNoArchives()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliNoArchives"], ConsoleTone.Error);
        Write(Localizer.Instance["CliCreateArchiveHint"], ConsoleTone.Hint);
    }

    public PresenterAction? PickAction(string title, IReadOnlyList<PresenterAction> actions)
    {
        int keyWidth = Math.Max(12, actions.Max(action => action.Display.Length));

        return Pick(title, actions, action => $"{action.Display.PadRight(keyWidth)} {action.Text}");
    }

    public WslPackDto? PickPack(IReadOnlyList<WslPackDto> packs)
    {
        return Pick(
            Localizer.Instance["CliPacksCatalog"],
            packs,
            pack => $"{pack.Name,-20} {pack.Family,-10} {pack.Description}");
    }

    public void ShowNoPacks()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliNoPacks"], ConsoleTone.Error);
        Write(Localizer.Instance["CliPackFolderHint"], ConsoleTone.Hint);
    }

    public string? PickPackAction(string packName)
    {
        List<ConsoleMenuItem> items =
        [
            new("edit", Localizer.Instance["CliPackActionEdit"], Localizer.Instance["CliPackEdit"]),
            new("duplicate", Localizer.Instance["CliPackActionDuplicate"], Localizer.Instance["CliPackDuplicate"]),
            new("delete", Localizer.Instance["CliPackActionDelete"], Localizer.Instance["CliPackDelete"]),
        ];
        ConsoleMenu menu = new(Localizer.Instance.Format("CliPackOf", packName), Host);

        foreach (ConsoleMenuItem item in items)
        {
            menu.Add(item);
        }

        IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

        return picked is { Count: > 0 } ? picked[0].Key : null;
    }

    public string? AskName(string sourceName)
    {
        string? answer = AskLine(Localizer.Instance.Format("CliAskCopyOf", sourceName));

        return answer?.Trim();
    }

    public void ShowOpening(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliOpeningFolder", name), ConsoleTone.Info);
    }

    public void ShowDuplicated(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliCreatedInCatalog", name), ConsoleTone.Success);
    }

    public void ShowRemoved(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliDeletedFromCatalog", name), ConsoleTone.Success);
    }

    public void ShowDependents(string name, IReadOnlyList<string> dependents)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliDependentsHeader", name), ConsoleTone.Warning);

        foreach (string dependent in dependents)
        {
            Write($"- {dependent}", ConsoleTone.Warning);
        }

        Write(Localizer.Instance["CliDependentsDanger"], ConsoleTone.Error);
    }

    public string? AskTarget(string root)
    {
        Write(string.Empty);

        return AskLine(Localizer.Instance.Format("CliAskTarget", root))?.Trim();
    }

    public void ShowSameFolder()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliSameFolder"], ConsoleTone.Warning);
    }

    public void ShowTargetProblem(string problem)
    {
        Write(string.Empty);
        Write($"  {problem}", ConsoleTone.Warning);
    }

    public void ShowLead()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliArchivingFleet"], ConsoleTone.Info);
        Write(Localizer.Instance["CliArchivingFleetDetail"], ConsoleTone.Muted);
    }

    public void Show(MigrateResponseDto response)
    {
        Write(string.Empty);
        string moved = response.Moved.Count > 0 ? string.Join(", ", response.Moved) : string.Empty;

        if (moved.Length > 0)
        {
            Write(Localizer.Instance.Format("CliMovedInstances", moved, response.Target), ConsoleTone.Success);
        }
        else
        {
            Write(Localizer.Instance.Format("CliMovedArchives", response.Target), ConsoleTone.Success);
        }

        Write(Localizer.Instance["CliRestoreOtherSide"], ConsoleTone.Muted);
        Write(string.Empty);
    }

    public void Show(WslConfigResponseDto response)
    {
        if (response.Created)
        {
            Write(Localizer.Instance["CliWslConfigCreated"], ConsoleTone.Success);
        }
        else
        {
            Write(Localizer.Instance.Format("CliWslConfigPath", response.Path), ConsoleTone.Info);
        }
    }

    private void ShowOrphans(IReadOnlyList<WslOrphanDto> orphans)
    {
        if (orphans.Count == 0)
        {
            return;
        }

        Write(string.Empty);
        Write(Localizer.Instance["CliOrphansHeader"], ConsoleTone.Hint);

        foreach (WslOrphanDto row in orphans)
        {
            Write($"      {row.Name,-30} {Size(row.Size),10}  {row.Path}", ConsoleTone.Hint);
        }

        Write(Localizer.Instance["CliOrphansNote"], ConsoleTone.Muted);
    }
}
