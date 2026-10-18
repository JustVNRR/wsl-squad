using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.ViewModels;

public sealed class PackRowViewModel : BaseViewModel
{
    private readonly ManagePacksViewModel _owner;
    private readonly string _name;
    private bool _checked;

    public PackRowViewModel(ManagePacksViewModel owner, string name, string label, bool initial)
    {
        _owner = owner;
        _name = name;
        Label = label;
        _checked = initial;
    }

    public string Label { get; }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (SetProperty(ref _checked, value))
            {
                _owner.Recount();
            }
        }
    }

    public string PackName()
    {
        return _name;
    }
}

public sealed partial class ManagePacksViewModel : BaseViewModel
{
    private readonly GetPacksResponseDto _packs;
    private readonly string _name;

    [ObservableProperty]
    private string _lead;

    [ObservableProperty]
    private string _willAdd = string.Empty;

    [ObservableProperty]
    private string _willRemove = string.Empty;

    [ObservableProperty]
    private bool _canApply;

    private readonly bool _empty;

    public ManagePacksViewModel(GetPacksResponseDto packs, string name)
    {
        _packs = packs;
        _name = name;
        _lead = $"Packs for '{name}'".ToUpperInvariant();

        List<(string Name, string Label, bool Checked)> rows = [];
        Dictionary<string, string> descriptions = new(StringComparer.Ordinal);

        foreach (WslPackDto pack in packs.Candidate)
        {
            string packName = pack.Name ?? string.Empty;
            descriptions[packName] = pack.Description ?? string.Empty;
            rows.Add((packName, LabelOf(packName, pack.Description), false));
        }

        foreach (WslPackDto pack in packs.InstalledPacks)
        {
            descriptions[pack.Name ?? string.Empty] = pack.Description ?? string.Empty;
        }

        foreach (string removable in packs.Removable)
        {
            if (rows.Exists(row => row.Name == removable))
            {
                continue;
            }

            _ = descriptions.TryGetValue(removable, out string? description);
            rows.Add((removable, LabelOf(removable, description), true));
        }

        if (rows.Count == 0)
        {
            _empty = true;

            return;
        }

        foreach ((string rowName, string label, bool isChecked) in rows)
        {
            PackRows.Add(new PackRowViewModel(this, rowName, label, isChecked));
        }

        ApplyCommand = new RelayCommand(Apply, () => CanApply);
        Recount();
    }

    public ObservableCollection<PackRowViewModel> PackRows { get; } = [];

    public ManagePacksDto? Result { get; private set; }

    public IRelayCommand ApplyCommand { get; private set; } = null!;

    public bool Empty()
    {
        return _empty;
    }

    public void Recount()
    {
        HashSet<string> installed = new(_packs.Installed, StringComparer.Ordinal);
        HashSet<string> ticked = new(
            PackRows.Where(row => row.Checked).Select(row => row.PackName()),
            StringComparer.Ordinal);
        List<string> toAdd = [.. ticked.Where(name => !installed.Contains(name))];
        List<string> toRemove = [.. _packs.Removable.Where(name => !ticked.Contains(name))];

        WillAdd = toAdd.Count > 0 ? $"Will install : {string.Join(", ", toAdd)}" : string.Empty;
        WillRemove = toRemove.Count > 0 ? $"Will remove  : {string.Join(", ", toRemove)}" : string.Empty;
        CanApply = toAdd.Count > 0 || toRemove.Count > 0;
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private static string LabelOf(string name, string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? name
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0,-12} {1}",
                name,
                description);
    }

    private void Apply()
    {
        if (!CanApply)
        {
            return;
        }

        HashSet<string> installed = new(_packs.Installed, StringComparer.Ordinal);
        HashSet<string> ticked = new(
            PackRows.Where(row => row.Checked).Select(row => row.PackName()),
            StringComparer.Ordinal);
        List<string> toAdd = [.. ticked.Where(name => !installed.Contains(name))];
        List<string> toRemove = [.. _packs.Removable.Where(name => !ticked.Contains(name))];

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return;
        }

        Result = new ManagePacksDto
        {
            InstanceName = _name,
            ToAdd = toAdd,
            ToRemove = toRemove,
        };
        RequestClose();
    }
}
