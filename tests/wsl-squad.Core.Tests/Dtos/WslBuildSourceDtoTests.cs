using FluentAssertions;

using WslSquad.Core.Dtos;

namespace WslSquad.Core.Tests.Dtos;

public class WslBuildSourceDtoTests
{
    [Fact]
    public void The_fingerprint_is_stripped_from_the_parent_folder()
    {
        WslBuildSourceDto source = new()
        {
            Path = "/catalog/dockerimages/ubuntu-1a2b3c4d/ubuntu-1a2b3c4d.tar",
        };

        source.SuggestedExportFileName.Should().Be("ubuntu.tar");
    }

    [Fact]
    public void The_file_extension_is_kept()
    {
        WslBuildSourceDto source = new()
        {
            Path = "/catalog/onboardings/setup-1a2b3c4d/onboarding.sh",
        };

        source.SuggestedExportFileName.Should().Be("setup.sh");
    }

    [Fact]
    public void A_folder_without_a_fingerprint_is_used_as_is()
    {
        WslBuildSourceDto source = new()
        {
            Path = "/catalog/recipes/default/Dockerfile",
        };

        source.SuggestedExportFileName.Should().Be("default");
    }
}
