using FluentAssertions;

using WslSquad.Core.Dtos;
using WslSquad.Core.Exceptions;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Tests;

public class ErrorMessagesTests
{
    [Fact]
    public void A_typed_core_error_is_formatted_for_the_interface()
    {
        WslSquadException exception = new(WslSquadErrorCode.InstanceAlreadyExists, "alpha");

        exception.ToMessage().Should().Be("An instance named 'alpha' already exists.");
    }

    [Fact]
    public void Disk_space_errors_keep_the_target_and_the_detail()
    {
        WslSquadException exception = new(
            WslSquadErrorCode.InsufficientDiskSpace,
            "D:",
            "2 GB needed, 1 GB free");

        exception.ToMessage().Should().Be("Not enough room on D:: 2 GB needed, 1 GB free.");
    }

    [Fact]
    public void Appearance_changes_are_rendered_for_the_interface()
    {
        new AppearanceChangeDto(AppearanceChangeKind.Font, "VT323")
            .ToMessage().Should().Be("font 'VT323'");
        new AppearanceChangeDto(AppearanceChangeKind.ColourScheme, "Campbell")
            .ToMessage().Should().Be("colours 'Campbell'");
        new AppearanceChangeDto(AppearanceChangeKind.Icon)
            .ToMessage().Should().Be("icon");
        new AppearanceChangeDto(AppearanceChangeKind.IconImage)
            .ToMessage().Should().Be("icon image");
    }

    [Fact]
    public void A_plain_operation_error_keeps_its_message()
    {
        InvalidOperationException exception = new("legacy");

        exception.ToMessage().Should().Be("legacy");
    }
}
