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

    public bool? AskKeepImage()
    {
        Write(string.Empty);

        return AskConfirmation(Localizer.Instance["CliKeepImage"], defaultNo: false);
    }

    public bool? AskUseLocalImage(string recipeName)
    {
        Write(string.Empty);

        return AskConfirmation(
            Localizer.Instance.Format("CliUseLocalImage", recipeName),
            defaultNo: false);
    }

    public bool? AskOverwriteImage(string recipeName)
    {
        Write(string.Empty);

        return AskConfirmation(
            Localizer.Instance.Format("CliOverwriteImage", recipeName),
            defaultNo: false);
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
