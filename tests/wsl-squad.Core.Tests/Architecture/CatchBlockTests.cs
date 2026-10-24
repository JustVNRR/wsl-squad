using System.Text.RegularExpressions;

using FluentAssertions;

namespace WslSquad.Core.Tests.Architecture;

public partial class CatchBlockTests
{
    [GeneratedRegex(
        @"catch\s*(?:\([^)]*\))?\s*(?:when\s*\([^)]*\))?\s*\{\s*\}",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmptyCatch();

    [Fact]
    public void Source_catch_blocks_are_not_empty()
    {
        string root = RepositoryRoot();
        string source = Path.Combine(root, "src");
        string[] files =
        [
            .. Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)),
        ];

        string[] offenders =
        [
            .. files.Where(path => EmptyCatch().IsMatch(File.ReadAllText(path))),
        ];

        files.Should().NotBeEmpty();
        offenders.Should().BeEmpty();
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
