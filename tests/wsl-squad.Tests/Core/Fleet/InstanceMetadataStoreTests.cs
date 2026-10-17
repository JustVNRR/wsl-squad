using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Fleet;

public class InstanceMetadataStoreTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 17, 15, 30, 0, TimeSpan.Zero);
    private const string _instanceDirectory = "/fleet/demo";

    private static InstanceMetadataStore CreateStore(MockFileSystem fileSystem)
    {
        return new InstanceMetadataStore(fileSystem, new FakeClock(_now));
    }

    [Fact]
    public void Save_sets_the_update_time_and_round_trips()
    {
        MockFileSystem fileSystem = new();
        InstanceMetadataStore store = CreateStore(fileSystem);

        store.Save(_instanceDirectory, new InstanceMetadata
        {
            Id = "id-1",
            Name = "demo",
            DefaultUser = "jane",
            DockerDesktop = "yes",
        }).IsSuccess.Should().BeTrue();

        OperationResult<InstanceMetadata> metadata = store.Read(_instanceDirectory);
        metadata.IsSuccess.Should().BeTrue();
        metadata.Value.Name.Should().Be("demo");
        metadata.Value.DefaultUser.Should().Be("jane");
        metadata.Value.DockerDesktop.Should().Be("yes");
        metadata.Value.UpdatedUtc.Should().Be(_now);
    }

    [Fact]
    public void Read_preserves_unknown_fields()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile(
            "/fleet/demo/instance.json",
            new MockFileData("""{ "schemaVersion": 1, "name": "demo", "customField": "keep" }"""));
        InstanceMetadataStore store = CreateStore(fileSystem);

        OperationResult<InstanceMetadata> metadata = store.Read(_instanceDirectory);

        metadata.IsSuccess.Should().BeTrue();
        metadata.Value.ExtensionData.Should().ContainKey("customField");
    }

    [Fact]
    public void Saving_after_a_read_keeps_unknown_fields()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile(
            "/fleet/demo/instance.json",
            new MockFileData("""{ "schemaVersion": 1, "name": "demo", "customField": "keep" }"""));
        InstanceMetadataStore store = CreateStore(fileSystem);

        InstanceMetadata metadata = store.Read(_instanceDirectory).Value;
        store.Save(_instanceDirectory, metadata);

        fileSystem.File.ReadAllText("/fleet/demo/instance.json").Should().Contain("customField");
    }

    [Fact]
    public void Read_reports_a_missing_file()
    {
        OperationResult<InstanceMetadata> result = CreateStore(new MockFileSystem()).Read(_instanceDirectory);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Read_reports_corrupt_json()
    {
        MockFileSystem fileSystem = new();
        fileSystem.AddFile("/fleet/demo/instance.json", new MockFileData("{broken"));

        OperationResult<InstanceMetadata> result = CreateStore(fileSystem).Read(_instanceDirectory);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }
}
