using System.IO.Abstractions;

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Infrastructure.Docker;
using wsl_squad.Core.Infrastructure.Environment;
using wsl_squad.Core.Infrastructure.Process;
using wsl_squad.Core.Infrastructure.Windows;
using wsl_squad.Windows.Tests.Support;

namespace wsl_squad.Windows.Tests;

[Trait("Suite", "WindowsIntegration")]
public class IntegrationTests
{
    [WindowsFact]
    public async Task Process_runner_captures_standard_output()
    {
        ProcessRunner runner = new();

        ProcessResult result = await runner.RunAsync(
            new ProcessInvocation("cmd.exe", ["/c", "echo wsl-squad"]));

        result.Succeeded.Should().BeTrue();
        result.StandardOutput.Should().Contain("wsl-squad");
    }

    [WindowsFact]
    public void Registry_gateway_reads_distributions()
    {
        WindowsRegistryGateway gateway = new();

        IReadOnlyList<WslRegistryEntry> entries = gateway.GetDistributions();

        entries.Should().NotBeNull();

        foreach (WslRegistryEntry entry in entries)
        {
            entry.Name.Should().NotBeNullOrWhiteSpace();
            entry.BasePath.Should().NotBeNullOrWhiteSpace();
        }
    }

    [WindowsFact]
    public async Task Wsl_gateway_lists_running_distributions()
    {
        WslCliGateway gateway = new(new ProcessRunner());

        IReadOnlyList<string> running = await gateway.GetRunningDistributionNamesAsync();

        running.Should().NotBeNull();
    }

    [WindowsFact]
    public async Task Docker_gateway_reports_availability()
    {
        DockerCliGateway gateway = new(new ProcessRunner(), new FileSystem());

        bool available = await gateway.IsAvailableAsync();

        available.Should().BeTrue("Docker Desktop is required for the integration suite");
    }

    [WindowsFact]
    public void Terminal_store_reads_unknown_profiles_without_failing()
    {
        WindowsTerminalStore store = new(new FileSystem(), new SystemPlatformEnvironment());

        store.GetProfileGuid("wsl-squad-integration-missing").IsSuccess.Should().BeTrue();
    }

    [WindowsFact]
    public void Docker_desktop_store_can_read_settings()
    {
        DockerDesktopStore store = new(new FileSystem(), new SystemPlatformEnvironment());

        store.Read().IsSuccess.Should().BeTrue();
    }
}
