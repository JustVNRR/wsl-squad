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
    public async Task Build_returns_the_service_response()
    {
        BuildDto request = new() { Name = "beta" };
        BuildResponseDto response = new() { InstanceName = "beta" };
        Mock<IInstanceService> service = new();
        service.Setup(s => s.Build(request)).Returns(response);
        service.Setup(s => s.Open("beta")).Returns(new OpenResponseDto { InstanceName = "beta" });
        List<string> messages = [];
        InstanceLifecycleWorkflow workflow = new(service.Object, Mock.Of<IInstanceInteraction>(), messages.Add);

        WorkflowResult<BuildResponseDto> result = await workflow.BuildAsync(request);

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("beta");
        result.Response.Should().BeSameAs(response);
        messages.Should().Contain("Building 'beta'...");
        service.Verify(s => s.Open("beta"), Times.Once);
    }

    [Fact]
    public async Task Build_reports_a_shell_that_does_not_open()
    {
        BuildDto request = new() { Name = "beta" };
        Mock<IInstanceService> service = new();
        service.Setup(s => s.Build(request)).Returns(new BuildResponseDto { InstanceName = "beta" });
        service.Setup(s => s.Open("beta")).Throws(new InvalidOperationException("no terminal"));
        InstanceLifecycleWorkflow workflow = new(service.Object, Mock.Of<IInstanceInteraction>());

        WorkflowResult<BuildResponseDto> result = await workflow.BuildAsync(request);

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Response!.Status.Should().Be(WslRecipeStatus.Warning);
        result.Response.Notices.Should().Contain(notice =>
            notice.Kind == BuildNoticeKind.ShellOpenFailed &&
            notice.TechnicalDetail == "no terminal");
    }

    [Fact]
    public async Task Build_reports_a_service_failure()
    {
        BuildDto request = new() { Name = "beta" };
        Mock<IInstanceService> service = new();
        service.Setup(s => s.Build(request)).Throws(new InvalidOperationException("docker failed"));
        InstanceLifecycleWorkflow workflow = new(service.Object, Mock.Of<IInstanceInteraction>());

        WorkflowResult<BuildResponseDto> result = await workflow.BuildAsync(request);

        result.Status.Should().Be(WorkflowStatus.Failed);
        result.Subject.Should().Be("beta");
        result.Message.Should().Be("docker failed");
    }

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
