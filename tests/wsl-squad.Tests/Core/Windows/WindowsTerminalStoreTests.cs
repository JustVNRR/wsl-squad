using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Infrastructure.Windows;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Windows;

public class WindowsTerminalStoreTests
{
    private const string _fragmentsRoot =
        "/home/test/.local/share/Microsoft/Windows Terminal/Fragments";

    private readonly MockFileSystem _fileSystem = new();

    private WindowsTerminalStore CreateStore()
    {
        return new WindowsTerminalStore(_fileSystem, new FakePlatformEnvironment());
    }

    [Fact]
    public void Finds_the_profile_guid_of_an_instance()
    {
        _fileSystem.AddFile(
            $"{_fragmentsRoot}/Microsoft.WSL/Ubuntu.json",
            new MockFileData(
                """{"profiles":[{"name":"demo","guid":"{11111111-1111-1111-1111-111111111111}"}]}"""));

        CreateStore().GetProfileGuid("demo").Value
            .Should().Be("{11111111-1111-1111-1111-111111111111}");
    }

    [Fact]
    public void Returns_null_when_no_profile_matches()
    {
        CreateStore().GetProfileGuid("demo").Value.Should().BeNull();
    }

    [Fact]
    public void Writes_and_deletes_an_instance_fragment()
    {
        WindowsTerminalStore store = CreateStore();

        store.WriteFragment(
            "demo",
            new TerminalFragmentSettings("guid-1", @"C:\icons\demo.png", "MesloLGS NF", "One Half Dark", "Demo"))
            .IsSuccess.Should().BeTrue();

        string path = $"{_fragmentsRoot}/wsl-squad/demo.json";
        _fileSystem.File.Exists(path).Should().BeTrue();
        string json = _fileSystem.File.ReadAllText(path);
        json.Should().Contain("\"updates\": \"guid-1\"");
        json.Should().Contain("\"face\": \"MesloLGS NF\"");
        json.Should().Contain("\"colorScheme\": \"One Half Dark\"");
        json.Should().Contain("\"tabTitle\": \"Demo\"");
        json.Should().Contain("\"suppressApplicationTitle\": true");

        store.DeleteFragment("demo").IsSuccess.Should().BeTrue();
        _fileSystem.File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Parses_colour_schemes_from_jsonc_settings()
    {
        _fileSystem.AddFile(
            "/home/test/.local/share/Microsoft/Windows Terminal/settings.json",
            new MockFileData(
                """
                {
                  // Windows Terminal settings
                  "schemes": [
                    {
                      "name": "One Half Dark",
                      "background": "#282C34",
                      "foreground": "#DCDFE4",
                      "black": "#282C34",
                      "red": "#E06C75",
                      "white": "#DCDFE4",
                    },
                  ]
                }
                """));

        IReadOnlyList<WslColorScheme> schemes = CreateStore().GetColorSchemes().Value;

        schemes.Should().ContainSingle();
        schemes[0].Name.Should().Be("One Half Dark");
        schemes[0].Background.Should().Be("#282C34");
        schemes[0].Colors.Should().HaveCount(16);
        schemes[0].Colors[1].Should().Be("#E06C75");
    }

    [Fact]
    public void Uses_the_palette_when_named_colours_are_missing()
    {
        _fileSystem.AddFile(
            "/home/test/.local/share/Microsoft/Windows Terminal/settings.json",
            new MockFileData(
                """{"schemes":[{"name":"Palette","palette":["#000001","#000002"]}]}"""));

        WslColorScheme scheme = CreateStore().GetColorSchemes().Value.Single();

        scheme.Colors[0].Should().Be("#000001");
        scheme.Colors[1].Should().Be("#000002");
        scheme.Colors[2].Should().Be("#000000");
    }
}
