using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation;

public static class OperationMessages
{
    public static string ToMessage(this DockerRestartOutcome outcome)
    {
        return outcome switch
        {
            DockerRestartOutcome.NotRequested => "unchanged",
            DockerRestartOutcome.Restarted => "restarted",
            DockerRestartOutcome.Started => "started",
            DockerRestartOutcome.Pending => "restart pending",
            _ => "unchanged",
        };
    }

    public static string ToMessage(
        this RegistryValidationError error,
        string? technicalDetail = null)
    {
        return error switch
        {
            RegistryValidationError.None => string.Empty,
            RegistryValidationError.DockerUnavailable => "docker is not on the PATH.",
            RegistryValidationError.NotFound => "no such image or tag on Docker Hub.",
            RegistryValidationError.Rejected =>
                string.IsNullOrEmpty(technicalDetail)
                    ? "the registry rejected the image."
                    : technicalDetail,
            RegistryValidationError.Unreachable => "Docker Hub could not be reached.",
            _ => "the registry could not validate the image.",
        };
    }
}
