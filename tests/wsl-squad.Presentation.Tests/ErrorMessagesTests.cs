using FluentAssertions;

using wsl_squad.Core.Exceptions;

namespace wsl_squad.Presentation.Tests;

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
    public void A_plain_operation_error_keeps_its_message()
    {
        InvalidOperationException exception = new("legacy");

        exception.ToMessage().Should().Be("legacy");
    }
}
