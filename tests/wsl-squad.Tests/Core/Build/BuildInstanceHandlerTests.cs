using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Application.Build;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Build;

public class BuildInstanceHandlerTests
{
    private const string _root = "/fleet";
    private const string _catalog = "/catalog";
    private const string _buildId = "20261017T120000Z-abcdef12";
    private static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeWslGateway _wsl = new();
    private readonly FakeDockerGateway _docker = new();
    private readonly FakeWslRegistryGateway _registry = new();
    private readonly FakeClock _clock = new(_now);

    private BuildInstanceHandler CreateHandler()
    {
        return new BuildInstanceHandler(
            _fileSystem,
            _wsl,
            _docker,
            new FleetMarkerStore(_fileSystem),
            new InstanceMetadataStore(_fileSystem, _clock),
            new BuildPlanFactory(_fileSystem, _registry, new FakeBuildIdGenerator()),
            _clock);
    }

    private void AddDockerfile()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));
    }

    private static BuildRequest DockerfileRequest(string name = "demo", bool keepImage = false, string? onboarding = null)
    {
        return new BuildRequest
        {
            Name = name,
            SourceKind = BuildSourceKind.Dockerfile,
            SourcePath = "/source/Dockerfile",
            KeepImage = keepImage,
            OnboardingPath = onboarding,
        };
    }

    [Fact]
    public async Task Builds_exports_imports_and_records_metadata()
    {
        AddDockerfile();

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(_root, _catalog, DockerfileRequest());

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(OperationStatus.Success);
        result.Value.Name.Should().Be("demo");
        result.Value.ImageTag.Should().Be($"wsl-squad/demo:{_buildId}");
        _wsl.ImportedNames.Should().Equal("demo");
        _docker.Calls.Should().Contain($"build wsl-squad/demo:{_buildId}");
        _docker.Calls.Should().Contain($"export wsl-squad/demo:{_buildId}->/fleet/.staging/{_buildId}/rootfs.tar");
        _docker.Calls.Should().Contain($"rmi wsl-squad/demo:{_buildId}");
        _fileSystem.File.Exists("/fleet/demo/.wsl-squad").Should().BeTrue();
        _fileSystem.File.Exists("/fleet/demo/instance.json").Should().BeTrue();
        _fileSystem.Directory.Exists($"/fleet/.staging/{_buildId}").Should().BeFalse();

        OperationResult<InstanceMetadata> metadata = new InstanceMetadataStore(_fileSystem, _clock)
            .Read("/fleet/demo");
        metadata.Value.Name.Should().Be("demo");
        metadata.Value.DefaultUser.Should().Be("root");
        metadata.Value.Source!.Kind.Should().Be("Dockerfile");
        metadata.Value.Source.ImageTag.Should().Be($"wsl-squad/demo:{_buildId}");
    }

    [Fact]
    public async Task Refuses_to_build_when_docker_is_unavailable()
    {
        AddDockerfile();
        _docker.Available = false;

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(_root, _catalog, DockerfileRequest());

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.DockerUnavailable);
        _docker.Calls.Should().BeEmpty();
        _wsl.ImportedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_an_invalid_request_before_running_any_tool()
    {
        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            DockerfileRequest("bad name"));

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.InvalidName);
        _docker.Calls.Should().BeEmpty();
        _wsl.ImportedNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Rolls_back_image_and_folder_when_import_fails()
    {
        AddDockerfile();
        _wsl.ImportFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--import"]),
            new ProcessResult(1, string.Empty, "import failed"));

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(_root, _catalog, DockerfileRequest());

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.ExternalTool);
        result.Error.ExternalExitCode.Should().Be(1);
        _wsl.UnregisteredNames.Should().Contain("demo");
        _docker.Calls.Should().Contain($"rmi wsl-squad/demo:{_buildId}");
        _fileSystem.Directory.Exists("/fleet/demo").Should().BeFalse();
        _fileSystem.Directory.Exists($"/fleet/.staging/{_buildId}").Should().BeFalse();
    }

    [Fact]
    public async Task Imports_an_existing_image_archive()
    {
        _fileSystem.AddFile("/images/base.tar", new MockFileData("tar"));
        _docker.LoadedImageReference = "ubuntu:24.04";

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Image,
                SourcePath = "/images/base.tar",
            });

        result.IsSuccess.Should().BeTrue();
        _docker.Calls.Should().Contain("load /images/base.tar");
        _docker.Calls.Should().Contain("export ubuntu:24.04->/fleet/.staging/" + _buildId + "/rootfs.tar");
        _docker.Calls.Should().NotContain(call => call.StartsWith("rmi", StringComparison.Ordinal));

        OperationResult<InstanceMetadata> metadata = new InstanceMetadataStore(_fileSystem, _clock)
            .Read("/fleet/demo");
        metadata.Value.Source!.Kind.Should().Be("Image");
    }

    [Fact]
    public async Task Imports_a_registry_reference_directly()
    {
        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Registry,
                SourcePath = "ubuntu:24.04",
            });

        result.IsSuccess.Should().BeTrue();
        _docker.Calls.Should().Contain("export ubuntu:24.04->/fleet/.staging/" + _buildId + "/rootfs.tar");
        _docker.Calls.Should().NotContain(call => call.StartsWith("build", StringComparison.Ordinal) ||
                                                   call.StartsWith("load", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Saves_the_image_when_keep_image_is_requested()
    {
        AddDockerfile();

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            DockerfileRequest(keepImage: true));

        string expectedPath = $"/catalog/images/demo/{_buildId}.tar";
        result.IsSuccess.Should().BeTrue();
        result.Value.SavedImagePath.Should().Be(expectedPath);
        _docker.Calls.Should().Contain($"save wsl-squad/demo:{_buildId}->{expectedPath}");
        _docker.Calls.Should().Contain($"rmi wsl-squad/demo:{_buildId}");
    }

    [Fact]
    public async Task Reports_a_warning_when_saving_the_image_fails()
    {
        AddDockerfile();
        _docker.SaveFailure = new ProcessExecutionException(
            new ProcessInvocation("docker", ["save"]),
            new ProcessResult(1, string.Empty, "save failed"));

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            DockerfileRequest(keepImage: true));

        result.Status.Should().Be(OperationStatus.Warning);
        result.Value.SavedImagePath.Should().BeNull();
        result.Warnings.Should().Contain(warning => warning.Contains("image save", StringComparison.Ordinal));
        _fileSystem.File.Exists("/fleet/demo/.wsl-squad").Should().BeTrue();
    }

    [Fact]
    public async Task Runs_onboarding_and_terminates_the_distribution()
    {
        AddDockerfile();
        _fileSystem.AddFile(@"C:\scripts\onboarding.sh", new MockFileData("#!/bin/bash"));

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            DockerfileRequest(onboarding: @"C:\scripts\onboarding.sh"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Onboarded.Should().BeTrue();
        _wsl.CopiedFiles.Should().Contain(@"demo:C:\scripts\onboarding.sh->/root/onboarding.sh");
        _wsl.OnboardedNames.Should().Equal("demo");
        _wsl.TerminatedNames.Should().Equal("demo");
    }

    [Fact]
    public async Task Keeps_the_instance_when_onboarding_fails()
    {
        AddDockerfile();
        _fileSystem.AddFile(@"C:\scripts\onboarding.sh", new MockFileData("#!/bin/bash"));
        _wsl.OnboardingFailure = new ProcessExecutionException(
            new ProcessInvocation("wsl.exe", ["--", "bash"]),
            new ProcessResult(1, string.Empty, "onboarding failed"));

        OperationResult<BuildSummary> result = await CreateHandler().BuildAsync(
            _root,
            _catalog,
            DockerfileRequest(onboarding: @"C:\scripts\onboarding.sh"));

        result.Status.Should().Be(OperationStatus.Warning);
        result.Value.Onboarded.Should().BeFalse();
        result.Warnings.Should().Contain(warning => warning.Contains("onboarding", StringComparison.Ordinal));
        _fileSystem.File.Exists("/fleet/demo/.wsl-squad").Should().BeTrue();
        _fileSystem.File.Exists("/fleet/demo/instance.json").Should().BeTrue();
    }
}
