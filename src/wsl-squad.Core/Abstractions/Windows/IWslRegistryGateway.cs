namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>A WSL distribution registered in the Windows registry.</summary>
public sealed record WslRegistryEntry(string Name, string BasePath);

/// <summary>Reads registered WSL distributions from the Windows registry.</summary>
public interface IWslRegistryGateway
{
    public bool IsSupported { get; }

    public IReadOnlyList<WslRegistryEntry> GetDistributions();
}
