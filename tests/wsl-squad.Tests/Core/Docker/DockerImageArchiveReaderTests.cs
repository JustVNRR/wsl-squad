using System.Formats.Tar;
using System.Text;

using FluentAssertions;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Infrastructure.Docker;

namespace wsl_squad.Tests.Core.Docker;

public class DockerImageArchiveReaderTests
{
    [Fact]
    public void Reads_the_first_repository_tag_from_the_manifest()
    {
        using MemoryStream archive = CreateArchive("""[{"RepoTags":["ubuntu:24.04"]}]""");
        archive.Position = 0;

        OperationResult<string> result = DockerImageArchiveReader.ReadImageReference(archive);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ubuntu:24.04");
    }

    [Fact]
    public void Fails_when_the_manifest_has_no_tags()
    {
        using MemoryStream archive = CreateArchive("""[{"RepoTags":[]}]""");
        archive.Position = 0;

        OperationResult<string> result = DockerImageArchiveReader.ReadImageReference(archive);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }

    [Fact]
    public void Fails_when_the_archive_has_no_manifest()
    {
        using MemoryStream archive = new();
        using (TarWriter writer = new(archive, leaveOpen: true))
        {
            using MemoryStream data = new(Encoding.UTF8.GetBytes("readme"));
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "readme.txt")
            {
                DataStream = data,
            });
        }

        archive.Position = 0;
        OperationResult<string> result = DockerImageArchiveReader.ReadImageReference(archive);

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.CorruptState);
    }

    private static MemoryStream CreateArchive(string manifestJson)
    {
        MemoryStream archive = new();

        using (TarWriter writer = new(archive, leaveOpen: true))
        {
            using MemoryStream data = new(Encoding.UTF8.GetBytes(manifestJson));
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "manifest.json")
            {
                DataStream = data,
            });
        }

        return archive;
    }
}
