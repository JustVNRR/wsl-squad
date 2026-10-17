using System.Diagnostics.CodeAnalysis;

namespace wsl_squad.Core.Domain.Common;

/// <summary>Result of an operation that does not produce a value.</summary>
public sealed class OperationResult : IOperationResult
{
    private static readonly string[] _noWarnings = [];

    private OperationResult(OperationStatus status, OperationError? error, IReadOnlyList<string> warnings)
    {
        Status = status;
        Error = error;
        Warnings = warnings;
    }

    public OperationStatus Status { get; }

    public OperationError? Error { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsSuccess => Status is OperationStatus.Success or OperationStatus.Warning or OperationStatus.Partial;

    public static OperationResult Ok()
    {
        return new(OperationStatus.Success, null, _noWarnings);
    }

    public static OperationResult Warn(params string[] warnings)
    {
        return new(OperationStatus.Warning, null, CopyWarnings(warnings));
    }

    public static OperationResult Partial(OperationError error, params string[] warnings)
    {
        return new(
            OperationStatus.Partial,
            error ?? throw new ArgumentNullException(nameof(error)),
            CopyWarnings(warnings));
    }

    public static OperationResult Cancelled(string message = "Operation cancelled.")
    {
        return new(
            OperationStatus.Cancelled,
            new OperationError(ErrorCode.Cancelled, message),
            _noWarnings);
    }

    public static OperationResult Fail(
        ErrorCode code,
        string message,
        string? detail = null,
        int? externalExitCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new OperationResult(
            OperationStatus.Failed,
            new OperationError(code, message, detail, externalExitCode),
            _noWarnings);
    }

    internal static string[] CopyWarnings(string[]? warnings)
    {
        return warnings is { Length: > 0 } ? [.. warnings] : _noWarnings;
    }
}

/// <summary>Result of an operation that produces a value.</summary>
/// <typeparam name="T">Type of the produced value.</typeparam>
[SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Factory methods are the intended API for operation results.")]
public sealed class OperationResult<T> : IOperationResult
{
    private OperationResult(
        OperationStatus status,
        T value,
        OperationError? error,
        IReadOnlyList<string> warnings)
    {
        Status = status;
        Value = value;
        Error = error;
        Warnings = warnings;
    }

    public OperationStatus Status { get; }

    /// <summary>Value produced on success; <see langword="default"/> otherwise.</summary>
    public T Value { get; }

    public OperationError? Error { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsSuccess => Status is OperationStatus.Success or OperationStatus.Warning or OperationStatus.Partial;

    public static OperationResult<T> Ok(T value)
    {
        return new(OperationStatus.Success, value, null, OperationResult.CopyWarnings(null));
    }

    public static OperationResult<T> Warn(T value, params string[] warnings)
    {
        return new(OperationStatus.Warning, value, null, OperationResult.CopyWarnings(warnings));
    }

    public static OperationResult<T> Partial(T value, OperationError error, params string[] warnings)
    {
        return new(
            OperationStatus.Partial,
            value,
            error ?? throw new ArgumentNullException(nameof(error)),
            OperationResult.CopyWarnings(warnings));
    }

    public static OperationResult<T> Cancelled(string message = "Operation cancelled.")
    {
        return new(
            OperationStatus.Cancelled,
            default!,
            new OperationError(ErrorCode.Cancelled, message),
            OperationResult.CopyWarnings(null));
    }

    public static OperationResult<T> Fail(
        ErrorCode code,
        string message,
        string? detail = null,
        int? externalExitCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new OperationResult<T>(
            OperationStatus.Failed,
            default!,
            new OperationError(code, message, detail, externalExitCode),
            OperationResult.CopyWarnings(null));
    }
}
