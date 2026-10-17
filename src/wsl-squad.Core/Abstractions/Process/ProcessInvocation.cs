using System.Collections.ObjectModel;

namespace wsl_squad.Core.Abstractions.Process;

/// <summary>Describes an external process invocation without executing it.</summary>
public sealed class ProcessInvocation
{
    private static readonly IReadOnlyDictionary<string, string> _noEnvironment =
        ReadOnlyDictionary<string, string>.Empty;

    public ProcessInvocation(
        string fileName,
        IEnumerable<string>? arguments = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        FileName = fileName;
        Arguments = arguments is null ? [] : [.. arguments];
        WorkingDirectory = workingDirectory;
        Environment = environment is null
            ? _noEnvironment
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(environment));
    }

    public string FileName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public string? WorkingDirectory { get; }

    public IReadOnlyDictionary<string, string> Environment { get; }
}
