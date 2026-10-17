using Microsoft.Win32;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Reads WSL distributions from <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Lxss</c>.</summary>
public sealed class WindowsRegistryGateway : IWslRegistryGateway
{
    private const string _lxssKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    public bool IsSupported => OperatingSystem.IsWindows();

    public IReadOnlyList<WslRegistryEntry> GetDistributions()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WSL registry access requires Windows.");
        }

        List<WslRegistryEntry> entries = [];

        using RegistryKey? lxss = Registry.CurrentUser.OpenSubKey(_lxssKeyPath);
        if (lxss is null)
        {
            return entries;
        }

        foreach (string subKeyName in lxss.GetSubKeyNames())
        {
            using RegistryKey? key = lxss.OpenSubKey(subKeyName);
            if (key?.GetValue("DistributionName") is not string name || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (key.GetValue("BasePath") is not string basePath || string.IsNullOrWhiteSpace(basePath))
            {
                continue;
            }

            entries.Add(new WslRegistryEntry(name, PathHelper.NormalizeRegistryBasePath(basePath)));
        }

        return entries;
    }
}
