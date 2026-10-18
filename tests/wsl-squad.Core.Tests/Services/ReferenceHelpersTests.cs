using FluentAssertions;

using wsl_squad.Core.Services;
using wsl_squad.Core.Tests.Support;

namespace wsl_squad.Core.Tests.Services;

public class ReferenceHelpersTests
{
    [Theory]
    [InlineData(0, "0 KB")]
    [InlineData(1536, "2 KB")]
    [InlineData(2 * 1024 * 1024, "2 MB")]
    public void Formats_sizes_like_the_reference(long bytes, string expected)
    {
        ReferenceHelpers.FormatSize(bytes).Should().Be(expected);
    }

    [Fact]
    public void Formats_gigabytes()
    {
        ReferenceHelpers.FormatSize(3L * 1024 * 1024 * 1024).Should().EndWith(" GB");
    }

    [Fact]
    public void Rejects_missing_relative_and_occupied_targets()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("archives"));

        ReferenceHelpers.TestFleetTarget(null).Should().Contain("no destination");
        ReferenceHelpers.TestFleetTarget("relative").Should().Contain("not an absolute folder");
        ReferenceHelpers.TestFleetTarget(temp.Path).Should().Contain("already exists");
    }

    [Fact]
    public void Accepts_an_absolute_empty_target()
    {
        using TemporaryDirectory temp = new();

        ReferenceHelpers.TestFleetTarget(temp.Combine("fleet")).Should().BeNull();
    }
}
