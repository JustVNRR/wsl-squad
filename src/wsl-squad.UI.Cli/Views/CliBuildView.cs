using WslSquad.Core.Dtos;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.UI.Cli.Interaction;
using WslSquad.Presentation.Localization;

namespace WslSquad.UI.Cli.Views;

public sealed class CliBuildView : CliViewBase, IBuildView
{
    public CliBuildView(ConsoleHost host)
        : base(host)
    {
    }

    public void ShowNameProblem(string message)
    {
        Write($"  {message}", ConsoleTone.Warning);
    }

    public string? AskInstanceName(string prompt)
    {
        return AskLine(prompt);
    }

    public BuildRoad? AskRoad()
    {
        return Pick(
            Localizer.Instance["CliBuildFrom"],
            [
                new BuildRoad(WslBuildType.Dockerfile, Localizer.Instance["BuildRoadDockerfile"]),
                new BuildRoad(WslBuildType.Image, Localizer.Instance["BuildRoadImage"]),
                new BuildRoad(WslBuildType.Recipe, Localizer.Instance["BuildRoadRecipe"]),
            ],
            road => road.Label);
    }

    public void ShowAbort(IReadOnlyList<string> lines)
    {
        Write(string.Empty);
        string abortPrefix = Localizer.Instance["CliAbortPrefix"];

        for (int index = 0; index < lines.Count; index++)
        {
            ConsoleTone tone = index == 0 ? ConsoleTone.Error : ConsoleTone.Hint;
            string prefix = index == 0 ? abortPrefix : new string(' ', abortPrefix.Length);
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
        Write(Localizer.Instance["CliNoOnboarding"], ConsoleTone.Muted);
    }

    public bool? AskRunOnboarding()
    {
        Write(string.Empty);

        return AskConfirmation(Localizer.Instance["CliRunOnboarding"], defaultNo: false);
    }

    public BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows)
    {
        return Pick(Localizer.Instance["CliOnboardingShell"], rows, row => row.Name);
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
        Write("  " + Localizer.Instance["ErrorUserName"], ConsoleTone.Hint);
    }

    public bool? AskKeepImage()
    {
        Write(string.Empty);

        return AskConfirmation(Localizer.Instance["CliKeepImage"], defaultNo: false);
    }

    public bool? AskRegisterDocker(string name)
    {
        Write(string.Empty);

        return AskConfirmation(Localizer.Instance.Format("CliAddDocker", name), defaultNo: false);
    }

    public void Show(BuildResponseDto response)
    {
        foreach (BuildNoticeDto notice in response.Notices)
        {
            Write(string.Empty);
            Write($"  {notice.ToMessage()}", ConsoleTone.Muted);
        }

        Write(string.Empty);
        Write(
            Localizer.Instance.Format("CliBuilt", response.InstanceName, response.Status),
            response.Status == WslRecipeStatus.Warning ? ConsoleTone.Warning : ConsoleTone.Success);
        Write(string.Empty);
    }
}
