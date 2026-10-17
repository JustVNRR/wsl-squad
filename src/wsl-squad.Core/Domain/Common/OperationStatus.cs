namespace wsl_squad.Core.Domain.Common;

/// <summary>Outcome of an operation.</summary>
public enum OperationStatus
{
    /// <summary>The operation completed and everything succeeded.</summary>
    Success,

    /// <summary>The main operation succeeded but secondary steps reported issues.</summary>
    Warning,

    /// <summary>Only part of the requested work succeeded.</summary>
    Partial,

    /// <summary>The user cancelled the operation before any commit point.</summary>
    Cancelled,

    /// <summary>The operation failed.</summary>
    Failed,
}
