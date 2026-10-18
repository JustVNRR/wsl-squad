using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Presenters;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.UI.Cli.Views;

/// <summary>The fleet browser, settings and migration views (reference CLI views).</summary>
public sealed class CliFleetView : CliViewBase, IFleetView
{
    public void Clear()
    {
        Host.Clear();
    }

    public string? PickInstance(
        IReadOnlyList<WslIndexDto> instances,
        IReadOnlyList<WslOrphanDto> orphans,
        IReadOnlyList<string> running)
    {
        ShowOrphans(orphans);

        WslIndexDto? picked = Pick(
            "Our Instances",
            instances,
            row =>
            {
                string state = running.Contains(row.Name ?? string.Empty) ? "running" : "stopped";

                return $"{row.Name,-30} {state,-8} {Size(row.Size),10}";
            });

        return picked?.Name;
    }

    public void ShowNoInstances(IReadOnlyList<WslOrphanDto> orphans)
    {
        ShowOrphans(orphans);
        Write(string.Empty);
        Write("[ABORT] No instance of this template is registered on this machine.", ConsoleTone.Error);
        Write("        Build one with  wsl-squad build", ConsoleTone.Hint);
    }

    public string? PickArchive(IReadOnlyList<WslIndexDto> archives)
    {
        WslIndexDto? picked = Pick(
            "Our Archives",
            archives,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowNoArchives()
    {
        Write(string.Empty);
        Write("[ABORT] There are no archives.", ConsoleTone.Error);
        Write("        Create one with  wsl-squad archive", ConsoleTone.Hint);
    }

    public PresenterAction? PickAction(string title, IReadOnlyList<PresenterAction> actions)
    {
        return Pick(title, actions, action => action.Label);
    }

    public WslPackDto? PickPack(IReadOnlyList<WslPackDto> packs)
    {
        return Pick(
            "Packs Catalog",
            packs,
            pack => $"{pack.Name,-20} {pack.Family,-10} {pack.Description}");
    }

    public void ShowNoPacks()
    {
        Write(string.Empty);
        Write("[ABORT] The catalog holds no pack.", ConsoleTone.Error);
        Write("        A pack is a folder under assets\\packs carrying a pack.conf.", ConsoleTone.Hint);
    }

    public string? PickPackAction(string packName)
    {
        List<ConsoleMenuItem> items =
        [
            new("edit      open the pack's folder", "edit"),
            new("duplicate duplicate it under a new name", "duplicate"),
            new("delete    delete it from the catalog", "delete"),
        ];
        ConsoleMenu menu = new($"Pack '{packName}'", new ConsoleHost(Theme));

        foreach (ConsoleMenuItem item in items)
        {
            _ = menu.Add(item);
        }

        IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

        return picked is { Count: > 0 } ? picked[0].Value as string : null;
    }

    public string? AskName(string sourceName)
    {
        string? answer = AskLine($"Name of the copy of '{sourceName}' (Enter to cancel)");

        return answer?.Trim();
    }

    public void ShowOpening(string name)
    {
        Write(string.Empty);
        Write($"==> Opening the folder of '{name}'", ConsoleTone.Info);
    }

    public void ShowDuplicated(string name)
    {
        Write(string.Empty);
        Write($"==> '{name}' created in the catalog", ConsoleTone.Success);
    }

    public void ShowRemoved(string name)
    {
        Write(string.Empty);
        Write($"==> '{name}' deleted from the catalog", ConsoleTone.Success);
    }

    public void ShowDependents(string name, IReadOnlyList<string> dependents)
    {
        Write(string.Empty);
        Write($"These packs depend on '{name}':", ConsoleTone.Warning);

        foreach (string dependent in dependents)
        {
            Write($"- {dependent}", ConsoleTone.Warning);
        }

        Write("Removing it may break them.", ConsoleTone.Error);
    }

    public bool? Confirm(string question, bool defaultNo)
    {
        return AskConfirmation(question, defaultNo);
    }

    public string? AskTarget(string root)
    {
        Write(string.Empty);
        Write($"Move the fleet from '{root}' to: ");

        return AskLine(string.Empty)?.Trim();
    }

    public void ShowCancelled(string noun)
    {
        Cancelled(noun);
    }

    public void ShowSameFolder()
    {
        Write(string.Empty);
        Write("  That is the working folder - nothing to move.", ConsoleTone.Warning);
    }

    public void ShowTargetProblem(string problem)
    {
        Write(string.Empty);
        Write($"  {problem}", ConsoleTone.Warning);
    }

    public void ShowLead()
    {
        Write(string.Empty);
        Write("==> Archiving the fleet", ConsoleTone.Info);
        Write("  Instances are stopped one by one as they are archived.", ConsoleTone.Muted);
    }

    public void Show(MigrateResponseDto response)
    {
        Write(string.Empty);
        string moved = response.Moved.Count > 0 ? string.Join(", ", response.Moved) : string.Empty;

        if (moved.Length > 0)
        {
            Write($"==> Moved {moved} and the archives to '{response.Target}'.", ConsoleTone.Success);
        }
        else
        {
            Write($"==> Moved the archives to '{response.Target}'.", ConsoleTone.Success);
        }

        Write("    Restore them on the other side with  wsl-squad restore", ConsoleTone.Muted);
        Write(string.Empty);
    }

    public void Show(WslConfigResponseDto response)
    {
        if (response.Created)
        {
            Write("  * .wslconfig : file created", ConsoleTone.Success);
        }
        else
        {
            Write($"  * .wslconfig : {response.Path}", ConsoleTone.Info);
        }
    }

    private void ShowOrphans(IReadOnlyList<WslOrphanDto> orphans)
    {
        if (orphans.Count == 0)
        {
            return;
        }

        Write(string.Empty);
        Write("Folders left behind by an instance that is gone:", ConsoleTone.Hint);

        foreach (WslOrphanDto row in orphans)
        {
            Write($"      {row.Name,-30} {Size(row.Size),10}  {row.Path}", ConsoleTone.Hint);
        }

        Write("      No instance claims them, and no command removes them: delete them by hand.", ConsoleTone.Muted);
    }
}
