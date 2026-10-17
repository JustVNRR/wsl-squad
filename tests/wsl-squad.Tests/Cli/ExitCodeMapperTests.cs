using FluentAssertions;

using wsl_squad.Core.Domain.Common;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.Tests.Cli;

public class ExitCodeMapperTests
{
    [Fact]
    public void Success_maps_to_zero()
    {
        ExitCodeMapper.Map(OperationResult.Ok()).Should().Be(0);
    }

    [Fact]
    public void Warning_and_partial_map_to_eight()
    {
        ExitCodeMapper.Map(OperationResult.Warn("warning")).Should().Be(8);
        ExitCodeMapper.Map(OperationResult.Partial(new OperationError(ErrorCode.PackConflict, "partial")))
            .Should()
            .Be(8);
    }

    [Theory]
    [InlineData(ErrorCode.Validation, 3)]
    [InlineData(ErrorCode.NotFound, 3)]
    [InlineData(ErrorCode.Conflict, 4)]
    [InlineData(ErrorCode.ExternalTool, 5)]
    [InlineData(ErrorCode.PermissionDenied, 6)]
    [InlineData(ErrorCode.DiskSpace, 7)]
    [InlineData(ErrorCode.Internal, 1)]
    public void Failures_map_by_error_code(ErrorCode code, int expected)
    {
        ExitCodeMapper.Map(OperationResult.Fail(code, "failure")).Should().Be(expected);
    }

    [Fact]
    public void Cancellation_maps_to_one_hundred_thirty()
    {
        ExitCodeMapper.Map(OperationResult.Cancelled()).Should().Be(130);
    }
}
