namespace WslSquad.UI.Cli.Interaction;

public sealed class ConsoleQuitException : Exception
{
    public ConsoleQuitException()
        : base("The user asked to quit.")
    {
    }
}
