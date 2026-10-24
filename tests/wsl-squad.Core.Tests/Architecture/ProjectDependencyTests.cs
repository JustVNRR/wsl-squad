using FluentAssertions;

namespace WslSquad.Core.Tests.Architecture;

public class ProjectDependencyTests
{
    [Fact]
    public void The_gui_does_not_depend_on_the_cli()
    {
        string root = RepositoryRoot();
        string project = Path.Combine(root, "src", "wsl-squad.UI.Wpf", "wsl-squad.UI.Wpf.csproj");
        string text = File.ReadAllText(project);

        text.Should().NotContain("wsl-squad.UI.Cli");
        text.Should().NotContain("CopyCli");
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "wsl-squad.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("The repository root could not be found.");
    }
}
