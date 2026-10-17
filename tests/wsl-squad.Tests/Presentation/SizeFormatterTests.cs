using FluentAssertions;

using wsl_squad.Presentation.Formatting;

namespace wsl_squad.Tests.Presentation;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    public void Formats_bytes_with_binary_units(long bytes, string expected)
    {
        SizeFormatter.Format(bytes).Should().Be(expected);
    }

    [Fact]
    public void Rejects_negative_sizes()
    {
        Action act = () => SizeFormatter.Format(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
