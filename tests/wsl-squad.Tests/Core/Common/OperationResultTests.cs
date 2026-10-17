using FluentAssertions;

using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Tests.Core.Common;

public class OperationResultTests
{
    [Fact]
    public void Ok_is_a_success_without_error()
    {
        OperationResult result = OperationResult.Ok();

        result.Status.Should().Be(OperationStatus.Success);
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Warn_preserves_messages_and_remains_successful()
    {
        OperationResult result = OperationResult.Warn("first", "second");

        result.Status.Should().Be(OperationStatus.Warning);
        result.IsSuccess.Should().BeTrue();
        result.Warnings.Should().Equal("first", "second");
    }

    [Fact]
    public void Partial_requires_an_error_and_remains_successful()
    {
        OperationError error = new(ErrorCode.PackConflict, "some packs failed");

        OperationResult result = OperationResult.Partial(error, "oh_my_py failed");

        result.Status.Should().Be(OperationStatus.Partial);
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Warnings.Should().ContainSingle().Which.Should().Be("oh_my_py failed");
    }

    [Fact]
    public void Cancelled_is_not_successful()
    {
        OperationResult result = OperationResult.Cancelled();

        result.Status.Should().Be(OperationStatus.Cancelled);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ErrorCode.Cancelled);
    }

    [Fact]
    public void Fail_keeps_error_details_and_external_exit_code()
    {
        OperationResult result = OperationResult.Fail(
            ErrorCode.ExternalTool,
            "docker failed",
            "docker: not found",
            125);

        result.Status.Should().Be(OperationStatus.Failed);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(
            new OperationError(ErrorCode.ExternalTool, "docker failed", "docker: not found", 125));
    }

    [Fact]
    public void Fail_rejects_a_blank_message()
    {
        Action act = () => OperationResult.Fail(ErrorCode.Validation, " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generic_ok_carries_the_value()
    {
        OperationResult<int> result = OperationResult<int>.Ok(42);

        result.Status.Should().Be(OperationStatus.Success);
        result.Value.Should().Be(42);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Generic_warning_carries_the_value_and_messages()
    {
        OperationResult<string> result = OperationResult<string>.Warn("value", "warning");

        result.Status.Should().Be(OperationStatus.Warning);
        result.Value.Should().Be("value");
        result.Warnings.Should().Equal("warning");
    }

    [Fact]
    public void Generic_failure_has_no_value_but_an_error()
    {
        OperationResult<string> result = OperationResult<string>.Fail(ErrorCode.NotFound, "missing");

        result.Status.Should().Be(OperationStatus.Failed);
        result.Value.Should().BeNull();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }
}
