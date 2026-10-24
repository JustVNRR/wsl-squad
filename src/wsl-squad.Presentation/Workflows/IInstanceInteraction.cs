using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Workflows;

public interface IInstanceInteraction
{
    string? PickInstance(IReadOnlyList<WslIndexDto> candidates);

    string? PickArchive(IReadOnlyList<WslIndexDto> archives);

    bool PromptRestart(string instanceName);

    ArchiveDto? PromptArchive(string instanceName, bool replacesArchive);

    CompactDto? PromptCompact(string instanceName, bool replacesArchive);

    DuplicateDto? PromptDuplicate(string instanceName, IReadOnlyList<string> takenNames);

    RemoveDto? PromptRemove(string instanceName, bool replacesArchive);

    RestoreDto? PromptRestore(string archiveName, IReadOnlyList<string> takenNames);

    bool PromptDeleteArchive(string archiveName);

    bool? PromptDocker(string instanceName, bool shouldRegister);
}
