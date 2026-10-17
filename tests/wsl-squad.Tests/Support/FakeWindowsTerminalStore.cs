using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Tests.Support;

public sealed class FakeWindowsTerminalStore : IWindowsTerminalStore
{
    private readonly List<TerminalFragmentSettings> _fragments = [];
    private readonly List<string> _deleted = [];
    private readonly List<WslColorScheme> _schemes =
    [
        new("One Half Dark", "#282C34", "#DCDFE4", [.. Enumerable.Repeat("#000000", 16)]),
    ];

    public string? ProfileGuid { get; set; } = "profile-guid";

    public bool LookupFails { get; set; }

    public Exception? WriteFailure { get; set; }

    public IReadOnlyList<TerminalFragmentSettings> Fragments => _fragments;

    public IReadOnlyList<string> Deleted => _deleted;

    public IReadOnlyList<WslColorScheme> Schemes => _schemes;

    public OperationResult<string?> GetProfileGuid(string instanceName)
    {
        return LookupFails
            ? OperationResult<string?>.Fail(ErrorCode.TerminalUnavailable, "terminal unavailable")
            : OperationResult<string?>.Ok(ProfileGuid);
    }

    public OperationResult WriteFragment(string instanceName, TerminalFragmentSettings settings)
    {
        _fragments.Add(settings);

        return WriteFailure is not null
            ? OperationResult.Fail(ErrorCode.IoError, "fragment not writable", WriteFailure.Message)
            : OperationResult.Ok();
    }

    public OperationResult DeleteFragment(string instanceName)
    {
        _deleted.Add(instanceName);

        return OperationResult.Ok();
    }

    public OperationResult<IReadOnlyList<WslColorScheme>> GetColorSchemes()
    {
        return OperationResult<IReadOnlyList<WslColorScheme>>.Ok(_schemes);
    }
}
