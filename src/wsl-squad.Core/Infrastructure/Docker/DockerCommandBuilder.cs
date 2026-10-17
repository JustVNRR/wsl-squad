using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Core.Infrastructure.Docker;

/// <summary>Builds exact command lines for <c>docker</c>.</summary>
public static class DockerCommandBuilder
{
    public static ProcessInvocation Info()
    {
        return new ProcessInvocation("docker", ["info"]);
    }

    public static ProcessInvocation Build(string imageTag, string dockerfilePath, string contextPath)
    {
        return new ProcessInvocation(
            "docker",
            ["build", "-t", imageTag, "-f", dockerfilePath, contextPath]);
    }

    public static ProcessInvocation Load(string tarPath)
    {
        return new ProcessInvocation("docker", ["load", "-i", tarPath]);
    }

    public static ProcessInvocation CreateContainer(string containerName, string imageReference)
    {
        return new ProcessInvocation("docker", ["create", "--name", containerName, imageReference]);
    }

    public static ProcessInvocation ExportContainer(string outputPath, string containerName)
    {
        return new ProcessInvocation("docker", ["export", "-o", outputPath, containerName]);
    }

    public static ProcessInvocation RemoveContainer(string containerName)
    {
        return new ProcessInvocation("docker", ["rm", "-f", containerName]);
    }

    public static ProcessInvocation Save(string outputPath, string imageTag)
    {
        return new ProcessInvocation("docker", ["save", "-o", outputPath, imageTag]);
    }

    public static ProcessInvocation RemoveImage(string imageTag)
    {
        return new ProcessInvocation("docker", ["rmi", "-f", imageTag]);
    }
}
