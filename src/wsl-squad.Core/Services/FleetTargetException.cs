using wsl_squad.Core.Exceptions;
namespace wsl_squad.Core.Services;

public sealed class FleetTargetException : WslSquadException
{
    public FleetTargetException(FleetTargetProblem problem, string? target)
        : base(WslSquadErrorCode.InvalidFleetTarget, target, problem.ToString())
    {
        Problem = problem;
    }

    public FleetTargetProblem Problem { get; }
}
