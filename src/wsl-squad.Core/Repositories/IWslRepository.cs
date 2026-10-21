using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public interface IWslRepository
{
    public IReadOnlyList<WslInstance> GetAllDistros();

    public string GetFolder(string distroName);

    public IReadOnlyList<string> GetRunningNames();

    public void Import(string instanceName, string folder, string tarPath, bool interactive = false);

    public void Export(string instanceName, string tarPath, string format);

    public void Unregister(string instanceName);

    public void Start(string instanceName);

    public void Terminate(string instanceName);

    public void TerminateQuietly(string instanceName);

    public void Compact(string instanceName);

    public void OpenProfile(string instanceName);

    public void OpenBareShell(string instanceName);

    public void OpenBareShellAsRoot(string instanceName);

    public void SavePackManager(string instanceName, string script, string cleanupScript, string catalogPath);

    public void OpenPackManager(string instanceName, bool useProfile);

    public int RunInInstance(string instanceName, string? asUser, IReadOnlyList<string> arguments, string? workingDirectory);

    public IReadOnlyList<string> GetInInstance(string instanceName, IReadOnlyList<string> arguments);

    public bool TestInInstance(string instanceName, IReadOnlyList<string> arguments);

    public string GetFamily(string instanceName);

    public void SaveOnboardingScript(string instanceName, string source, string? asUser = null);

}
