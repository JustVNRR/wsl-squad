using System.Diagnostics;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class BuildPresenter : BasePresenter<IBuildView>
{
    private readonly IInstanceService _service;

    public BuildPresenter(IInstanceService service, IBuildView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run()
    {
        GetBuildSourcesResponseDto sources;
        IReadOnlyList<WslDockerImageDto> images;
        IReadOnlyList<WslBuildPackDto> packs;

        try
        {
            sources = _service.GetBuildSources();
            images = _service.GetAllSavedImages();
            packs = _service.GetBuildPacks();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Build : {exception.Message}");
        }

        string name = string.Empty;
        BuildRoad? road = null;
        string buildPath = string.Empty;
        string context = string.Empty;
        string family = string.Empty;
        IReadOnlyList<string> packNames = [];
        string onboarding = string.Empty;
        string user = string.Empty;
        bool keep = false;
        bool register = false;
        bool done = false;

        Stack<string> stack = new();
        stack.Push("name");

        while (stack.Count > 0)
        {
            string step = stack.Peek();

            switch (step)
            {
                case "name":
                {
                    bool ok = false;

                    while (true)
                    {
                        string? answer = View.AskInstanceName("Name of the instance (CTRL+C to abort)");

                        if (string.IsNullOrWhiteSpace(answer))
                        {
                            break;
                        }

                        answer = answer.Trim();

                        if (NameRules.IsValid(answer))
                        {
                            name = answer;
                            ok = true;

                            break;
                        }

                        View.ShowNameHint();
                    }

                    if (ok)
                    {
                        stack.Push("road");
                    }
                    else
                    {
                        stack.Clear();
                    }

                    break;
                }

                case "road":
                {
                    BuildRoad? picked = View.AskRoad();

                    if (picked is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    road = picked;

                    if (road.Type == WslBuildType.Registry)
                    {
                        stack.Push("reference");

                        break;
                    }

                    IReadOnlyList<BuildSourceChoice> rows = road.Type switch
                    {
                        WslBuildType.Image => [.. images.Select(image => new BuildSourceChoice(
                            image.Name ?? string.Empty,
                            image.Path ?? string.Empty,
                            null))],
                        WslBuildType.Recipe => [.. sources.Recipes.Select(ToChoice)],
                        _ => [.. sources.Dockerfiles.Select(ToChoice)],
                    };

                    if (rows.Count == 0)
                    {
                        View.ShowAbort(AbortLines(road.Type));

                        return ActionResult.Nok(road.Type switch
                        {
                            WslBuildType.Image => "Build : no Docker image",
                            WslBuildType.Recipe => "Build : no recipe",
                            _ => "Build : no Dockerfile",
                        });
                    }

                    if (rows.Count == 1 && road.Type != WslBuildType.Recipe)
                    {
                        SelectSource(rows[0], road.Type, ref buildPath, ref context, ref family);
                        stack.Push(packs.Count > 0
                            ? "packs"
                            : NextAfterPacks(sources.Onboardings.Count, road));
                    }
                    else
                    {
                        stack.Push("source");
                    }

                    break;
                }

                case "reference":
                {
                    bool ok = false;

                    while (true)
                    {
                        string? reference = View.AskRegistryReference(
                            "Docker image from a registry (e.g. alpine:latest)");

                        if (string.IsNullOrWhiteSpace(reference))
                        {
                            break;
                        }

                        IsRegistryImageValidResponseDto verdict = _service.IsRegistryImageValid(reference);

                        if (verdict.Valid)
                        {
                            buildPath = reference;
                            ok = true;

                            break;
                        }

                        View.ShowUnknownRegistry(reference, verdict.Why ?? string.Empty);
                    }

                    if (!ok)
                    {
                        _ = stack.Pop();
                    }
                    else
                    {
                        stack.Push(packs.Count > 0
                            ? "packs"
                            : NextAfterPacks(sources.Onboardings.Count, road!));
                    }

                    break;
                }

                case "source":
                {
                    IReadOnlyList<BuildSourceChoice> rows = road!.Type switch
                    {
                        WslBuildType.Image => [.. images.Select(image => new BuildSourceChoice(
                            image.Name ?? string.Empty,
                            image.Path ?? string.Empty,
                            null))],
                        WslBuildType.Recipe => [.. sources.Recipes.Select(ToChoice)],
                        _ => [.. sources.Dockerfiles.Select(ToChoice)],
                    };
                    BuildSourceChoice? picked = View.AskSource(road.Label, rows);

                    if (picked is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    SelectSource(picked, road.Type, ref buildPath, ref context, ref family);
                    stack.Push(packs.Count > 0
                        ? "packs"
                        : NextAfterPacks(sources.Onboardings.Count, road));

                    break;
                }

                case "packs":
                {
                    IReadOnlyList<WslBuildPackDto> offered = family.Length > 0
                        ? [.. packs.Where(pack => pack.Family == family)]
                        : packs;
                    string note = family.Length > 0
                        ? string.Empty
                        : "This recipe's system cannot be read - a pack made for another one will fail to install.";
                    List<int> checkedIndexes = [];

                    for (int index = 0; index < offered.Count; index++)
                    {
                        if (family.Length > 0 &&
                            offered[index].Shell &&
                            offered[index].Family == family)
                        {
                            checkedIndexes.Add(index);
                        }
                    }

                    IReadOnlyList<WslBuildPackDto>? chosen = View.AskPacks(name, offered, checkedIndexes, note);

                    if (chosen is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    packNames = [.. chosen.Select(pack => pack.Name ?? string.Empty)];

                    if (packNames.Count == 0)
                    {
                        View.ShowNoPackChosen();
                    }

                    stack.Push(NextAfterPacks(sources.Onboardings.Count, road!));

                    break;
                }

                case "onboarding":
                {
                    bool? run = View.AskRunOnboarding(road!.Type == WslBuildType.Recipe);

                    if (run is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    if (run == false)
                    {
                        onboarding = string.Empty;
                        stack.Push(NextAfterOnboarding(road));

                        break;
                    }

                    BuildSourceChoice? picked = View.AskOnboarding([.. sources.Onboardings.Select(ToChoice)]);

                    if (picked is not null)
                    {
                        onboarding = picked.Path;
                        stack.Push("user");
                    }

                    break;
                }

                case "user":
                {
                    string proposal = NameRules.ProposeUserName();
                    string prompt = proposal.Length > 0
                        ? $"User name for '{name}' [{proposal}] (CTRL+C to abort)"
                        : $"User name for '{name}' (CTRL+C to abort)";
                    View.ShowBeforeAccount();
                    bool ok = false;

                    while (true)
                    {
                        string? answer = View.AskUserName(prompt);

                        if (answer is null)
                        {
                            break;
                        }

                        if (answer.Length == 0 && proposal.Length > 0)
                        {
                            answer = proposal;
                        }

                        if (NameRules.IsValidUser(answer))
                        {
                            user = answer;
                            ok = true;

                            break;
                        }

                        View.ShowUserHint();
                    }

                    if (!ok)
                    {
                        _ = stack.Pop();
                    }
                    else
                    {
                        stack.Push(NextAfterOnboarding(road!));
                    }

                    break;
                }

                case "keep":
                {
                    bool? answer = View.AskKeepImage();

                    if (answer is null)
                    {
                        _ = stack.Pop();
                    }
                    else
                    {
                        keep = answer.Value;
                        stack.Push("register");
                    }

                    break;
                }

                case "register":
                {
                    bool? answer = View.AskRegisterDocker(name);

                    if (answer is null)
                    {
                        _ = stack.Pop();
                    }
                    else
                    {
                        register = answer.Value;
                        done = true;
                        stack.Clear();
                    }

                    break;
                }

                default:
                    stack.Clear();
                    break;
            }
        }

        if (!done || road is null)
        {
            View.ShowCancelled("modified");

            return ActionResult.Ok("Build : cancelled");
        }

        BuildDto dto = new()
        {
            Name = name,
            User = user,
            BuildType = road.Type,
            BuildPath = buildPath,
            Context = context,
            Onboarding = onboarding,
            Packs = [.. packNames],
            KeepImage = keep,
            RegisterDocker = register,
            Interactive = true,
        };

        if (!_service.StartDocker())
        {
            View.ShowAbort(["Docker is not responding.", "Start Docker Desktop, then try again."]);

            return ActionResult.Nok("Build : Docker is not responding.");
        }

        try
        {
            BuildResponseDto response = _service.Build(dto);
            View.Show(response);

            try
            {
                _ = _service.Open(dto.Name ?? string.Empty);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning($"[BuildPresenter] Could not open shell for '{dto.Name}': {ex.Message}");
            }

            return ActionResult.Ok($"Build : {dto.Name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Build : {dto.Name} - {exception.Message}");
        }
    }

    private string NextAfterPacks(int onboardingCount, BuildRoad road)
    {
        if (onboardingCount == 0)
        {
            View.ShowNoOnboarding();

            return NextAfterOnboarding(road);
        }

        return "onboarding";
    }

    private static string NextAfterOnboarding(BuildRoad road)
    {
        return road.Type is WslBuildType.Dockerfile or WslBuildType.Recipe ? "keep" : "register";
    }

    private static void SelectSource(
        BuildSourceChoice choice,
        WslBuildType type,
        ref string buildPath,
        ref string context,
        ref string family)
    {
        if (type == WslBuildType.Recipe)
        {
            context = choice.Path;
            buildPath = Path.Combine(context, "Dockerfile");
            family = choice.Family ?? string.Empty;
        }
        else
        {
            buildPath = choice.Path;

            if (type == WslBuildType.Dockerfile)
            {
                context = Path.GetDirectoryName(buildPath) ?? string.Empty;
                family = choice.Family ?? string.Empty;
            }
        }
    }

    private static BuildSourceChoice ToChoice(WslBuildSourceDto source)
    {
        return new BuildSourceChoice(
            source.Name ?? string.Empty,
            source.Path ?? string.Empty,
            source.Family);
    }

    private static IReadOnlyList<string> AbortLines(WslBuildType type)
    {
        return type switch
        {
            WslBuildType.Image =>
            [
                "No Docker image under 'assets\\dockerimages'.",
                "Upload one, or pick another road.",
                "Nothing was modified.",
            ],
            WslBuildType.Recipe =>
            [
                "No recipe under 'assets\\recipes'.",
                "Create one with the window's + button.",
                "Nothing was modified.",
            ],
            _ =>
            [
                "No Dockerfile under 'assets\\dockerfiles'.",
                "Upload one, or delete that folder to get the repository's own back.",
                "Nothing was modified.",
            ],
        };
    }

}
