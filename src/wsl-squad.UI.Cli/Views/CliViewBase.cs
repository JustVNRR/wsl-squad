using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli.Interaction;

namespace WslSquad.UI.Cli.Views;

public abstract class CliViewBase : IPresenterViewBase
{
    protected CliViewBase()
    {
        Theme = new ConsoleTheme();
        Host = new ConsoleHost(Theme);
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

            if (trimmed.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Write(Localizer.Instance["CliAnswerYesNo"], ConsoleTone.Hint);
        }
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

    protected IReadOnlyList<T>? Checklist<T>(
        string title,
        IReadOnlyList<T> items,
        Func<T, string> label,
        IReadOnlyList<int> checkedIndexes,
        string note = "")
        where T : class
    {
        if (items.Count == 0)
        {
            return [];
        }

        ConsoleMenu menu = new(title, Host) { Multi = true, Note = note };

        foreach (T item in items)
        {
            menu.Add(new ConsoleMenuItem(label(item), item));
        }

        foreach (int index in checkedIndexes)
        {
            if (index >= 0 && index < menu.Items.Count)
            {
                menu.Items[index].Checked = true;
            }
        }

        IReadOnlyList<ConsoleMenuItem>? picked = menu.Prompt();

        return picked is null
            ? null
            : [.. picked.Select(item => item.Value).OfType<T>()];
    }

    protected void Cancelled(string what)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliCancelled", what), ConsoleTone.Success);
    }

    protected void Banner(string message)
    {
        (ConsoleColor box, ConsoleColor ink) = Theme.GetBannerColors();
        ConsoleColor priorForeground = Console.ForegroundColor;
        ConsoleColor priorBackground = Console.BackgroundColor;

        int inner = Math.Clamp(message.Length + 4, 24, 56);
        string text = message.Length <= inner ? message.PadRight(inner) : message[..inner];
        string rule = new('=', inner + 2);

        try
        {
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
