using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.UI.Cli.Views;

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
        Write($"[FAIL] {message}", ConsoleTone.Error);
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
        string shown = defaultNo ? $"{question} [y/N]" : $"{question} [Y/n]";

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

            Write("  Answer 'y' or 'n'.", ConsoleTone.Hint);
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
            _ = menu.Add(new ConsoleMenuItem(label(item), item));
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
            _ = menu.Add(new ConsoleMenuItem(label(item), item));
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
        Write($"[ABORT] Operation cancelled by user. Nothing was {what}.", ConsoleTone.Success);
    }

    protected void Banner(string message)
    {
        (ConsoleColor box, ConsoleColor ink) = Theme.GetBannerColors();
        ConsoleColor priorForeground = Console.ForegroundColor;
        ConsoleColor priorBackground = Console.BackgroundColor;

        try
        {
            Console.ForegroundColor = ink;
            Console.BackgroundColor = box;
            Write(" /!\\ ================================================================ /!\\");
            Write($" |   {message,-60} |");
            Write(" \\!/ ================================================================ \\!/");
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
