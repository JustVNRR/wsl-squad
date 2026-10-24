using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Adapters;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Tests.Workflows;

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

        result.Status.Should().Be(WorkflowStatus.NoTarget);
        result.Message.Should().Be("No instance is available.");
    }

    [Fact]
    public async Task Restore_reports_a_missing_archive_as_a_no_target_failure()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([]);
        Mock<IInstanceView> view = new();
        InstanceStorageWorkflow workflow = new(service.Object, new ViewInstanceInteraction(view.Object));

        WorkflowResult<RestoreResponseDto> result = await workflow.RestoreAsync(null);

        result.Status.Should().Be(WorkflowStatus.NoTarget);
        result.Message.Should().Be("No archive is available.");
    }
}
