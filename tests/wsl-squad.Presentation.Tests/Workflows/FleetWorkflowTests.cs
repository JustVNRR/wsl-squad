using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Tests.Workflows;

public class FleetWorkflowTests
{
    [Fact]
    public async Task An_explicit_target_is_migrated_without_a_prompt()
    {
        Mock<IFleetService> service = new();
        MigrateResponseDto response = new() { Target = "/new", Moved = ["alpha"] };
        service.Setup(s => s.Migrate("/new")).Returns(response);
        FleetWorkflow workflow = new(service.Object);

        WorkflowResult<MigrateResponseDto> result = await workflow.MigrateRootAsync("/new");

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("/new");
        result.Response.Should().BeSameAs(response);
        service.Verify(s => s.GetRoot(), Times.Never);
        service.Verify(s => s.Migrate("/new"), Times.Once);
    }

    [Fact]
    public async Task An_invalid_target_fails_without_migrating()
    {
        Mock<IFleetService> service = new();
        FleetWorkflow workflow = new(service.Object);

        WorkflowResult<MigrateResponseDto> result = await workflow.MigrateRootAsync("relative");

        result.Status.Should().Be(WorkflowStatus.Failed);
        result.Message.Should().Contain("absolute");
        service.Verify(s => s.Migrate(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task The_prompt_retries_same_and_invalid_folders()
    {
        Mock<IFleetService> service = new();
        service.Setup(s => s.GetRoot()).Returns("/fleet");
        service.Setup(s => s.Migrate("/new")).Returns(new MigrateResponseDto { Target = "/new" });
        Mock<IFleetInteraction> interaction = new();
        interaction.SetupSequence(i => i.AskRoot("/fleet"))
            .Returns("relative")
            .Returns("/fleet")
            .Returns("\"/new\"");
        FleetWorkflow workflow = new(service.Object, interaction.Object);

        WorkflowResult<MigrateResponseDto> result = await workflow.MigrateRootAsync(null);

        result.Status.Should().Be(WorkflowStatus.Success);
        interaction.Verify(i => i.ShowTargetProblem(It.IsAny<string>()), Times.Once);
        interaction.Verify(i => i.ShowSameFolder(), Times.Once);
        interaction.Verify(i => i.ShowLead(), Times.Once);
        service.Verify(s => s.Migrate("/new"), Times.Once);
    }

    [Fact]
    public async Task Cancelling_the_prompt_leaves_the_fleet_alone()
    {
        Mock<IFleetService> service = new();
        service.Setup(s => s.GetRoot()).Returns("/fleet");
        Mock<IFleetInteraction> interaction = new();
        interaction.Setup(i => i.AskRoot("/fleet")).Returns((string?)null);
        FleetWorkflow workflow = new(service.Object, interaction.Object);

        WorkflowResult<MigrateResponseDto> result = await workflow.MigrateRootAsync(null);

        result.Status.Should().Be(WorkflowStatus.Cancelled);
        service.Verify(s => s.Migrate(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void The_wsl_config_reports_the_file_it_opened()
    {
        Mock<IFleetService> service = new();
        WslConfigResponseDto response = new() { Path = "/home/agent/.wslconfig", Created = true };
        service.Setup(s => s.WslConfig()).Returns(response);
        FleetWorkflow workflow = new(service.Object);

        WorkflowResult<WslConfigResponseDto> result = workflow.OpenWslConfig();

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("/home/agent/.wslconfig");
        result.Response.Should().BeSameAs(response);
    }
}
