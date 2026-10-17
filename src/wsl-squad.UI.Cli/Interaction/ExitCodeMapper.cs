using wsl_squad.Core.Domain.Common;

namespace wsl_squad.UI.Cli.Interaction;

/// <summary>Maps operation results to documented process exit codes.</summary>
public static class ExitCodeMapper
{
    public static int Map(IOperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status == OperationStatus.Cancelled)
        {
            return 130;
        }

        if (result.IsSuccess)
        {
            return result.Status == OperationStatus.Success ? 0 : 8;
        }

        return result.Error?.Code switch
        {
            ErrorCode.Usage => 2,
            ErrorCode.InvalidName or ErrorCode.NotFound or ErrorCode.Validation or ErrorCode.CorruptState => 3,
            ErrorCode.AlreadyExists or ErrorCode.InvalidState or ErrorCode.ConfirmationRequired
                or ErrorCode.Conflict or ErrorCode.PackConflict or ErrorCode.MigrationBlocked => 4,
            ErrorCode.ExternalTool or ErrorCode.Timeout or ErrorCode.DockerUnavailable
                or ErrorCode.TerminalUnavailable => 5,
            ErrorCode.PermissionDenied => 6,
            ErrorCode.DiskSpace => 7,
            ErrorCode.IoError or ErrorCode.Internal => 1,
            ErrorCode.Cancelled => 130,
            _ => 1,
        };
    }
}
