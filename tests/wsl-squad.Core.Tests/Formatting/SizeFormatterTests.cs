using FluentAssertions;

using WslSquad.Core.Formatting;

namespace WslSquad.Core.Tests.Formatting;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 KB")]
    [InlineData(1536, "2 KB")]
    [InlineData(2 * 1024 * 1024, "2 MB")]
    public void Formats_sizes_like_the_reference(long bytes, string expected)
    {
        SizeFormatter.Format(bytes).Should().Be(expected);
    }

    [Fact]
    public void Formats_gigabytes()
    {
        SizeFormatter.Format(3L * 1024 * 1024 * 1024).Should().EndWith(" GB");
    }
}
