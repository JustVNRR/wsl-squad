using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Workflows;

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

    Task<WorkflowResult<RegisterDockerResponseDto>> RegisterDockerAsync(string? target);

    Task<WorkflowResult<UnregisterDockerResponseDto>> UnregisterDockerAsync(string? target);

    Task<WorkflowResult<SetAppearanceResponseDto>> AppearanceAsync(string? target);
}
