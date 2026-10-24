using WslSquad.Core.Validation;

namespace WslSquad.Core.Entities;

public sealed class WslInstance
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set
        {
            if (!NameRules.IsValid(value))
            {
                throw new ArgumentException(
                    $"'{value}' is not usable as an instance name (letters, digits, '.', '_' and '-' only).",
                    nameof(value));
            }

            _name = value;
        }
    }

    public string DefaultUser { get; set; } = "root";

    public string Family { get; set; } = string.Empty;

    public bool IsArchive { get; set; }

    public WslTheme? Theme { get; set; }

    public IReadOnlyList<WslPack> Packs { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public long Size { get; set; }

    public override string ToString()
    {
        string theme = Theme is null ? "-" : Theme.ToString();
        string packs = Packs.Count == 0 ? "none" : string.Join(", ", Packs.Select(pack => pack.Name));

        return string.Join(
            Environment.NewLine,
            $"  * Distribution Name : {Name}",
            $"  * Default User      : {DefaultUser}",
            $"  * Look              : {theme}",
            $"  * Packs             : {packs}");
    }

}
