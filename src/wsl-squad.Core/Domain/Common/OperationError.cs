namespace wsl_squad.Core.Domain.Common;

/// <summary>Describes why an operation failed or only partially succeeded.</summary>
public sealed record OperationError(
    ErrorCode Code,
    string Message,
    string? Detail = null,
    int? ExternalExitCode = null);
