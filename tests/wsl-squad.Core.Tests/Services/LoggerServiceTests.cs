using FluentAssertions;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Services;

public class LoggerServiceTests
{
    [Fact]
    public void Survives_a_log_directory_that_cannot_be_created()
    {
        using TemporaryDirectory temp = new();
        string blocker = temp.Write("blocker", "x");
        string path = Path.Combine(blocker, "logs", "wsl-squad.log");

        Action act = () => _ = new LoggerService(path);

        act.Should().NotThrow();
    }

    [Fact]
    public void Survives_a_log_rotation_that_cannot_complete()
    {
        using TemporaryDirectory temp = new();
        string path = temp.Combine("wsl-squad.log");
        File.WriteAllBytes(path, new byte[(1024 * 1024) + 1]);
        Directory.CreateDirectory(path + ".1");

        Action act = () => _ = new LoggerService(path);

        act.Should().NotThrow();
    }

    [Fact]
    public void Survives_a_log_file_that_cannot_be_written()
    {
        using TemporaryDirectory temp = new();
        string path = temp.Combine("wsl-squad.log");
        Directory.CreateDirectory(path);

        Action act = () =>
        {
            LoggerService logger = new(path);
            logger.Write(new LogEntryDto
            {
                When = DateTime.Now,
                Level = "ERROR",
                Message = "unwritable",
            });
        };

        act.Should().NotThrow();
    }
}
