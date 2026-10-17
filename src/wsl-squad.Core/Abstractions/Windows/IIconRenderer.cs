using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Abstractions.Windows;

/// <summary>Renders the PNG icon used by a Windows Terminal profile.</summary>
public interface IIconRenderer
{
    public OperationResult<string> Render(
        string outputPath,
        string instanceName,
        string? text,
        string? top,
        string? bottom,
        string? textColor);
}
