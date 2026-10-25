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

    [Fact]
    public void Bundled_text_assets_use_lf_line_endings()
    {
        System.Reflection.Assembly assembly = typeof(ServiceFactory).Assembly;
        string[] names =
        [
            .. assembly.GetManifestResourceNames()
                .Where(name => !name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)),
        ];

        names.Should().NotBeEmpty();

        foreach (string name in names)
        {
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            using StreamReader reader = new(stream);
            string text = reader.ReadToEnd();

            text.Contains('\r').Should().BeFalse($"{name} must use LF line endings");
        }
    }
}
