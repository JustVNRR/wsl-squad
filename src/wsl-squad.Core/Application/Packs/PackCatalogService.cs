using System.IO.Abstractions;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Core.Application.Packs;

/// <summary>Lists, duplicates and deletes catalog packs.</summary>
public sealed class PackCatalogService
{
    private readonly IFileSystem _fileSystem;
    private readonly PackCatalogRepository _repository;

    public PackCatalogService(IFileSystem fileSystem, PackCatalogRepository repository)
    {
        _fileSystem = fileSystem;
        _repository = repository;
    }

    public OperationResult<IReadOnlyList<WslPack>> List(string catalogRoot)
    {
        OperationResult<PackCatalogLoadResult> loaded = _repository.Load(catalogRoot);

        return loaded.IsSuccess
            ? OperationResult<IReadOnlyList<WslPack>>.Ok(loaded.Value.Packs)
            : OperationResult<IReadOnlyList<WslPack>>.Fail(
                loaded.Error!.Code,
                loaded.Error.Message,
                loaded.Error.Detail);
    }

    public OperationResult<string> GetDirectory(string catalogRoot, string packName)
    {
        if (!InstanceName.IsValid(packName))
        {
            return OperationResult<string>.Fail(ErrorCode.InvalidName, $"Pack name '{packName}' is invalid.");
        }

        string directory = _fileSystem.Path.Combine(catalogRoot, "packs", packName);

        return _fileSystem.Directory.Exists(directory)
            ? OperationResult<string>.Ok(directory)
            : OperationResult<string>.Fail(ErrorCode.NotFound, $"Pack '{packName}' does not exist.");
    }

    public OperationResult<string> Duplicate(string catalogRoot, string sourceName, string targetName)
    {
        OperationResult<string> source = GetDirectory(catalogRoot, sourceName);

        if (!source.IsSuccess)
        {
            return source;
        }

        if (!InstanceName.IsValid(targetName))
        {
            return OperationResult<string>.Fail(ErrorCode.InvalidName, $"Pack name '{targetName}' is invalid.");
        }

        string target = _fileSystem.Path.Combine(catalogRoot, "packs", targetName);

        if (_fileSystem.Directory.Exists(target))
        {
            return OperationResult<string>.Fail(ErrorCode.AlreadyExists, $"Pack '{targetName}' already exists.");
        }

        try
        {
            CopyDirectory(source.Value, target);

            return OperationResult<string>.Ok(target);
        }
        catch (IOException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                $"Pack '{sourceName}' could not be duplicated.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.PermissionDenied,
                $"Pack '{sourceName}' could not be duplicated.",
                exception.Message);
        }
    }

    public OperationResult Delete(string catalogRoot, string packName)
    {
        OperationResult<string> directory = GetDirectory(catalogRoot, packName);

        if (!directory.IsSuccess)
        {
            return OperationResult.Fail(directory.Error!.Code, directory.Error.Message, directory.Error.Detail);
        }

        try
        {
            _fileSystem.Directory.Delete(directory.Value, recursive: true);

            return OperationResult.Ok();
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Pack '{packName}' could not be deleted.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                $"Pack '{packName}' could not be deleted.",
                exception.Message);
        }
    }

    private void CopyDirectory(string source, string target)
    {
        _ = _fileSystem.Directory.CreateDirectory(target);

        foreach (string file in _fileSystem.Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = _fileSystem.Path.GetRelativePath(source, file);
            string destination = _fileSystem.Path.Combine(target, relative);
            string? destinationDirectory = _fileSystem.Path.GetDirectoryName(destination);

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                _ = _fileSystem.Directory.CreateDirectory(destinationDirectory);
            }

            _fileSystem.File.Copy(file, destination);
        }
    }
}
