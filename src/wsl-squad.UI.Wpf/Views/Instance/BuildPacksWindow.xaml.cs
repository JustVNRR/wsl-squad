using System.Windows;
using System.Windows.Controls;

using wsl_squad.Core.Dtos;

namespace wsl_squad.UI.Wpf.Views.Instance;

public partial class BuildPacksWindow : ChromeWindow
{
    private readonly List<(WslBuildPackDto Pack, CheckBox Check)> _entries = [];

    public BuildPacksWindow(string instanceName, string family, IReadOnlyList<WslBuildPackDto> packs)
    {
        InitializeComponent();
        Dress();

        TxtPacksTitle.Text = $"ADD PACKS FOR '{instanceName}'";

        if (string.IsNullOrEmpty(family))
        {
            TxtPacksForeign.Visibility = Visibility.Visible;
        }

        IReadOnlyList<WslBuildPackDto> offered = string.IsNullOrEmpty(family)
            ? packs
            : [.. packs.Where(pack => pack.Family == family)];

        foreach (WslBuildPackDto pack in offered)
        {
            TextBlock label = new()
            {
                TextWrapping = TextWrapping.Wrap,
                Text = $"{pack.Name,-12} [{pack.Family}] {pack.Description}",
            };
            CheckBox check = new()
            {
                Content = label,
                Margin = new Thickness(0, 3, 0, 3),
                IsChecked = !string.IsNullOrEmpty(family) &&
                            pack.Name == "oh_my_shell" &&
                            pack.Family == family,
            };

            check.Checked += (_, _) => RefreshNotes();
            check.Unchecked += (_, _) => RefreshNotes();
            _entries.Add((pack, check));
            _ = Boxes.Children.Add(check);
        }

        BtnPickCancel.Click += (_, _) => Close();
        BtnPickApply.Click += (_, _) => Apply();
        RefreshNotes();
    }

    public IReadOnlyList<string>? Result { get; private set; }

    private void RefreshNotes()
    {
        TxtNotes.Text = _entries.Exists(entry => entry.Check.IsChecked == true)
            ? string.Empty
            : "No pack selected - it will start bare.";
    }

    private void Apply()
    {
        Result = [.. _entries.Where(entry => entry.Check.IsChecked == true).Select(entry => entry.Pack.Name ?? string.Empty)];
        DialogResult = true;
    }
}
