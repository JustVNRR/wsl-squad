using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Windows;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core;

public class DockerDesktopStoreTests
{
    private const string _settingsPath = "/home/test/.config/Docker/settings-store.json";

    private readonly MockFileSystem _fileSystem = new();

    private DockerDesktopStore CreateStore()
    {
        return new DockerDesktopStore(_fileSystem, new FakePlatformEnvironment());
    }

    [Fact]
    public void Reports_missing_settings()
    {
        OperationResult<DockerDesktopSettings> result = CreateStore().Read();

        result.IsSuccess.Should().BeTrue();
        result.Value.SettingsAvailable.Should().BeFalse();
        result.Value.IntegratedDistros.Should().BeEmpty();
    }

    [Fact]
    public void Reads_integrated_distributions()
    {
        _fileSystem.AddFile(
            _settingsPath,
            new MockFileData("""{ "IntegratedWslDistros": ["demo", "other"], "theme": "dark" }"""));

        DockerDesktopSettings settings = CreateStore().Read().Value;

        settings.SettingsAvailable.Should().BeTrue();
        settings.IntegratedDistros.Should().Equal("demo", "other");
    }

    [Fact]
    public void Adds_a_distribution_and_preserves_unknown_fields()
    {
        _fileSystem.AddFile(
            _settingsPath,
            new MockFileData("""{ "IntegratedWslDistros": ["other"], "theme": "dark" }"""));

        DockerDesktopStore store = CreateStore();
        store.SetIntegration("demo", integrated: true).IsSuccess.Should().BeTrue();

        string written = _fileSystem.File.ReadAllText(_settingsPath);
        written.Should().Contain("\"demo\"").And.Contain("\"other\"").And.Contain("\"theme\"");
        _fileSystem.File.Exists(_settingsPath + ".bak").Should().BeTrue();
        store.Read().Value.IntegratedDistros.Should().Equal("other", "demo");
    }

    [Fact]
    public void Removes_a_distribution()
    {
        _fileSystem.AddFile(
            _settingsPath,
            new MockFileData("""{ "IntegratedWslDistros": ["demo", "other"] }"""));

        DockerDesktopStore store = CreateStore();
        store.SetIntegration("demo", integrated: false).IsSuccess.Should().BeTrue();

        store.Read().Value.IntegratedDistros.Should().Equal("other");
    }

    [Fact]
    public void Does_not_rewrite_when_the_state_is_unchanged()
    {
        _fileSystem.AddFile(
            _settingsPath,
            new MockFileData("""{ "IntegratedWslDistros": ["demo"] }"""));

        DockerDesktopStore store = CreateStore();
        store.SetIntegration("demo", integrated: true).IsSuccess.Should().BeTrue();

        _fileSystem.File.Exists(_settingsPath + ".bak").Should().BeFalse();
    }

    [Fact]
    public void Fails_to_write_when_settings_are_missing()
    {
        OperationResult result = CreateStore().SetIntegration("demo", integrated: true);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Reports_corrupt_settings()
    {
        _fileSystem.AddFile(_settingsPath, new MockFileData("{broken"));

        OperationResult<DockerDesktopSettings> result = CreateStore().Read();

        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }
}
