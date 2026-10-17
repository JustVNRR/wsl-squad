using System.ComponentModel;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Application.Common;

/// <summary>Maps expected failures to typed operation results.</summary>
public static class OperationFailure
{
    public static bool IsExpected(Exception exception)
    {
        return exception is ProcessExecutionException
            or IOException
            or UnauthorizedAccessException
            or Win32Exception
            or InvalidDataException;
    }

    public static OperationResult<T> From<T>(OperationError error, IReadOnlyList<string>? extraDetails = null)
    {
        return OperationResult<T>.Fail(
            error.Code,
            error.Message,
            Combine(error.Detail, extraDetails),
            error.ExternalExitCode);
    }

    public static OperationResult<T> From<T>(Exception exception, IReadOnlyList<string>? extraDetails = null)
    {
        ErrorCode code;
        string message;
        string? detail;
        int? exitCode;

        if (exception is ProcessExecutionException process)
        {
            code = ErrorCode.ExternalTool;
            message = "An external tool failed.";
            detail = string.IsNullOrWhiteSpace(process.Result.StandardError)
                ? process.Message
                : process.Result.StandardError;
            exitCode = process.Result.ExitCode;
        }
        else if (exception is Win32Exception)
        {
            code = ErrorCode.ExternalTool;
            message = "An external tool could not be started.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is InvalidDataException)
        {
            code = ErrorCode.CorruptState;
            message = "A file could not be read.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is IOException)
        {
            code = ErrorCode.IoError;
            message = "A file operation failed.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is UnauthorizedAccessException)
        {
            code = ErrorCode.PermissionDenied;
            message = "A file operation was denied.";
            detail = exception.Message;
            exitCode = null;
        }
        else
        {
            code = ErrorCode.Internal;
            message = "The operation failed.";
            detail = exception.Message;
            exitCode = null;
        }

        return OperationResult<T>.Fail(code, message, Combine(detail, extraDetails), exitCode);
    }

    public static string? Combine(string? detail, IReadOnlyList<string>? extraDetails)
    {
        if (extraDetails is null || extraDetails.Count == 0)
        {
            return detail;
        }

        string extra = string.Join(" | ", extraDetails);

        return string.IsNullOrWhiteSpace(detail) ? extra : $"{detail}{Environment.NewLine}{extra}";
    }
}
