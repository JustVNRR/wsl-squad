using FluentAssertions;

using WslSquad.Core.Dtos;

namespace WslSquad.Core.Tests.Dtos;

public class BuildDtoTests
{
    [Fact]
    public void A_dockerfile_source_uses_its_folder_as_context()
    {
        BuildDto dto = BuildDto.FromSource(
            "alpha",
            "alice",
            WslBuildType.Dockerfile,
            "/catalog/ubuntu/Dockerfile",
            "/catalog/setup/onboarding.sh",
            keepImage: true,
            registerDocker: true,
            interactive: true);

        dto.BuildPath.Should().Be("/catalog/ubuntu/Dockerfile");
        dto.Context.Should().Be("/catalog/ubuntu");
        dto.User.Should().Be("alice");
        dto.Onboarding.Should().Be("/catalog/setup/onboarding.sh");
        dto.KeepImage.Should().BeTrue();
        dto.RegisterDocker.Should().BeTrue();
        dto.Interactive.Should().BeTrue();
    }

    [Fact]
    public void A_recipe_source_uses_its_folder_and_carries_no_account_options()
    {
        BuildDto dto = BuildDto.FromSource(
            "alpha",
            "alice",
            WslBuildType.Recipe,
            "/catalog/default",
            "/catalog/setup/onboarding.sh",
            keepImage: true,
            registerDocker: true);

        dto.BuildPath.Should().Be(Path.Combine("/catalog/default", "Dockerfile"));
        dto.Context.Should().Be("/catalog/default");
        dto.User.Should().BeEmpty();
        dto.Onboarding.Should().BeEmpty();
        dto.KeepImage.Should().BeFalse();
        dto.RegisterDocker.Should().BeFalse();
        dto.Interactive.Should().BeTrue();
    }

    [Fact]
    public void An_image_source_is_the_build_path_without_context()
    {
        BuildDto dto = BuildDto.FromSource(
            "alpha",
            "alice",
            WslBuildType.Image,
            "/catalog/images/ubuntu.tar",
            keepImage: true);

        dto.BuildPath.Should().Be("/catalog/images/ubuntu.tar");
        dto.Context.Should().BeEmpty();
        dto.KeepImage.Should().BeTrue();
    }

    [Fact]
    public void A_registry_reference_is_the_build_path_without_context()
    {
        BuildDto dto = BuildDto.FromSource(
            "alpha",
            "alice",
            WslBuildType.Registry,
            "alpine:latest");

        dto.BuildPath.Should().Be("alpine:latest");
        dto.Context.Should().BeEmpty();
    }
}
