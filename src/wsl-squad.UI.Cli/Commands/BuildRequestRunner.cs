using System.Text.Json;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation;
using wsl_squad.UI.Cli.Composition;

namespace wsl_squad.UI.Cli.Commands;

public static class BuildRequestRunner
{
    public static int Run(string requestPath)
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

        IInstanceService service = CliServices.Create().Instance;
        BuildResponseDto response;

        try
        {
            response = service.Build(request with { Interactive = true });
        }
        catch (Exception exception) when (IsExternalFailure(exception))
        {
            return Fail($"Build : {name} - {exception.Message}");
        }

        foreach (BuildNoticeDto notice in response.Notices)
        {
            Console.WriteLine(notice.ToMessage());
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not delete the temporary build request '{path}': {exception.Message}");
        }
    }
}
