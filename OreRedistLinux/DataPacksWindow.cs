using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Choose which ore data packs to use and apply them to the ore list (rock types and weights from the real abundance).</summary>
    public class DataPacksWindow : DialogWindow
    {
        readonly RedistributionSetupViewModel _vm;
        readonly StackPanel _list = new() { Spacing = 6 };
        readonly TextBlock _skipped = new() { Foreground = Brushes.Gray, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        readonly TextBlock _result = new() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold };
        readonly CheckBox _rocks = new() { Content = "Set rock types (used by the geology placement mode)" };
        readonly CheckBox _regions = new() { Content = "Set the regions of each ore: Glob % strength and size (ores that occur in a few regions)" };
        readonly CheckBox _rarity = new() { Content = "Set the Weight of each ore from its real abundance (relative to the other ores in the list)" };
        readonly List<(CheckBox box, OrePack pack)> _boxes = new();

        internal DataPacksWindow(RedistributionSetupViewModel vm)
        {
            _vm = vm;
            Title = "Ore data packs";
            Width = 640; SizeToContent = SizeToContent.Height; CanResize = false;

            var root = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            root.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray, FontSize = 12,
                Text = "A data pack lists ores with their abundance in the Earth's crust and the rock types they form in. Tick the packs to use; when two packs " +
                       "have the same ore the later one (lower in the list) wins. Ores that no selected pack knows are left as they are."
            });
            root.Children.Add(_list);
            root.Children.Add(_skipped);

            root.Children.Add(_rocks);
            root.Children.Add(_regions);
            root.Children.Add(_rarity);
            var strength = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(24, 0, 0, 0) };
            strength.Children.Add(new TextBlock { Text = "Strength", VerticalAlignment = VerticalAlignment.Center });
            var tb = new TextBox { Width = 60, Text = _vm.RarityStrength.ToString("0.##", CultureInfo.InvariantCulture) };
            tb.TextChanged += (_, _) => { if (double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) _vm.RarityStrength = v; };
            strength.Children.Add(tb);
            strength.Children.Add(new TextBlock
            {
                Text = "0 = all ores equal, 1 = the real ratios (iron about 14 million times as common as gold), 0.25 = compressed so rare ores still appear.",
                FontSize = 12, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap, MaxWidth = 430, VerticalAlignment = VerticalAlignment.Center
            });
            root.Children.Add(strength);

            root.Children.Add(new TextBlock { Text = "Pack files (edit them, or add your own):", FontSize = 12, Foreground = Brushes.Gray });
            root.Children.Add(new TextBox { Text = OrePacks.Folder, IsReadOnly = true, FontSize = 12 });
            root.Children.Add(_result);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            var reload = new Button { Content = "Reload packs" };
            reload.Click += (_, _) => Rebuild();
            var apply = new Button { Content = "Apply", MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            apply.Click += (_, _) => Apply();
            var close = new Button { Content = "Close", MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            close.Click += (_, _) => { SaveChoice(); CloseWithResult(true); };
            Closing += (_, _) => SaveChoice();
            buttons.Children.Add(reload); buttons.Children.Add(apply); buttons.Children.Add(close);
            root.Children.Add(buttons);
            Content = root;

            _rocks.IsChecked = AppSettings.PackRocks;
            _rarity.IsChecked = AppSettings.PackRarity;
            _regions.IsChecked = AppSettings.PackRegions;
            Rebuild(AppSettings.SelectedDataPacks?.ToList() ?? new List<string> { "vanilla.xml" });
        }

        void Rebuild(List<string> selected = null)
        {
            selected ??= _boxes.Where(b => b.box.IsChecked == true).Select(b => b.pack.FileName).ToList();
            var packs = OrePacks.LoadAll(out var skipped);
            _list.Children.Clear(); _boxes.Clear();
            foreach (var p in packs)
            {
                var box = new CheckBox { IsChecked = selected.Contains(p.FileName, StringComparer.OrdinalIgnoreCase) };
                var text = new StackPanel();
                text.Children.Add(new TextBlock { Text = $"{p.Name}   ({p.Ores.Count} ores, {p.FileName})", FontWeight = FontWeight.SemiBold });
                if (p.Description.Length > 0) text.Children.Add(new TextBlock { Text = p.Description, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brushes.Gray, MaxWidth = 560 });
                if (p.Author.Length > 0) text.Children.Add(new TextBlock { Text = "by " + p.Author, FontSize = 11, Foreground = Brushes.Gray });
                box.Content = text;
                _list.Children.Add(box);
                _boxes.Add((box, p));
            }
            _skipped.Text = skipped.Count == 0 ? "" : "Not loaded (no valid pack header, too large or malformed): " + string.Join(", ", skipped);
            _skipped.IsVisible = skipped.Count > 0;
        }

        void SaveChoice() =>
            AppSettings.SetDataPackChoice(_boxes.Where(b => b.box.IsChecked == true).Select(b => b.pack.FileName), _rocks.IsChecked == true, _rarity.IsChecked == true, _regions.IsChecked == true);

        void Apply()
        {
            var packs = _boxes.Where(b => b.box.IsChecked == true).Select(b => b.pack).ToList();
            if (packs.Count == 0) { _result.Text = "Tick at least one pack."; return; }
            if (_rocks.IsChecked != true && _rarity.IsChecked != true && _regions.IsChecked != true) { _result.Text = "Tick what to set: rock types, regions and / or weights."; return; }
            SaveChoice();
            var r = _vm.ApplyDataPacks(packs, _rocks.IsChecked == true, _rarity.IsChecked == true, _regions.IsChecked == true);
            var lines = new List<string>();
            if (_rocks.IsChecked == true) lines.Add($"Rock types set for {r.RockOres} ore(s)." + (r.NoRocks.Count > 0 ? " No rock data for: " + string.Join(", ", r.NoRocks) + "." : ""));
            if (_regions.IsChecked == true) lines.Add($"Regions set for {r.RegionOres} ore(s)." + (r.NoRegion.Count > 0 ? " No region data for: " + string.Join(", ", r.NoRegion) + "." : ""));
            if (_rarity.IsChecked == true) lines.Add($"Weights set for {r.RarityOres} ore(s), strength {_vm.RarityStrength:0.##}." + (r.NoRarity.Count > 0 ? " No abundance for (scaled along): " + string.Join(", ", r.NoRarity) + "." : ""));
            _result.Text = string.Join("\n", lines);
        }
    }
}
