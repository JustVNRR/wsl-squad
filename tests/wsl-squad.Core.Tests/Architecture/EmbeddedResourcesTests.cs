using FluentAssertions;

using WslSquad.Core.Services;

namespace WslSquad.Core.Tests.Architecture;

public class EmbeddedResourcesTests
{
    [Fact]
    public void Bundled_catalog_resources_are_embedded_at_the_root()
    {
        string[] names =
        [
            .. typeof(ServiceFactory).Assembly.GetManifestResourceNames()
                .Select(name => name.Replace('\\', '/')),
        ];

        names.Should().Contain("ingredients/manage-packs.sh");
        names.Should().Contain("ingredients/manage-packs-bootstrap.sh");
        names.Should().Contain("packs/_oh_my_pack/pack.conf");
        names.Should().Contain("packs/cleanup_orphans.sh");
        names.Should().Contain("recipes/ubuntu-24-04/Dockerfile");
        names.Should().Contain("recipes/ubuntu-24-04/onboarding.sh");
        names.Should().Contain("languages/en.json");
        names.Should().NotContain(name => name.StartsWith("linux/", StringComparison.Ordinal));
    }
}
