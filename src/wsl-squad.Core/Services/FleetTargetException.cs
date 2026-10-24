namespace wsl_squad.Core.Services;

public sealed class FleetTargetException : InvalidOperationException
{
    public FleetTargetException(FleetTargetProblem problem, string? target)
        : base($"Invalid fleet target: {problem} ('{target}').")
    {
        Problem = problem;
        Target = target;
    }

    public FleetTargetProblem Problem { get; }

    public string? Target { get; }
}
