using System.Diagnostics;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

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
        IReadOnlyList<WslBuildSourceDto> images;
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

            return WorkflowResults.Failed(string.Empty, Localizer.Instance.Format("CliBuildFailed", exception.Message));
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
                            string? answer = View.AskInstanceName(Localizer.Instance["CliAskInstanceName"]);

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
                            stack.Pop();

                            break;
                        }

                        road = picked;

                        IReadOnlyList<BuildSourceChoice> rows = road.Type switch
                        {
                            WslBuildType.Image => [.. images.Select(image => new BuildSourceChoice(
                            image.Name ?? string.Empty,
                            image.Path ?? string.Empty))],
                            WslBuildType.Recipe => [.. sources.Recipes.Select(ToChoice)],
                            WslBuildType.Dockerfile => [.. sources.Dockerfiles.Select(ToChoice)],
                            _ => [],
                        };

                        if (rows.Count == 0)
                        {
                            View.ShowAbort(AbortLines(road.Type));

                            return WorkflowResults.Failed(string.Empty, road.Type switch
                            {
                                WslBuildType.Image => Localizer.Instance["CliBuildNoImage"],
                                WslBuildType.Recipe => Localizer.Instance["CliBuildNoRecipe"],
                                WslBuildType.Dockerfile => Localizer.Instance["CliBuildNoDockerfile"],
                                _ => Localizer.Instance["CliBuildNoSource"],
                            });
                        }

                        stack.Push("source");

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
                            WslBuildType.Dockerfile => [.. sources.Dockerfiles.Select(ToChoice)],
                            _ => [],
                        };
                        BuildSourceChoice? picked = View.AskSource(road.Label, rows);

                        if (picked is null)
                        {
                            stack.Pop();

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
                            stack.Pop();

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
                            ? Localizer.Instance.Format("CliUserNameProposal", name, proposal)
                            : Localizer.Instance.Format("CliUserName", name);
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
                            stack.Pop();
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
                            stack.Pop();
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
                            stack.Pop();
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
            View.ShowCancelled(Localizer.Instance["CliModified"]);

            return WorkflowResults.Cancelled(Localizer.Instance["OperationBuild"]);
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
                _service.Open(dto.Name ?? string.Empty);
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
                Localizer.Instance["CliAbortNoImage"],
                Localizer.Instance["CliAbortUploadOrPick"],
                Localizer.Instance["CliAbortNothingModified"],
            ],
            WslBuildType.Recipe =>
            [
                Localizer.Instance["CliAbortNoRecipe"],
                Localizer.Instance["CliAbortCreateRecipe"],
                Localizer.Instance["CliAbortNothingModified"],
            ],
            WslBuildType.Dockerfile =>
            [
                Localizer.Instance["CliAbortNoDockerfile"],
                Localizer.Instance["CliAbortUploadOrDeleteFolder"],
                Localizer.Instance["CliAbortNothingModified"],
            ],
            _ =>
            [
                Localizer.Instance["CliAbortNoDockerfile"],
                Localizer.Instance["CliAbortUploadOrDeleteFolder"],
                Localizer.Instance["CliAbortNothingModified"],
            ],
        };
    }

}
