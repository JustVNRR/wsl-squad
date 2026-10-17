using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Infrastructure.Docker;

namespace wsl_squad.Tests.Core.Docker;

public class DockerCommandBuilderTests
{
    [Fact]
    public void Info_uses_docker_info()
    {
        ProcessInvocation invocation = DockerCommandBuilder.Info();

        invocation.FileName.Should().Be("docker");
        invocation.Arguments.Should().Equal("info");
    }

    [Fact]
    public void Build_uses_argument_list_without_shell_quoting()
    {
        ProcessInvocation invocation = DockerCommandBuilder.Build(
            "wsl-squad/demo:build",
            "C:\\repo\\Dockerfile",
            "C:\\repo");

        invocation.Arguments.Should().Equal(
            "build",
            "-t",
            "wsl-squad/demo:build",
            "-f",
            "C:\\repo\\Dockerfile",
            "C:\\repo");
    }

    [Fact]
    public void Load_create_export_and_remove_use_expected_arguments()
    {
        DockerCommandBuilder.Load("C:\\images\\base.tar").Arguments
            .Should().Equal("load", "-i", "C:\\images\\base.tar");

        DockerCommandBuilder.CreateContainer("wsl-squad-export-x", "ubuntu:24.04").Arguments
            .Should().Equal("create", "--name", "wsl-squad-export-x", "ubuntu:24.04");

        DockerCommandBuilder.ExportContainer("C:\\tar\\rootfs.tar", "wsl-squad-export-x").Arguments
            .Should().Equal("export", "-o", "C:\\tar\\rootfs.tar", "wsl-squad-export-x");

        DockerCommandBuilder.RemoveContainer("wsl-squad-export-x").Arguments
            .Should().Equal("rm", "-f", "wsl-squad-export-x");
    }

    [Fact]
    public void Save_and_remove_image_use_expected_arguments()
    {
        DockerCommandBuilder.Save("C:\\images\\demo.tar", "wsl-squad/demo:build").Arguments
            .Should().Equal("save", "-o", "C:\\images\\demo.tar", "wsl-squad/demo:build");

        DockerCommandBuilder.RemoveImage("wsl-squad/demo:build").Arguments
            .Should().Equal("rmi", "-f", "wsl-squad/demo:build");
    }
}
