using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace WslSquad.UI.Cli.Interaction;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Instance methods keep the console injectable and mirror the reference.")]
[SuppressMessage(
    "Major Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "Instance methods keep the console injectable and mirror the reference.")]
public sealed class ConsoleHost
{
    private readonly ConsoleTheme _theme;

    public ConsoleHost(ConsoleTheme theme)
    {
        _theme = theme;
    }

    public bool HasKeyboard()
    {
        if (Console.IsInputRedirected)
        {
            return false;
        }

        try
        {
            _ = Console.KeyAvailable;

            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return false;
        }
    }

    public ConsoleKey ReadKey()
    {
        ConsoleKeyInfo info = Console.ReadKey(intercept: true);

        if (info.Key == ConsoleKey.C && (info.Modifiers & ConsoleModifiers.Control) != 0)
        {
            throw new ConsoleQuitException();
        }

        return info.Key;
    }

    public int? Top()
    {
        try
        {
            return Console.CursorTop;
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    public bool SetTop(int top)
    {
        try
        {
            Console.SetCursorPosition(0, top);

            return Console.CursorTop == top;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            return false;
        }
    }

    public (int Width, int Height) Size()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException)
        {
            return (0, 0);
        }
    }

    public void Line(string text, ConsoleTone? tone = null)
    {
        bool plain = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")) ||
                     string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase);

        if (tone is not null && !plain && !Console.IsOutputRedirected)
        {
            ConsoleColor previous = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = _theme.GetColor(tone.Value);
                Console.WriteLine(text);
            }
            finally
            {
                Console.ForegroundColor = previous;
            }

            return;
        }

        Console.WriteLine(text);
    }

    public string? Prompt(string prompt)
    {
        return ReadLine(prompt);
    }

    public string? ReadLine(string prompt)
    {
        if (!HasKeyboard())
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write($"{prompt}: ");
            }

            return Console.ReadLine();
        }

        (bool captured, bool prior) = CaptureControlC();

        try
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.Write($"{prompt}: ");
            }

            StringBuilder text = new();

            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey(intercept: true);

                if (info.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (info.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();

                    return null;
                }

                if (info.Key == ConsoleKey.C && (info.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    Console.WriteLine();

                    throw new ConsoleQuitException();
                }

                if (info.Key == ConsoleKey.Backspace)
                {
                    if (text.Length > 0)
                    {
                        _ = text.Remove(text.Length - 1, 1);
                        Console.Write("\b \b");
                    }

                    continue;
                }

                if (info.KeyChar != '\0' && !char.IsControl(info.KeyChar))
                {
                    _ = text.Append(info.KeyChar);
                    Console.Write(info.KeyChar);
                }
            }

            Console.WriteLine();

            return text.ToString();
        }
        finally
        {
            if (captured)
            {
                RestoreControlC(prior);
            }
        }
    }

    internal static (bool Captured, bool Prior) CaptureControlC()
    {
        try
        {
            bool prior = Console.TreatControlCAsInput;
            Console.TreatControlCAsInput = true;

            return (true, prior);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return (false, false);
        }
    }

    internal static void RestoreControlC(bool prior)
    {
        try
        {
            Console.TreatControlCAsInput = prior;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            Trace.TraceWarning($"[ConsoleHost] Could not restore Ctrl+C mode: {ex.Message}");
        }
    }

    public void Clear()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            Console.Clear();
        }
        catch (IOException ex)
        {
            Trace.TraceWarning($"[ConsoleHost] Could not clear console: {ex.Message}");
        }
    }
}
