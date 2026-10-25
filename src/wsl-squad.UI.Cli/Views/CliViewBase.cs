using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli.Interaction;

namespace WslSquad.UI.Cli.Views;

public abstract class CliViewBase : IPresenterViewBase
{
    protected CliViewBase(ConsoleHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        Theme = host.Theme;
    }

    protected ConsoleHost Host { get; }

    protected ConsoleTheme Theme { get; }

    public void ShowError(string message)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliFail", message), ConsoleTone.Error);
        Write(string.Empty);
    }

    public bool? Confirm(string question, bool defaultNo = true)
    {
        return AskConfirmation(question, defaultNo);
    }

    public void ShowCancelled(string noun)
    {
        Cancelled(noun);
    }

    protected void Write(string text = "", ConsoleTone? tone = null)
    {
        Host.Line(text, tone);
    }

    protected string? AskLine(string prompt)
    {
        return Host.ReadLine(prompt);
    }

    protected bool? AskConfirmation(string question, bool defaultNo)
    {
        string shown = Localizer.Instance.Format(defaultNo ? "CliConfirmDefaultNo" : "CliConfirmDefaultYes", question);

        while (true)
        {
            string? answer = AskLine(shown);

            if (answer is null)
            {
                return null;
            }

            string trimmed = answer.Trim();

            if (trimmed.Length == 0)
            {
                return !defaultNo;
            }

            if (Matches(trimmed, "y", Localizer.Instance["CliYesShort"], Localizer.Instance["CliYesLong"]))
            {
                return true;
            }

            if (Matches(trimmed, "n", Localizer.Instance["CliNoShort"], Localizer.Instance["CliNoLong"]))
            {
                return false;
            }

            Write(Localizer.Instance["CliAnswerYesNo"], ConsoleTone.Hint);
        }
    }

    private static bool Matches(string answer, params string[] accepted)
    {
        return accepted.Any(candidate => answer.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }

    protected T? Pick<T>(string title, IReadOnlyList<T> items, Func<T, string> label, string note = "")
        where T : class
    {
        if (items.Count == 0)
        {
            return null;
        }

        ConsoleMenu menu = new(title, Host) { Note = note };

        foreach (T item in items)
        {
            menu.Add(new ConsoleMenuItem(label(item), item));
        }

        IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

        return picked is { Count: > 0 } && picked[0].Value is T value ? value : null;
    }

    protected void Cancelled(string what)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliCancelled", what), ConsoleTone.Success);
    }

    protected void Banner(string message)
    {
        int inner = Math.Clamp(message.Length + 4, 24, 56);
        string text = message.Length <= inner ? message.PadRight(inner) : message[..inner];
        string rule = new('=', inner + 2);

        if (ConsoleHost.IsPlainOutput())
        {
            Write($"+{rule}+");
            Write($"| {text} |");
            Write($"+{rule}+");

            return;
        }

        ConsoleColor priorForeground = Console.ForegroundColor;
        ConsoleColor priorBackground = Console.BackgroundColor;

        try
        {
            (ConsoleColor box, ConsoleColor ink) = Theme.GetBannerColors();
            Console.ForegroundColor = ink;
            Console.BackgroundColor = box;
            Write($"+{rule}+");
            Write($"| {text} |");
            Write($"+{rule}+");
        }
        finally
        {
            Console.ForegroundColor = priorForeground;
            Console.BackgroundColor = priorBackground;
        }
    }

    protected static string Size(long bytes)
    {
        return ReferenceHelpers.FormatSize(bytes);
    }
}
