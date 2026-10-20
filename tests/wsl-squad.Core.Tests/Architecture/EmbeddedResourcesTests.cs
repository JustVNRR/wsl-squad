using FluentAssertions;

using wsl_squad.Core.Services;

namespace wsl_squad.Core.Tests.Architecture;

public class EmbeddedResourcesTests
{
    [Fact]
    public void Packs_ingredients_and_recipes_are_embedded_at_the_root()
    {
        string[] names =
        [
            .. typeof(ServiceFactory).Assembly.GetManifestResourceNames()
                .Select(name => name.Replace('\\', '/')),
        ];

        _ = names.Should().Contain("ingredients/Dockerfile");
        _ = names.Should().Contain("ingredients/onboarding.sh");
        _ = names.Should().Contain("packs/_oh_my_pack/pack.conf");
        _ = names.Should().Contain("packs/cleanup_orphans.sh");
        _ = names.Should().Contain("recipes/ubuntu-24-04/Dockerfile");
        _ = names.Should().Contain("recipes/ubuntu-24-04/onboarding.sh");
        _ = names.Should().NotContain(name => name.StartsWith("linux/", StringComparison.Ordinal));
    }
}
