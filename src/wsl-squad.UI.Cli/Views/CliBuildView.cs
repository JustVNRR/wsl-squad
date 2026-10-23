using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.UI.Cli.Views;

public sealed class CliBuildView : CliViewBase, IBuildView
{
    public void ShowNameHint()
    {
        Write("  Letters, digits, '.', '_' and '-' only.", ConsoleTone.Hint);
    }

    public string? AskInstanceName(string prompt)
    {
        return AskLine(prompt);
    }

    public BuildRoad? AskRoad()
    {
        return Pick(
            "Build from",
            [
                new BuildRoad(WslBuildType.Dockerfile, "Dockerfile"),
                new BuildRoad(WslBuildType.Image, "Docker image"),
                new BuildRoad(WslBuildType.Registry, "Docker registry"),
                new BuildRoad(WslBuildType.Recipe, "Recipe"),
            ],
            road => road.Label);
    }

    public string? AskRegistryReference(string prompt)
    {
        return AskLine(prompt)?.Trim();
    }

    public void ShowUnknownRegistry(string reference, string why)
    {
        Write($"  Unknown registry image '{reference}' - {why}", ConsoleTone.Warning);
    }

    public void ShowAbort(IReadOnlyList<string> lines)
    {
        Write(string.Empty);

        for (int index = 0; index < lines.Count; index++)
        {
            ConsoleTone tone = index == 0 ? ConsoleTone.Error : ConsoleTone.Hint;
            string prefix = index == 0 ? "[ABORT] " : "        ";
            Write(prefix + lines[index], tone);
        }
    }

    public BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows)
    {
        return Pick(label, rows, row => row.Name);
    }

    public void ShowNoOnboarding()
    {
        Write(string.Empty);
        Write("  No onboarding shell under 'assets\\onboardings' - the instance will be built without one.", ConsoleTone.Muted);
    }

    public bool? AskRunOnboarding()
    {
        Write(string.Empty);

        return AskConfirmation("Run the onboarding shell?", defaultNo: false);
    }

    public BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows)
    {
        return Pick("Onboarding shell", rows, row => row.Name);
    }

    public void ShowBeforeAccount()
    {
        Write(string.Empty);
    }

    public string? AskUserName(string prompt)
    {
        return AskLine(prompt)?.Trim();
    }

    public void ShowUserHint()
    {
        Write("  Lowercase letters, digits, '_' and '-' only, starting with a letter.", ConsoleTone.Hint);
    }

    public bool? AskKeepImage()
    {
        Write(string.Empty);

        return AskConfirmation("Keep Docker image?", defaultNo: false);
    }

    public bool? AskRegisterDocker(string name)
    {
        Write(string.Empty);

        return AskConfirmation($"Add '{name}' to Docker Desktop? (it will be restarted)", defaultNo: false);
    }

    public void Show(BuildResponseDto response)
    {
        foreach (string message in response.Messages)
        {
            Write(string.Empty);
            Write($"  {message}", ConsoleTone.Muted);
        }

        Write(string.Empty);
        Write(
            $"==> Built '{response.InstanceName}' ({response.Status}).",
            response.Status == WslRecipeStatus.Warning ? ConsoleTone.Warning : ConsoleTone.Success);
        Write(string.Empty);
    }
}
