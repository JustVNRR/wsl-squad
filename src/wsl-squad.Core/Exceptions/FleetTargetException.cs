using WslSquad.Core.Validation;

namespace WslSquad.Core.Exceptions;

public sealed class FleetTargetException : WslSquadException
{
    public FleetTargetException(FleetTargetProblem problem, string? target)
        : base(WslSquadErrorCode.InvalidFleetTarget, target, problem.ToString())
    {
        Problem = problem;
    }

    public FleetTargetProblem Problem { get; }
}
