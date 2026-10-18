namespace wsl_squad.Core.Entities;

/// <summary>A tool pack from the catalogue (reference <c>WslPack</c>).</summary>
public sealed class WslPack
{
    private string _name = string.Empty;
    private string? _family = "debian";
    private IReadOnlyList<string> _requires = [];

    public string Name
    {
        get => _name;
        set
        {
            if (!IsValidPackName(value, allowTemplate: true))
            {
                throw new ArgumentException(
                    $"'{value}' is not usable as a pack name (a letter or digit first, then letters, digits, '.', '_' and '-'; a leading '_' for the template).",
                    nameof(value));
            }

            _name = value;
        }
    }

    public string? Description { get; set; }

    public IReadOnlyList<string> Requires
    {
        get => _requires;
        set
        {
            if (value is null)
            {
                _requires = [];

                return;
            }

            foreach (string required in value)
            {
                if (!IsValidPackName(required, allowTemplate: false))
                {
                    throw new ArgumentException(
                        $"'{required}' is not usable as a pack name (letters, digits, '.', '_' and '-' only).",
                        nameof(value));
                }
            }

            _requires = [.. value];
        }
    }

    public bool Offered { get; set; } = true;

    public string? Family
    {
        get => _family;
        set
        {
            if (!string.IsNullOrEmpty(value) && !IsValidFamily(value))
            {
                throw new ArgumentException(
                    $"'{value}' is not usable as a family (lowercase letters, digits and '-' only).",
                    nameof(value));
            }

            _family = value;
        }
    }

    public string? Welcome { get; set; }

    public WslPack WithName(string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            Name = name;
        }

        return this;
    }

    public WslPack WithDescription(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Description = text;
        }

        return this;
    }

    public WslPack WithRequires(IReadOnlyList<string>? packs)
    {
        Requires = packs ?? [];

        return this;
    }

    public WslPack WithOffered(bool offered)
    {
        Offered = offered;

        return this;
    }

    public WslPack WithFamily(string? family)
    {
        if (!string.IsNullOrEmpty(family))
        {
            Family = family;
        }

        return this;
    }

    public WslPack WithWelcome(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Welcome = text;
        }

        return this;
    }

    public override string ToString()
    {
        return $"pack '{Name}' ({Family}) - {Description}";
    }

    private static bool IsValidPackName(string value, bool allowTemplate)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        int start = allowTemplate && value[0] == '_' ? 1 : 0;

        if (start >= value.Length || !char.IsAsciiLetterOrDigit(value[start]))
        {
            return false;
        }

        return value[start..].All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
    }

    private static bool IsValidFamily(string value)
    {
        return char.IsAsciiLetterLower(value[0]) && value.All(character =>
            char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
    }
}
