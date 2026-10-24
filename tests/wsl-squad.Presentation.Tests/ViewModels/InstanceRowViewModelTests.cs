using System.Windows.Input;

using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.ViewModels;

public class InstanceRowViewModelTests
{
    private readonly Mock<ICommand> _command = new();

    [Fact]
    public void A_live_row_keeps_a_disabled_pack_slot_when_no_pack_is_eligible()
    {
        WslIndexDto row = Row(
            Docker: "no",
            WslCommandKind.Shell,
            WslCommandKind.Restart,
            WslCommandKind.Appearance,
            WslCommandKind.Duplicate,
            WslCommandKind.Archive,
            WslCommandKind.Compact,
            WslCommandKind.RegisterDocker,
            WslCommandKind.Remove);

        InstanceRowViewModel viewModel = InstanceRowViewModel.From(row, _command.Object);

        viewModel.Commands.Select(command => command.Kind).Should().ContainInOrder(
            WslCommandKind.Shell,
            WslCommandKind.Restart,
            WslCommandKind.ManagePacks,
            WslCommandKind.Appearance,
            WslCommandKind.Duplicate,
            WslCommandKind.Archive,
            WslCommandKind.Compact,
            WslCommandKind.RegisterDocker,
            WslCommandKind.Remove);
        viewModel.Commands.Single(command => command.Kind == WslCommandKind.ManagePacks)
            .Enabled.Should().BeFalse();
    }

    [Fact]
    public void A_live_row_keeps_a_disabled_docker_slot_when_its_state_is_unknown()
    {
        WslIndexDto row = Row(
            Docker: "unknown",
            WslCommandKind.Shell,
            WslCommandKind.Restart,
            WslCommandKind.ManagePacks,
            WslCommandKind.Appearance,
            WslCommandKind.Duplicate,
            WslCommandKind.Archive,
            WslCommandKind.Compact,
            WslCommandKind.Remove);

        InstanceRowViewModel viewModel = InstanceRowViewModel.From(row, _command.Object);

        viewModel.Commands.Select(command => command.Kind).Should().ContainInOrder(
            WslCommandKind.Shell,
            WslCommandKind.Restart,
            WslCommandKind.ManagePacks,
            WslCommandKind.Appearance,
            WslCommandKind.Duplicate,
            WslCommandKind.Archive,
            WslCommandKind.Compact,
            WslCommandKind.RegisterDocker,
            WslCommandKind.Remove);
        viewModel.Commands.Single(command => command.Kind == WslCommandKind.RegisterDocker)
            .Enabled.Should().BeFalse();
    }

    [Fact]
    public void An_archive_row_has_only_its_restore_and_delete_commands()
    {
        WslIndexDto row = Row(
            Docker: string.Empty,
            WslCommandKind.Restore,
            WslCommandKind.DeleteArchive) with
        {
            IsArchive = true,
        };

        InstanceRowViewModel viewModel = InstanceRowViewModel.From(row, _command.Object);

        viewModel.Commands.Select(command => command.Kind).Should().Equal(
            WslCommandKind.Restore,
            WslCommandKind.DeleteArchive);
        viewModel.Commands.Should().OnlyContain(command => command.Enabled);
    }

    private static WslIndexDto Row(string Docker, params WslCommandKind[] kinds)
    {
        return new WslIndexDto
        {
            Name = "alpha",
            Docker = Docker,
            Commands = [.. kinds.Select(kind => new WslCommandDto { Kind = kind })],
        };
    }
}
