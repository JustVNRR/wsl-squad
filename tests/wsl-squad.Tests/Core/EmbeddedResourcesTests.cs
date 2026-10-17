using System.Reflection;

namespace wsl_squad.Tests.Core;

public class EmbeddedResourcesTests
{
    [Fact]
    public void CoreAssemblyEmbedsLinuxResourcesAndSharedFonts()
    {
        string[] resources = Assembly.Load("wsl-squad.Core").GetManifestResourceNames();

        Assert.Contains(resources, name => name.EndsWith("Linux.build.Dockerfile", StringComparison.Ordinal));
        Assert.Contains(resources, name => name.EndsWith("recipes.ubuntu_24_04.Dockerfile", StringComparison.Ordinal));
        Assert.Contains(resources, name => name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase));
    }
}
