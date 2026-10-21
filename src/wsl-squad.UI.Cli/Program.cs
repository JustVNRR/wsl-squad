using wsl_squad.Core.Services;
using wsl_squad.UI.Cli;
using wsl_squad.UI.Cli.Commands;
using wsl_squad.UI.Cli.Composition;

try
{
    CliInvocation invocation = CliInvocation.Parse(args);

    if (!WslService.IsAvailable())
    {
        await Console.Error.WriteLineAsync("WSL 2 is required but was not detected.");
        await Console.Error.WriteLineAsync("Install it with: wsl --install");
        await Console.Error.WriteLineAsync("https://learn.microsoft.com/windows/wsl/install");

        return 1;
    }

    if (invocation.BuildRequest is not null)
    {
        return BuildRequestRunner.Run(invocation.BuildRequest);
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
