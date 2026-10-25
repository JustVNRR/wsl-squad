using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public interface IWslRepository
{
    IReadOnlyList<WslInstance> GetAllDistros();

    string GetFolder(string distroName);

    IReadOnlyList<string> GetRunningNames();

    void Import(string instanceName, string folder, string tarPath, bool interactive = false);

    void Export(string instanceName, string tarPath, string format);

    void Unregister(string instanceName);

    void Start(string instanceName);

    void Terminate(string instanceName);

    void TerminateQuietly(string instanceName);

    void Compact(string instanceName);

    void OpenProfile(string instanceName);

    void OpenBareShell(string instanceName);

    void OpenBareShellAsRoot(string instanceName);

    void SavePackManager(
        string instanceName,
        string script,
        string cleanupScript,
        string bootstrapScript,
        string catalogPath);

    void OpenPackManager(string instanceName, bool useProfile);

    int RunInInstance(string instanceName, string? asUser, IReadOnlyList<string> arguments, string? workingDirectory);

    IReadOnlyList<string> GetInInstance(string instanceName, IReadOnlyList<string> arguments);

    string GetFamily(string instanceName);
}
