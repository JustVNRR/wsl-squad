using wsl_squad.Core.Services;
using wsl_squad.UI.Cli.Composition;

string? command = args.Length > 0 ? args[0] : null;
string? name = args.Length > 1 ? args[1] : null;
try
{
    if (!WslService.IsAvailable())
    {
        await Console.Error.WriteLineAsync("WSL 2 is required but was not detected.");
        await Console.Error.WriteLineAsync("Install it with: wsl --install");
        await Console.Error.WriteLineAsync("https://learn.microsoft.com/windows/wsl/install");

        return 1;
    }

    return new CliComposition().Run(command, name);
}
catch (Exception exception)
{
    await Console.Error.WriteLineAsync(exception.ToString());

    return 1;
}
