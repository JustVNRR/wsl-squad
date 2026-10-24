using System.Diagnostics;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;
using wsl_squad.Presentation.Localization;

namespace wsl_squad.Presentation.Cli.Presenters.Instance;

public sealed class BuildPresenter : BasePresenter<IBuildView>
{
    private readonly IInstanceService _service;

    public BuildPresenter(IInstanceService service, IBuildView view)
        : base(view)
    {
        _service = service;
    }

    public IWorkflowResult Run()
    {
        GetBuildSourcesResponseDto sources;
        IReadOnlyList<WslDockerImageDto> images;
        IReadOnlyList<string> takenNames;

        try
        {
            BuildContextDto buildContext = _service.GetBuildContext();
            sources = buildContext.Sources;
            images = buildContext.Images;
            takenNames = buildContext.TakenNames;
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed(string.Empty, $"Build : {exception.Message}");
        }

        string name = string.Empty;
        BuildRoad? road = null;
        string source = string.Empty;
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

                        NameProblem problem = NameRules.ValidateInstanceName(answer, takenNames);

                        if (problem != NameProblem.None)
                        {
                            View.ShowNameProblem(problem.ToMessage(answer)!);

                            continue;
                        }

                        name = answer;
                        ok = true;

                        break;
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
                            image.Path ?? string.Empty))],
                        WslBuildType.Recipe => [.. sources.Recipes.Select(ToChoice)],
                        _ => [.. sources.Dockerfiles.Select(ToChoice)],
                    };

                    if (rows.Count == 0)
                    {
                        View.ShowAbort(AbortLines(road.Type));

                        return WorkflowResults.Failed(string.Empty, road.Type switch
                        {
                            WslBuildType.Image => "Build : no Docker image",
                            WslBuildType.Recipe => "Build : no recipe",
                            _ => "Build : no Dockerfile",
                        });
                    }

                    stack.Push("source");

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
                            source = reference;
                            ok = true;

                            break;
                        }

                        View.ShowUnknownRegistry(reference, verdict.Error.ToMessage(verdict.TechnicalDetail));
                    }

                    if (!ok)
                    {
                        _ = stack.Pop();
                    }
                    else
                    {
                        stack.Push(NextAfterSource(sources.Onboardings.Count, road!));
                    }

                    break;
                }

                case "source":
                {
                    IReadOnlyList<BuildSourceChoice> rows = road!.Type switch
                    {
                        WslBuildType.Image => [.. images.Select(image => new BuildSourceChoice(
                            image.Name ?? string.Empty,
                            image.Path ?? string.Empty))],
                        WslBuildType.Recipe => [.. sources.Recipes.Select(ToChoice)],
                        _ => [.. sources.Dockerfiles.Select(ToChoice)],
                    };
                    BuildSourceChoice? picked = View.AskSource(road.Label, rows);

                    if (picked is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    source = picked.Path;
                    stack.Push(NextAfterSource(sources.Onboardings.Count, road));

                    break;
                }

                case "onboarding":
                {
                    bool? run = View.AskRunOnboarding();

                    if (run is null)
                    {
                        _ = stack.Pop();

                        break;
                    }

                    if (run == false)
                    {
                        onboarding = string.Empty;
                        stack.Push(NextAfterOnboarding(road!));

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

                case "done":
                {
                    done = true;
                    stack.Clear();

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

            return WorkflowResults.Cancelled("Build");
        }

        BuildDto dto = BuildDto.FromSource(
            name,
            user,
            road.Type,
            source,
            onboarding,
            keep,
            register,
            interactive: true);

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

            return WorkflowResults.Success(dto.Name ?? string.Empty);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed(dto.Name ?? string.Empty, exception.ToMessage());
        }
    }

    private string NextAfterSource(int onboardingCount, BuildRoad road)
    {
        if (road.Type == WslBuildType.Recipe)
        {
            return "done";
        }

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

    private static BuildSourceChoice ToChoice(WslBuildSourceDto source)
    {
        return new BuildSourceChoice(
            source.Name ?? string.Empty,
            source.Path ?? string.Empty);
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
