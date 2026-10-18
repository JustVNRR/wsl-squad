using wsl_squad.UI.Cli.Composition;

string? command = args.Length > 0 ? args[0] : null;
string? name = args.Length > 1 ? args[1] : null;
try
{
    return new CliComposition().Run(command, name);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);

    return 1;
}
