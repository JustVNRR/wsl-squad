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

    public int RunInInstance(string instanceName, string? asUser, IReadOnlyList<string> arguments, string? workingDirectory);

    public IReadOnlyList<string> GetInInstance(string instanceName, IReadOnlyList<string> arguments);

    public bool TestInInstance(string instanceName, IReadOnlyList<string> arguments);

    public string GetHome(string instanceName);

    public string GetFamily(string instanceName);

    public void SaveOnboardingScript(string instanceName, string source);

    public void RunOnboarding(string instanceName, string asUser, bool interactive = false);

    public IReadOnlyList<string> GetInstalledPacks(string instanceName, string packsDirectory);

    public int CopyFolderIntoInstance(string instanceName, string sourceFolder, string target);

    public int CopyFileIntoInstance(string instanceName, string sourceFile, string targetPath);

    public int RunScriptInInstance(string instanceName, string workingDirectory, string script, bool asRoot, string? errorLog, bool interactive = false);

    public bool HasInInstance(string instanceName, string path);

    public int RemoveFolderInInstance(string instanceName, string target);

    public bool OpenPackSudoWindow(string instanceName);

    public void ClosePackSudoWindow(string instanceName);

    public bool RunOrphanCleanup(string instanceName);
}
