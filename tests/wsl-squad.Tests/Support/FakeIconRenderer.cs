using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Tests.Support;

public sealed class FakeIconRenderer : IIconRenderer
{
    private readonly List<string> _rendered = [];

    public bool Fail { get; set; }

    public IReadOnlyList<string> Rendered => _rendered;

    public OperationResult<string> Render(
        string outputPath,
        string instanceName,
        string? text,
        string? top,
        string? bottom,
        string? textColor)
    {
        if (Fail)
        {
            return OperationResult<string>.Fail(ErrorCode.IoError, "icon rendering failed");
        }

        _rendered.Add(outputPath);

        return OperationResult<string>.Ok(outputPath);
    }
}
