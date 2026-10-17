using System.Diagnostics.CodeAnalysis;

namespace wsl_squad.Core.Domain.Common;

/// <summary>Common contract shared by typed and untyped operation results.</summary>
public interface IOperationResult
{
    public OperationStatus Status { get; }

    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Error is the established name for the failure payload.")]
    public OperationError? Error { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsSuccess { get; }
}
