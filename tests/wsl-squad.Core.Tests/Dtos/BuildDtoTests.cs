using FluentAssertions;

using WslSquad.Core.Dtos;

namespace WslSquad.Core.Tests.Dtos;

public class BuildDtoTests
{
    [Fact]
    public void Keep_image_defaults_to_false()
    {
        new BuildDto().KeepImage.Should().BeFalse();
    }

    [Fact]
    public void A_recipe_uses_its_folder_as_context()
    {
        BuildDto dto = BuildDto.FromRecipe(
            "alpha",
            "default",
            "/catalog/default",
            keepImage: true,
            interactive: true);

        dto.Name.Should().Be("alpha");
        dto.RecipeName.Should().Be("default");
        dto.BuildPath.Should().Be(Path.Combine("/catalog/default", "Dockerfile"));
        dto.Context.Should().Be("/catalog/default");
        dto.KeepImage.Should().BeTrue();
        dto.UseSavedImage.Should().BeFalse();
        dto.Interactive.Should().BeTrue();
    }

    [Fact]
    public void A_recipe_can_reuse_its_saved_image()
    {
        BuildDto dto = BuildDto.FromRecipe(
            "alpha",
            "default",
            "/catalog/default",
            useSavedImage: true);

        dto.UseSavedImage.Should().BeTrue();
        dto.KeepImage.Should().BeFalse();
    }
}
