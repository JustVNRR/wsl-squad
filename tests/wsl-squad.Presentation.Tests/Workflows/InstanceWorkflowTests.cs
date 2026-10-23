using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Cli.Adapters;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Tests.Workflows;

public class InstanceWorkflowTests
{
    [Fact]
    public async Task Archive_reports_a_missing_instance_as_a_no_target_failure()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([]);
        Mock<IInstanceView> view = new();
        InstanceStorageWorkflow workflow = new(service.Object, new ViewInstanceInteraction(view.Object));

        WorkflowResult<ArchiveResponseDto> result = await workflow.ArchiveAsync(null);

        result.Status.Should().Be(WorkflowStatus.Failed);
        result.Message.Should().Be("No instance is available.");
        view.Verify(v => v.ShowNoInstances(), Times.Once);
    }

    [Fact]
    public async Task Restore_reports_a_missing_archive_as_a_no_target_failure()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([]);
        Mock<IInstanceView> view = new();
        InstanceStorageWorkflow workflow = new(service.Object, new ViewInstanceInteraction(view.Object));

        WorkflowResult<RestoreResponseDto> result = await workflow.RestoreAsync(null);

        result.Status.Should().Be(WorkflowStatus.Failed);
        result.Message.Should().Be("No archive is available.");
        view.Verify(v => v.ShowNoArchives(), Times.Once);
    }
}
