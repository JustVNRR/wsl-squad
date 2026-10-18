using System.Diagnostics;
using System.Text.Json;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Drawing;

namespace wsl_squad.UI.Runner;

internal static class Runner
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: wsl-squad.UI.Runner apply_build <request.json> | apply_packs <name> <add> <remove>");

                return 1;
            }

            return args[0] switch
            {
                "apply_build" => ApplyBuild(args.Length > 1 ? args[1] : null),
                "apply_packs" => ApplyPacks(args),
                _ => Fail($"Unknown runner command '{args[0]}'."),
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Pause();

            return 1;
        }
    }

    private static int ApplyBuild(string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath) || !File.Exists(requestPath))
        {
            return Fail("Build : the request file is missing.");
        }

        BuildDto? request;

        try
        {
            request = JsonSerializer.Deserialize<BuildDto>(File.ReadAllText(requestPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Fail($"Build : the request could not be read - {exception.Message}");
        }
        finally
        {
            TryDelete(requestPath);
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return Fail("Build : the request carries no instance name.");
        }

        string name = request.Name;
        Console.WriteLine($"Building '{name}'...");

        IInstanceService service = CreateService();
        BuildResponseDto response;

        try
        {
            response = service.Build(request with { Interactive = true });
        }
        catch (Exception exception) when (IsExternalFailure(exception))
        {
            return Fail($"Build : {name} - {exception.Message}");
        }

        foreach (string message in response.Messages)
        {
            Console.WriteLine(message);
        }

        if (response.Status is not (WslRecipeStatus.Ok or WslRecipeStatus.Warning))
        {
            return Fail($"Build : {name} failed.");
        }

        Console.WriteLine($"Built '{name}'.");

        try
        {
            Console.WriteLine($"Opening a shell into '{name}'...");
            _ = service.Open(name);
        }
        catch (Exception exception) when (IsExternalFailure(exception))
        {
            Console.WriteLine($"The shell window could not be opened: {exception.Message}");
        }

        return 0;
    }

    private static int ApplyPacks(string[] args)
    {
        string name = args.Length > 1 ? args[1] : string.Empty;
        string add = args.Length > 2 ? args[2] : string.Empty;
        string remove = args.Length > 3 ? args[3] : string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Fail("Packs : no instance name was given.");
        }

        Console.WriteLine($"Applying packs on '{name}'...");

        IInstanceService service = CreateService();
        ManagePacksResponseDto response;

        try
        {
            response = service.ManagePacks(new ManagePacksDto
            {
                InstanceName = name,
                ToAdd = SplitPacks(add),
                ToRemove = SplitPacks(remove),
                Interactive = true,
            });
        }
        catch (Exception exception) when (IsExternalFailure(exception))
        {
            return Fail($"Packs : {name} - {exception.Message}");
        }

        foreach (string line in response.Installed)
        {
            Console.WriteLine($"  installed: {line}");
        }

        foreach (string line in response.Removed)
        {
            Console.WriteLine($"  removed:   {line}");
        }

        foreach (string line in response.Failed)
        {
            Console.WriteLine($"  FAILED:    {line}");
        }

        bool ok = response.ExitCode == 0;
        Console.WriteLine(ok
            ? $"Packs updated for '{name}'."
            : $"Packs failed for '{name}' (exit {response.ExitCode}).");

        Pause();

        return ok ? 0 : 1;
    }

    private static IInstanceService CreateService()
    {
        string assets = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wsl-squad");
        _ = Directory.CreateDirectory(assets);
        CatalogBootstrap.Ensure(assets);

        ServiceFactory factory = new();
        string root = factory.ResolveRoot(assets, assets);

        return factory.CreateInstanceService(root, assets, assets, IconPainter.Draw);
    }

    private static List<string> SplitPacks(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static bool IsExternalFailure(Exception exception)
    {
        return exception is InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception;
    }

    private static int Fail(string message)
    {
        Console.WriteLine(message);
        Pause();

        return 1;
    }

    private static void Pause()
    {
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine();
            Console.WriteLine("Press Enter to close.");
            _ = Console.ReadLine();
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[Runner] Could not delete temporary file '{path}': {ex.Message}");
        }
    }
}
