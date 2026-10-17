namespace wsl_squad.Core.Domain.Packs;

/// <summary>Metadata declared in a pack's <c>pack.conf</c>.</summary>
public sealed record PackConf(
    string Description,
    string Family,
    IReadOnlyList<string> Requires,
    bool Visible,
    string Welcome);

/// <summary>A pack folder discovered in the catalog.</summary>
public sealed record WslPack(
    string Name,
    string DirectoryPath,
    string Description,
    string Family,
    IReadOnlyList<string> Requires,
    bool Visible,
    string Welcome);

/// <summary>Result of loading the pack catalog.</summary>
public sealed record PackCatalogLoadResult(IReadOnlyList<WslPack> Packs, IReadOnlyList<string> Warnings);

/// <summary>Packs to remove, in removal order, and blocking dependents.</summary>
public sealed record PackRemovalPlan(IReadOnlyList<WslPack> RemovalOrder, IReadOnlyList<string> Blockers);

/// <summary>Current pack state of an instance.</summary>
public sealed record PackStatus(
    string InstanceName,
    IReadOnlyList<string> Installed,
    IReadOnlyList<WslPack> Candidates,
    IReadOnlyList<string> Removable,
    IReadOnlyList<WslPack> Catalog);

/// <summary>Failure of one pack step.</summary>
public sealed record PackOperationFailure(string PackName, string Step, string Message, int? ExitCode);

/// <summary>Result of a pack management operation.</summary>
public sealed record PackManageOutcome(
    string InstanceName,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<PackOperationFailure> Failed);
