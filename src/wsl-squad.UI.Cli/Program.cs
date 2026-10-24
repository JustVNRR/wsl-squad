using WslSquad.Core.Services;
using WslSquad.Presentation.Localization;
using WslSquad.UI.Cli;
using WslSquad.UI.Cli.Composition;

try
{
    CliInvocation invocation = CliInvocation.Parse(args);

    if (!WslService.IsAvailable())
    {
        await Console.Error.WriteLineAsync(Localizer.Instance["CliWslMissing"]);
        await Console.Error.WriteLineAsync(Localizer.Instance["CliWslInstall"]);
        await Console.Error.WriteLineAsync("https://learn.microsoft.com/windows/wsl/install");

        return 1;
    }

    return new CliComposition().Run(invocation.Command, invocation.Name);
}
catch (ArgumentException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);

    return 2;
}
catch (Exception exception)
{
    await Console.Error.WriteLineAsync(exception.ToString());

    return 1;
}
