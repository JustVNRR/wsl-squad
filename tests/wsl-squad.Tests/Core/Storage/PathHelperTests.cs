using FluentAssertions;

using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Tests.Core.Storage;

public class PathHelperTests
{
    [Theory]
    [InlineData("/tmp/fleet", true)]
    [InlineData(@"C:\WSL", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData("relative/path", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Detects_fully_qualified_paths(string? path, bool expected)
    {
        PathHelper.IsFullyQualified(path).Should().Be(expected);
    }

    [Fact]
    public void Extracts_parent_for_unix_and_windows_paths()
    {
        PathHelper.GetParent("/fleet/demo").Should().Be("/fleet");
        PathHelper.GetParent(@"C:\WSL\demo").Should().Be("C:/WSL");
    }

    [Theory]
    [InlineData(@"C:\WSL\Demo", "c:/wsl/demo", true)]
    [InlineData("/fleet/demo", "/fleet/demo/", true)]
    [InlineData("/fleet/demo", "/fleet/other", false)]
    [InlineData(null, "/fleet/demo", false)]
    public void Compares_paths_ignoring_case_and_separators(string? left, string right, bool expected)
    {
        PathHelper.AreSamePath(left, right).Should().Be(expected);
    }

    [Fact]
    public void Detects_paths_under_a_root()
    {
        PathHelper.IsUnder("/fleet/demo", "/fleet").Should().BeTrue();
        PathHelper.IsUnder("/fleet/demo/sub", "/fleet").Should().BeTrue();
        PathHelper.IsUnder("/fleet-other/demo", "/fleet").Should().BeFalse();
    }

    [Theory]
    [InlineData(@"\\?\D:\WSL\demo\", @"D:\WSL\demo")]
    [InlineData(@"D:\WSL\demo", @"D:\WSL\demo")]
    [InlineData(@"\\?\D:\WSL\demo", @"D:\WSL\demo")]
    public void Normalizes_registry_base_paths(string input, string expected)
    {
        PathHelper.NormalizeRegistryBasePath(input).Should().Be(expected);
    }
}
