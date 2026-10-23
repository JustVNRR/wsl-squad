using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Validation;

public enum DockerTargetStatus
{
    Eligible,
    AlreadyRegistered,
    NotRegistered,
    Unknown,
}

public static class DockerRules
{
    public static DockerTargetStatus Status(string? state, bool register)
    {
        if (state == "yes")
        {
            return register ? DockerTargetStatus.AlreadyRegistered : DockerTargetStatus.Eligible;
        }

        if (state == "no")
        {
            return register ? DockerTargetStatus.Eligible : DockerTargetStatus.NotRegistered;
        }

        return DockerTargetStatus.Unknown;
    }

    public static bool IsCandidate(WslIndexDto row, bool register)
    {
        ArgumentNullException.ThrowIfNull(row);

        return !row.IsArchive && Status(row.Docker, register) == DockerTargetStatus.Eligible;
    }
}
