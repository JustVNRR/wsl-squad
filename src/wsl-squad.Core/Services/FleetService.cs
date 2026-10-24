using wsl_squad.Core.Dtos;
using wsl_squad.Core.Repositories;

namespace wsl_squad.Core.Services;

public sealed class FleetService : IFleetService
{
    private readonly IConfigRepository _configRepository;
    private readonly IWslRepository _wslRepository;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IArchiveRepository _archiveRepository;
    private readonly IInstanceService _instanceService;

    public FleetService(
        IConfigRepository configRepository,
        IWslRepository wslRepository,
        IInstanceRepository instanceRepository,
        IArchiveRepository archiveRepository,
        IInstanceService instanceService)
    {
        _configRepository = configRepository;
        _wslRepository = wslRepository;
        _instanceRepository = instanceRepository;
        _archiveRepository = archiveRepository;
        _instanceService = instanceService;
    }

    public string GetRoot()
    {
        string saved = _configRepository.GetSavedRoot();

        if (saved.Length > 0 && Directory.Exists(saved))
        {
            return saved;
        }

        List<string> homes = RootHomes();

        if (saved.Length == 0)
        {
            string decided = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "WSL");

            if (homes.Count == 1)
            {
                decided = homes[0];
            }
            else if (Directory.Exists("D:\\"))
            {
                decided = "D:\\WSL";
            }

            _configRepository.SaveRoot(decided);

            return decided;
        }

        if (homes.Count == 1)
        {
            _configRepository.SaveRoot(homes[0]);

            return homes[0];
        }

        throw new InvalidOperationException(
            $"The working folder '{saved}' is gone, and no existing folder holds your instances.");
    }

    public string SetRoot(string target)
    {
        FleetTargetProblem problem = ReferenceHelpers.TestFleetTarget(target);

        if (problem != FleetTargetProblem.None)
        {
            throw new FleetTargetException(problem, target);
        }

        _instanceRepository.EnsureFolder(target);
        _archiveRepository.MoveTo(target);
        _configRepository.SaveRoot(target);

        return target;
    }

    public IReadOnlyList<string> GetFleet()
    {
        string root = GetRoot();
        List<string> names = [];

        foreach (Entities.WslInstance distro in _wslRepository.GetAllDistros())
        {
            string folder = _wslRepository.GetFolder(distro.Name);

            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                continue;
            }

            if (!_instanceRepository.HasMarkerIn(folder))
            {
                continue;
            }

            if ((Path.GetDirectoryName(folder) ?? string.Empty).TrimEnd('\\') != root.TrimEnd('\\'))
            {
                continue;
            }

            names.Add(distro.Name);
        }

        return [.. names.Order(StringComparer.Ordinal)];
    }

    public WslConfigResponseDto WslConfig()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig");
        bool created = !File.Exists(path);
        path = _configRepository.EnsureWslConfig();

        _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
        {
            UseShellExecute = true,
        });

        return new WslConfigResponseDto
        {
            Path = path,
            Created = created,
        };
    }

    public MigrateResponseDto Migrate(string target)
    {
        FleetTargetProblem problem = ReferenceHelpers.TestFleetTarget(target);

        if (problem != FleetTargetProblem.None)
        {
            throw new FleetTargetException(problem, target);
        }

        List<string> moved = [];

        foreach (string name in GetFleet())
        {
            _instanceService.Remove(new RemoveDto
            {
                InstanceName = name,
                ArchiveFirst = true,
            });
            moved.Add(name);
        }

        return new MigrateResponseDto
        {
            Moved = moved,
            Target = SetRoot(target),
        };
    }

    private List<string> RootHomes()
    {
        List<string> homes = [];

        foreach (Entities.WslInstance distro in _wslRepository.GetAllDistros())
        {
            string folder = _wslRepository.GetFolder(distro.Name);

            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                continue;
            }

            if (!_instanceRepository.HasMarkerIn(folder))
            {
                continue;
            }

            string parent = Path.GetDirectoryName(folder) ?? string.Empty;

            if (!homes.Contains(parent))
            {
                homes.Add(parent);
            }
        }

        return homes;
    }
}
