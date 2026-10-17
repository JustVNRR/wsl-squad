using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Migration;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Migration;

public class LegacyMigratorTests
{
    private const string _legacyRoot = "/legacy";
    private const string _configPath = "/home/test/.local/share/wsl-squad/config.json";
    private static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakePlatformEnvironment _platform = new();
    private readonly FakeWslRegistryGateway _registry = new();
    private readonly FakeClock _clock = new(_now);

    private LegacyMigrator CreateMigrator()
    {
        return new LegacyMigrator(
            _fileSystem,
            _platform,
            _registry,
            new FleetMarkerStore(_fileSystem),
            new InstanceMetadataStore(_fileSystem, _clock),
            new ArchiveStore(_fileSystem),
            new FleetConfigStore(_fileSystem, AppPaths.GetConfigFilePath(_platform)),
            _clock);
    }

    private void AddLegacyRootFile()
    {
        _fileSystem.AddFile(
            "/home/test/.local/share/wsl-fleet/root.txt",
            new MockFileData(_legacyRoot + "\n"));
    }

    private void AddLegacyInstance(string name = "demo")
    {
        _fileSystem.AddFile(
            $"{_legacyRoot}/{name}/.wsl-fleet",
            new MockFileData("""{"template":"wsl-fleet","created":"2026-01-01","by":"build"}"""));
        _fileSystem.AddFile(
            $"{_legacyRoot}/{name}/instance.json",
            new MockFileData(
                """
                {
                  "DefaultUser": "jane",
                  "Theme": {
                    "Font": "Fira Code",
                    "ColorScheme": "One Half Dark",
                    "IconFrom": "C:\\icons\\demo.png",
                    "IconText": "DM",
                    "IconTop": "#111111",
                    "IconBottom": "#222222",
                    "IconTextColor": "#FFFFFF"
                  },
                  "Docker": "yes"
                }
                """));
        _registry.Add(name, $"{_legacyRoot}/{name}");
    }

    private void AddLegacyArchive(string name = "demo_archive", bool extraTar = false)
    {
        string directory = $"{_legacyRoot}/archives/{name}";
        _fileSystem.AddFile(
            $"{directory}/instance.json",
            new MockFileData("""{"DefaultUser":"jane","Docker":"no"}"""));
        _fileSystem.AddFile($"{directory}/{name}.tar.gz", new MockFileData("tar-data"));

        if (extraTar)
        {
            _fileSystem.AddFile($"{directory}/extra.tar", new MockFileData("extra"));
        }
    }

    [Fact]
    public void Dry_run_writes_nothing()
    {
        AddLegacyRootFile();
        AddLegacyInstance();

        OperationResult<MigrationPlan> plan = CreateMigrator().CreatePlan(null);

        plan.IsSuccess.Should().BeTrue();
        plan.Value.SourceRoot.Should().Be(_legacyRoot);
        plan.Value.Instances.Should().ContainSingle();
        plan.Value.Archives.Should().BeEmpty();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-squad").Should().BeFalse();
        _fileSystem.File.Exists(_configPath).Should().BeFalse();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-fleet").Should().BeTrue();
    }

    [Fact]
    public void Migrates_an_instance_and_writes_the_configuration()
    {
        AddLegacyRootFile();
        AddLegacyInstance();

        OperationResult<MigrationOutcome> result = CreateMigrator().Migrate(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Instances.Should().Be(1);
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-squad").Should().BeTrue();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-fleet").Should().BeFalse();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-fleet.migrated").Should().BeTrue();
        _fileSystem.File.ReadAllText(_configPath).Should().Contain(_legacyRoot);
        _fileSystem.File.Exists($"{result.Value.BackupDirectory}/manifest.json").Should().BeTrue();

        InstanceMetadata metadata = new InstanceMetadataStore(_fileSystem, _clock)
            .Read($"{_legacyRoot}/demo").Value;
        metadata.DefaultUser.Should().Be("jane");
        metadata.DockerDesktop.Should().Be("yes");
        metadata.Appearance!.Font.Should().Be("Fira Code");
        metadata.Appearance.ColorScheme.Should().Be("One Half Dark");
        metadata.Appearance.Icon!.Text.Should().Be("DM");
        metadata.Appearance.Icon.Image.Should().Be(@"C:\icons\demo.png");
    }

    [Fact]
    public void Migrates_an_archive()
    {
        AddLegacyRootFile();
        AddLegacyInstance();
        AddLegacyArchive();

        OperationResult<MigrationOutcome> result = CreateMigrator().Migrate(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Archives.Should().Be(1);

        OperationResult<ArchiveMetadata> metadata =
            new ArchiveStore(_fileSystem).Read($"{_legacyRoot}/archives/demo_archive");
        metadata.Value.File.Should().Be("demo_archive.tar.gz");
        metadata.Value.Format.Should().Be("tar.gz");
        metadata.Value.SourceInstance.Should().Be("demo");
        metadata.Value.DefaultUser.Should().Be("jane");
        metadata.Value.DockerDesktop.Should().Be("no");
        metadata.Value.Sha256.Should().HaveLength(64);
    }

    [Fact]
    public void Blocks_when_an_archive_contains_several_tar_files()
    {
        AddLegacyRootFile();
        AddLegacyInstance();
        AddLegacyArchive(extraTar: true);

        OperationResult<MigrationPlan> plan = CreateMigrator().CreatePlan(null);
        plan.Value.Blockers.Should().NotBeEmpty();

        OperationResult<MigrationOutcome> result = CreateMigrator().Migrate(null);

        result.Error!.Code.Should().Be(ErrorCode.MigrationBlocked);
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-squad").Should().BeFalse();
        _fileSystem.File.Exists(_configPath).Should().BeFalse();
    }

    [Fact]
    public void Skips_instances_that_are_already_migrated()
    {
        AddLegacyRootFile();
        AddLegacyInstance();
        _fileSystem.AddFile($"{_legacyRoot}/demo/.wsl-squad", new MockFileData("{}"));

        OperationResult<MigrationPlan> plan = CreateMigrator().CreatePlan(null);

        plan.Value.Instances.Should().BeEmpty();
        plan.Value.Warnings.Should().Contain(message => message.Contains("already migrated", StringComparison.Ordinal));
    }

    [Fact]
    public void Rollback_restores_the_legacy_state()
    {
        AddLegacyRootFile();
        AddLegacyInstance();
        AddLegacyArchive();
        LegacyMigrator migrator = CreateMigrator();
        OperationResult<MigrationOutcome> migrated = migrator.Migrate(null);
        string backupId = Path.GetFileName(migrated.Value.BackupDirectory);

        OperationResult rollback = migrator.Rollback(backupId);

        rollback.IsSuccess.Should().BeTrue();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-squad").Should().BeFalse();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-fleet").Should().BeTrue();
        _fileSystem.File.Exists($"{_legacyRoot}/demo/.wsl-fleet.migrated").Should().BeFalse();
        _fileSystem.File.Exists($"{_legacyRoot}/archives/demo_archive/archive.json").Should().BeFalse();
        _fileSystem.File.Exists(_configPath).Should().BeFalse();
        _fileSystem.File.ReadAllText($"{_legacyRoot}/demo/instance.json").Should().Contain("Theme");
    }

    [Fact]
    public void Rollback_restores_a_previous_configuration()
    {
        AddLegacyRootFile();
        AddLegacyInstance();
        _fileSystem.AddFile(_configPath, new MockFileData("""{ "fleetRoot": "/old-fleet" }"""));
        LegacyMigrator migrator = CreateMigrator();
        OperationResult<MigrationOutcome> migrated = migrator.Migrate(null);
        string backupId = Path.GetFileName(migrated.Value.BackupDirectory);

        migrator.Rollback(backupId).IsSuccess.Should().BeTrue();

        _fileSystem.File.ReadAllText(_configPath).Should().Contain("/old-fleet");
    }

    [Fact]
    public void Rejects_a_traversal_backup_id()
    {
        OperationResult result = CreateMigrator().Rollback("../../evil");

        result.Error!.Code.Should().Be(ErrorCode.InvalidName);
    }

    [Fact]
    public void Reports_a_missing_legacy_root_file()
    {
        OperationResult<MigrationPlan> result = CreateMigrator().CreatePlan(null);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Accepts_an_explicit_source_root_without_root_file()
    {
        AddLegacyInstance();

        OperationResult<MigrationPlan> result = CreateMigrator().CreatePlan(_legacyRoot);

        result.IsSuccess.Should().BeTrue();
        result.Value.Instances.Should().ContainSingle();
    }
}
