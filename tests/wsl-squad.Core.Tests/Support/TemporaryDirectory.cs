namespace wsl_squad.Core.Tests.Support;

/// <summary>A temporary folder removed when the test ends.</summary>
public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"wsl-squad-tests-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts)
    {
        return System.IO.Path.Combine([Path, .. parts]);
    }

    public string Write(string relativePath, string content)
    {
        string target = Combine(relativePath.Split('/'));
        _ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target) ?? Path);
        File.WriteAllText(target, content);

        return target;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup.
        }
    }
}
