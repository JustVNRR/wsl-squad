using System.IO.Abstractions;

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Packs;

namespace wsl_squad.Core.Application.Packs;

/// <summary>Loads the pack catalog from <c>catalog/packs</c>.</summary>
public sealed class PackCatalogRepository
{
    private readonly IFileSystem _fileSystem;

    public PackCatalogRepository(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public OperationResult<PackCatalogLoadResult> Load(string catalogRoot)
    {
        string packsRoot = _fileSystem.Path.Combine(catalogRoot, "packs");
        List<WslPack> packs = [];
        List<string> warnings = [];

        if (!_fileSystem.Directory.Exists(packsRoot))
        {
            return OperationResult<PackCatalogLoadResult>.Ok(new PackCatalogLoadResult(packs, warnings));
        }

        try
        {
            foreach (string directory in _fileSystem.Directory.GetDirectories(packsRoot))
            {
                string name = _fileSystem.Path.GetFileName(directory);

                if (!InstanceName.IsValid(name))
                {
                    warnings.Add($"Pack folder '{name}' has an invalid name.");
                    continue;
                }

                string confPath = _fileSystem.Path.Combine(directory, "pack.conf");

                if (!_fileSystem.File.Exists(confPath))
                {
                    warnings.Add($"Pack '{name}' has no pack.conf.");
                    continue;
                }

                PackConf conf = PackConfParser.Parse(_fileSystem.File.ReadAllText(confPath));
                packs.Add(new WslPack(
                    name,
                    directory,
                    conf.Description,
                    conf.Family,
                    conf.Requires,
                    conf.Visible,
                    conf.Welcome));
            }
        }
        catch (IOException exception)
        {
            return OperationResult<PackCatalogLoadResult>.Fail(
                ErrorCode.IoError,
                $"Pack catalog '{packsRoot}' could not be read.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<PackCatalogLoadResult>.Fail(
                ErrorCode.PermissionDenied,
                $"Pack catalog '{packsRoot}' could not be read.",
                exception.Message);
        }

        packs.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        return OperationResult<PackCatalogLoadResult>.Ok(new PackCatalogLoadResult(packs, warnings));
    }
}
