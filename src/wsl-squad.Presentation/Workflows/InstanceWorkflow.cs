using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.Workflows;

public sealed class InstanceWorkflow : IInstanceWorkflow
{
    private readonly InstanceLifecycleWorkflow _lifecycle;
    private readonly InstanceStorageWorkflow _storage;
    private readonly InstanceIntegrationWorkflow _integration;
    private readonly InstanceAppearanceWorkflow _appearance;

    public InstanceWorkflow(
        IInstanceService service,
        IInstanceInteraction interaction,
        IAppearanceInteraction appearanceInteraction,
        Action<string>? executionStarted = null)
    {
        _lifecycle = new InstanceLifecycleWorkflow(service, interaction, executionStarted);
        _storage = new InstanceStorageWorkflow(service, interaction, executionStarted);
        _integration = new InstanceIntegrationWorkflow(service, interaction, executionStarted);
        _appearance = new InstanceAppearanceWorkflow(service, appearanceInteraction, executionStarted);
    }

    public Task<WorkflowResult<OpenResponseDto>> OpenAsync(string? target)
    {
        return _lifecycle.OpenAsync(target);
    }

    public Task<WorkflowResult<RestartResponseDto>> RestartAsync(string? target)
    {
        return _lifecycle.RestartAsync(target);
    }

    public Task<WorkflowResult<bool>> ManagePacksAsync(string? target)
    {
        return _lifecycle.ManagePacksAsync(target);
    }

    public Task<WorkflowResult<ArchiveResponseDto>> ArchiveAsync(string? target)
    {
        return _storage.ArchiveAsync(target);
    }

    public Task<WorkflowResult<CompactResponseDto>> CompactAsync(string? target)
    {
        return _storage.CompactAsync(target);
    }

    public Task<WorkflowResult<DuplicateResponseDto>> DuplicateAsync(string? target)
    {
        return _lifecycle.DuplicateAsync(target);
    }

    public Task<WorkflowResult<RemoveDto>> RemoveAsync(string? target)
    {
        return _lifecycle.RemoveAsync(target);
    }

    public Task<WorkflowResult<RestoreResponseDto>> RestoreAsync(string? target)
    {
        return _storage.RestoreAsync(target);
    }

    public Task<WorkflowResult<DeleteArchiveResponseDto>> DeleteArchiveAsync(string? target)
    {
        return _storage.DeleteArchiveAsync(target);
    }

    public Task<WorkflowResult<RegisterDockerResponseDto>> RegisterDockerAsync(string? target)
    {
        return _integration.RegisterDockerAsync(target);
    }

    public Task<WorkflowResult<UnregisterDockerResponseDto>> UnregisterDockerAsync(string? target)
    {
        return _integration.UnregisterDockerAsync(target);
    }

    public Task<WorkflowResult<SetAppearanceResponseDto>> AppearanceAsync(string? target)
    {
        return _appearance.AppearanceAsync(target);
    }
}
