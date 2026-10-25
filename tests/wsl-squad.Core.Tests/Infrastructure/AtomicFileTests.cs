using FluentAssertions;

using WslSquad.Core.Infrastructure;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Infrastructure;

public class AtomicFileTests
{
    [Fact]
    public void A_failed_replace_removes_the_temporary_file()
    {
        using TemporaryDirectory temp = new();
        string target = temp.Combine("settings.json");
        Directory.CreateDirectory(target);

        Action act = () => AtomicFile.WriteText(target, "content");

        act.Should().Throw<Exception>();
        File.Exists(target + ".tmp").Should().BeFalse();
    }
}
