using WslSquad.Core.Exceptions;
using System.Diagnostics;
using System.Text;

using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public sealed class PackCatalogRepository : IPackCatalogRepository
{
    private readonly string _packsRoot;

    public PackCatalogRepository(string packsRoot)
    {
        _packsRoot = packsRoot;
    }

    public void SeedFrom(string sourceRoot)
    {
        if (Directory.Exists(_packsRoot) || !Directory.Exists(sourceRoot))
        {
            return;
        }

        Directory.CreateDirectory(_packsRoot);

        foreach (string directory in Directory.EnumerateDirectories(sourceRoot))
        {
            CopyTree(directory, Path.Combine(_packsRoot, Path.GetFileName(directory)));
        }
    }

    public string GetRoot()
    {
        return _packsRoot;
    }

    private string GetFolder(string packName)
    {
        return Path.Combine(_packsRoot, packName);
    }

    public IReadOnlyList<WslPack> GetAllPacks()
    {
        if (!Directory.Exists(_packsRoot))
        {
            return [];
        }

        List<WslPack> packs = [];

        foreach (string directory in Directory.EnumerateDirectories(_packsRoot).Order(StringComparer.Ordinal))
        {
            if (File.Exists(Path.Combine(directory, "pack.conf")))
            {
                packs.Add(GetPack(directory));
            }
        }

        return packs;
    }

    public void SavePackDescription(string packName, string description)
    {
        string confFile = Path.Combine(GetFolder(packName), "pack.conf");
        List<string> rewritten = [];
        bool found = false;

        foreach (string line in File.ReadLines(confFile, Encoding.UTF8))
        {
            if (line.TrimStart().StartsWith("PACK_DESCRIPTION", StringComparison.Ordinal) &&
                line.Contains(":=", StringComparison.Ordinal))
            {
                found = true;
                rewritten.Add($"PACK_DESCRIPTION := {description}");
            }
            else
            {
                rewritten.Add(line);
            }
        }

        if (!found)
        {
            rewritten.Add($"PACK_DESCRIPTION := {description}");
        }

        AtomicFile.WriteText(confFile, string.Join(Environment.NewLine, rewritten) + Environment.NewLine);
    }

    public void CopyPack(string sourceName, string newName)
    {
        if (Directory.Exists(GetFolder(newName)))
        {
            throw new WslSquadException(WslSquadErrorCode.PackAlreadyExists, newName);
        }

        CopyTree(GetFolder(sourceName), GetFolder(newName));
    }

    public void DeletePack(string packName)
    {
        Directory.Delete(GetFolder(packName), recursive: true);
    }

    public void OpenFolder(string packName)
    {
        Process.Start(new ProcessStartInfo(GetFolder(packName)) { UseShellExecute = true });
    }

    private WslPack GetPack(string folder)
    {
        WslPack pack = new WslPack().WithName(Path.GetFileName(folder));

        foreach (string line in File.ReadLines(Path.Combine(folder, "pack.conf"), Encoding.UTF8))
        {
            string text = line.Trim();

            if (TryValue(text, "PACK_DESCRIPTION", out string description))
            {
                pack.WithDescription(description);
            }
            else if (TryValue(text, "PACK_REQUIRES", out string requires))
            {
                pack.WithRequires(
                    [.. requires.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)]);
            }
            else if (TryValue(text, "PACK_VISIBLE", out string visible))
            {
                pack.WithOffered(!visible.Equals("no", StringComparison.OrdinalIgnoreCase));
            }
            else if (TryValue(text, "PACK_FAMILY", out string family))
            {
                pack.WithFamily(family.ToLowerInvariant());
            }
        }

        return pack;
    }

    private static bool TryValue(string line, string key, out string value)
    {
        value = string.Empty;

        if (!line.StartsWith(key, StringComparison.Ordinal))
        {
            return false;
        }

        int separator = line.IndexOf(":=", StringComparison.Ordinal);

        if (separator < 0)
        {
            return false;
        }

        string remainder = line[(separator + 2)..].Trim();

        if (remainder.StartsWith('?'))
        {
            remainder = remainder[1..].TrimStart();
        }

        value = remainder;

        return true;
    }

    private static void CopyTree(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}
