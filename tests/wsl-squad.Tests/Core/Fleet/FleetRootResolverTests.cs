using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Fleet;

public class FleetRootResolverTests
{
    private const string _configPath = "/config/config.json";

    private static (FleetRootResolver Resolver, MockFileSystem FileSystem, FakePlatformEnvironment Platform, FakeWslRegistryGateway Registry) Create()
    {
        MockFileSystem fileSystem = new();
        FakePlatformEnvironment platform = new();
        FakeWslRegistryGateway registry = new();
        FleetRootResolver resolver = new(
            platform,
            new FleetConfigStore(fileSystem, _configPath),
            registry,
            new FleetMarkerStore(fileSystem));

        return (resolver, fileSystem, platform, registry);
    }

    private static void AddMarker(MockFileSystem fileSystem, string directory)
    {
        fileSystem.AddFile($"{directory}/.wsl-squad", new MockFileData("{}"));
    }

    [Fact]
    public void Environment_override_wins_over_configuration()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, FakePlatformEnvironment platform, _) = Create();
        fileSystem.AddFile(_configPath, new MockFileData("""{ "fleetRoot": "/from-config" }"""));
        platform.SetVariable(AppPaths.FleetRootVariable, "/from-env");

        resolver.Resolve().Value.Should().Be("/from-env");
    }

    [Fact]
    public void Configuration_wins_over_discovery()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, _, FakeWslRegistryGateway registry) = Create();
        fileSystem.AddFile(_configPath, new MockFileData("""{ "fleetRoot": "/from-config" }"""));
        registry.Add("demo", "/discovered/demo");
        AddMarker(fileSystem, "/discovered/demo");

        resolver.Resolve().Value.Should().Be("/from-config");
    }

    [Fact]
    public void Discovers_a_unique_marked_root()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, _, FakeWslRegistryGateway registry) = Create();
        registry.Add("alpha", "/fleet/alpha");
        registry.Add("beta", "/fleet/beta");
        AddMarker(fileSystem, "/fleet/alpha");
        AddMarker(fileSystem, "/fleet/beta");

        resolver.Resolve().Value.Should().Be("/fleet");
    }

    [Fact]
    public void Fails_when_several_marked_roots_exist()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, _, FakeWslRegistryGateway registry) = Create();
        registry.Add("alpha", "/fleet-a/alpha");
        registry.Add("beta", "/fleet-b/beta");
        AddMarker(fileSystem, "/fleet-a/alpha");
        AddMarker(fileSystem, "/fleet-b/beta");

        OperationResult<string> result = resolver.Resolve();

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    [Fact]
    public void Falls_back_to_the_default_root()
    {
        (FleetRootResolver resolver, _, _, _) = Create();

        resolver.Resolve().Value.Should().Be("/home/test/WSL/wsl-squad");
    }

    [Fact]
    public void Rejects_a_relative_override()
    {
        (FleetRootResolver resolver, _, FakePlatformEnvironment platform, _) = Create();
        platform.SetVariable(AppPaths.FleetRootVariable, "relative/fleet");

        OperationResult<string> result = resolver.Resolve();

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Propagates_a_corrupt_configuration()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, _, _) = Create();
        fileSystem.AddFile(_configPath, new MockFileData("{broken"));

        OperationResult<string> result = resolver.Resolve();

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }

    [Fact]
    public void Skips_discovery_when_the_registry_is_unsupported()
    {
        (FleetRootResolver resolver, MockFileSystem fileSystem, _, FakeWslRegistryGateway registry) = Create();
        registry.IsSupported = false;
        registry.Add("alpha", "/fleet/alpha");
        AddMarker(fileSystem, "/fleet/alpha");

        resolver.Resolve().Value.Should().Be("/home/test/WSL/wsl-squad");
    }
}
