using System.Formats.Tar;
using System.Text.Json;

using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Infrastructure.Docker;

/// <summary>Reads the image reference from a <c>docker save</c> archive without parsing CLI output.</summary>
public static class DockerImageArchiveReader
{
    public static OperationResult<string> ReadImageReference(Stream archiveStream)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);

        try
        {
            using TarReader reader = new(archiveStream, leaveOpen: true);
            TarEntry? entry;

            while ((entry = reader.GetNextEntry()) is not null)
            {
                if (!string.Equals(entry.Name, "manifest.json", StringComparison.Ordinal))
                {
                    continue;
                }

                using Stream? manifest = entry.DataStream;
                if (manifest is null)
                {
                    return OperationResult<string>.Fail(
                        ErrorCode.CorruptState,
                        "The Docker image archive manifest is empty.");
                }

                using JsonDocument document = JsonDocument.Parse(manifest);

                foreach (JsonElement image in document.RootElement.EnumerateArray())
                {
                    if (!image.TryGetProperty("RepoTags", out JsonElement tags) ||
                        tags.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement tag in tags.EnumerateArray())
                    {
                        string? reference = tag.GetString();
                        if (!string.IsNullOrWhiteSpace(reference))
                        {
                            return OperationResult<string>.Ok(reference);
                        }
                    }
                }

                return OperationResult<string>.Fail(
                    ErrorCode.CorruptState,
                    "The Docker image archive contains no repository tags.");
            }

            return OperationResult<string>.Fail(
                ErrorCode.CorruptState,
                "The Docker image archive does not contain manifest.json.");
        }
        catch (JsonException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.CorruptState,
                "The Docker image archive manifest is not valid JSON.",
                exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.CorruptState,
                "The Docker image archive is not a valid tar file.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                "The Docker image archive could not be read.",
                exception.Message);
        }
    }
}
