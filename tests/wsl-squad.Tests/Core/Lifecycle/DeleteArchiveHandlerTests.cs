using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Lifecycle;

public class DeleteArchiveHandlerTests
{
    private readonly LifecycleFixture _fixture = new();

    private DeleteArchiveHandler CreateHandler()
    {
        return new DeleteArchiveHandler(_fixture.FileSystem, _fixture.Archives);
    }

    [Fact]
    public void Deletes_a_managed_archive()
    {
        _fixture.AddArchive("old");

        OperationResult<DeleteArchiveOutcome> result = CreateHandler().Delete(LifecycleFixture.Root, "old");

        result.IsSuccess.Should().BeTrue();
        _fixture.FileSystem.Directory.Exists("/fleet/archives/old").Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_invalid_archive_name()
    {
        OperationResult<DeleteArchiveOutcome> result = CreateHandler().Delete(LifecycleFixture.Root, "..");

        result.Error!.Code.Should().Be(ErrorCode.InvalidName);
    }

    [Fact]
    public void Rejects_a_missing_archive()
    {
        OperationResult<DeleteArchiveOutcome> result = CreateHandler().Delete(LifecycleFixture.Root, "missing");

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Rejects_an_unmanaged_folder()
    {
        _fixture.FileSystem.AddFile("/fleet/archives/raw/file.txt", new MockFileData("data"));

        OperationResult<DeleteArchiveOutcome> result = CreateHandler().Delete(LifecycleFixture.Root, "raw");

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }
}
