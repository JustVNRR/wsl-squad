using System.Reflection;

using FluentAssertions;

namespace wsl_squad.Tests.Core;

public class EmbeddedResourcesTests
{
    private static readonly string[] _resourceNames =
    [
        .. Assembly.Load("wsl-squad.Core")
            .GetManifestResourceNames()
            .Select(name => name.Replace('\\', '/')),
    ];

    [Fact]
    public void CoreAssemblyEmbedsBuildAndRecipeResources()
    {
        _resourceNames.Should().Contain("linux/build/Dockerfile");
        _resourceNames.Should().Contain("linux/build/onboarding.sh");
        _resourceNames.Should().Contain("recipes/ubuntu-24-04/Dockerfile");
        _resourceNames.Should().Contain("recipes/ubuntu-24-04/onboarding.sh");
    }

    [Fact]
    public void CoreAssemblyEmbedsSharedFonts()
    {
        _resourceNames
            .Where(name => name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            .Should()
            .BeEquivalentTo(
                "fonts/FontAwesome/fa-solid-900.ttf",
                "fonts/MesloLGS NF Regular-7c66a6d9/MesloLGS NF Regular.ttf",
                "fonts/VT323/VT323-Regular.ttf");
    }

    [Theory]
    [InlineData("linux/packs/oh_my_code/bin/claude.sh")]
    [InlineData("linux/packs/oh_my_code/bin/statusline.sh")]
    [InlineData("linux/packs/oh_my_doc/bin/csl.sh")]
    [InlineData("linux/packs/oh_my_doc/bin/document.sh")]
    [InlineData("linux/packs/oh_my_doc/bin/font.sh")]
    [InlineData("linux/packs/oh_my_doc/bin/font_google.sh")]
    [InlineData("linux/packs/oh_my_doc/bin/font_ubuntu.sh")]
    [InlineData("linux/packs/oh_my_web/bin/fox.sh")]
    [InlineData("linux/packs/oh_my_web/bin/vpn-boot.sh")]
    [InlineData("linux/packs/oh_my_web/bin/vpn-migrate.sh")]
    [InlineData("linux/packs/oh_my_web/bin/vpn.sh")]
    public void CoreAssemblyEmbedsPackBinScripts(string expectedResource)
    {
        _resourceNames.Should().Contain(expectedResource);
    }
}
