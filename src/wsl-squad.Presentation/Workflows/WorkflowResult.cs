using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Workflows;

public enum WorkflowStatus
{
    Success,
    Cancelled,
    NoTarget,
    Failed,
}

public sealed record WorkflowResult<T>(
    WorkflowStatus Status,
    string Subject,
    string Message,
    T? Response);

public static class WorkflowResults
{
    public static WorkflowResult<T> Success<T>(string subject, T response)
    {
        return new WorkflowResult<T>(WorkflowStatus.Success, subject, string.Empty, response);
    }

    public static WorkflowResult<T> Cancelled<T>(string subject)
    {
        return new WorkflowResult<T>(WorkflowStatus.Cancelled, subject, string.Empty, default);
    }

    public static WorkflowResult<T> NoTarget<T>()
    {
        return new WorkflowResult<T>(WorkflowStatus.NoTarget, string.Empty, string.Empty, default);
    }

    public static WorkflowResult<T> Failed<T>(string subject, string message)
    {
        return new WorkflowResult<T>(WorkflowStatus.Failed, subject, message, default);
    }
}

public enum TargetResolutionStatus
{
    Selected,
    None,
    Cancelled,
}

public sealed record TargetResolution(TargetResolutionStatus Status, string Name);

public interface IInstanceWorkflow
{
    Task<WorkflowResult<OpenResponseDto>> OpenAsync(string? target);

    Task<WorkflowResult<RestartResponseDto>> RestartAsync(string? target);

    Task<WorkflowResult<bool>> ManagePacksAsync(string? target);

    Task<WorkflowResult<ArchiveResponseDto>> ArchiveAsync(string? target);

    Task<WorkflowResult<CompactResponseDto>> CompactAsync(string? target);

    Task<WorkflowResult<DuplicateResponseDto>> DuplicateAsync(string? target);

    Task<WorkflowResult<RemoveDto>> RemoveAsync(string? target);

    Task<WorkflowResult<RestoreResponseDto>> RestoreAsync(string? target);

    Task<WorkflowResult<DeleteArchiveResponseDto>> DeleteArchiveAsync(string? target);

    Task<WorkflowResult<bool>> RegisterDockerAsync(string? target);

    Task<WorkflowResult<bool>> UnregisterDockerAsync(string? target);

    Task<WorkflowResult<SetAppearanceResponseDto>> AppearanceAsync(string? target);
}

public interface IInstanceInteraction
{
    TargetResolution SelectInstance(string? target, IReadOnlyList<WslIndexDto> rows);

    TargetResolution SelectArchive(string? target, IReadOnlyList<WslIndexDto> rows);

    bool PromptRestart(string instanceName);

    ArchiveDto? PromptArchive(string instanceName, bool replacesArchive);

    CompactDto? PromptCompact(string instanceName, bool replacesArchive);

    DuplicateDto? PromptDuplicate(string instanceName, IReadOnlyList<string> takenNames);

    RemoveDto? PromptRemove(string instanceName, bool replacesArchive);

    RestoreDto? PromptRestore(string archiveName, IReadOnlyList<string> takenNames);

    bool PromptDeleteArchive(string archiveName);

    void ShowDockerProblem(string instanceName, DockerProblem problem);

    bool? PromptDocker(string instanceName, bool shouldRegister);
}

public interface IAppearanceInteraction
{
    TargetResolution SelectInstance(string? target, IReadOnlyList<WslIndexDto> rows);

    IReadOnlyList<SetAppearanceDto> PromptAppearance(string instanceName, AppearanceContextDto context);
}

public enum DockerProblem
{
    AlreadyRegistered,
    NotRegistered,
    Unknown,
    NoCandidates,
}
