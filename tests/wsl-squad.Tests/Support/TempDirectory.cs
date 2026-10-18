namespace wsl_squad.Tests.Support;

public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = Directory.CreateTempSubdirectory("wsl-squad-tests-").FullName;
    }

    public string Path { get; }

    public string Combine(params string[] parts)
    {
        return System.IO.Path.Combine([Path, .. parts]);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
