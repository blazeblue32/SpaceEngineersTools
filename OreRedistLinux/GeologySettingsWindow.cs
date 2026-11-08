using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Geology settings of the planet: share of uplift, patch size and the rock recipes of the four regions.</summary>
    public class GeologySettingsWindow : DialogWindow
    {
        readonly GeologySettings _g;

        public GeologySettingsWindow(GeologySettings settings)
        {
            _g = settings;
            Title = "Geology settings";
            Width = 760; SizeToContent = SizeToContent.Height; CanResize = false;

            var root = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            root.Children.Add(Note("Every region has a recipe: for each rock layer (surface = ore layers 1-3, layer 1 = 4-6, layer 2 = 7-10) the weights of the five rock types. " +
                "The rock at a place is drawn from the recipe of its region and forms patches. Weights only count relative to each other (0 = never)."));

            var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            top.Children.Add(Label("Uplift: highest"));
            top.Children.Add(Num(() => _g.UpliftPercent, v => _g.UpliftPercent = v, 60));
            top.Children.Add(Label("% of the land   (mountains: metamorphic and intrusive rock reach the surface)"));
            root.Children.Add(top);
            var patch = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            patch.Children.Add(Label("Rock patch size"));
            patch.Children.Add(Num(() => _g.PatchKm, v => _g.PatchKm = v, 60));
            patch.Children.Add(Label("km   (typical width of a patch of one rock type)"));
            root.Children.Add(patch);

            var grid = new Grid { ColumnSpacing = 6, RowSpacing = 3, Margin = new Thickness(0, 6, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            for (int k = 0; k < GeoInfo.Rocks; ++k) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            int row = 0;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int k = 0; k < GeoInfo.Rocks; ++k)
            {
                var h = new TextBlock { Text = GeoInfo.RockNames[k], FontSize = 11, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(h, 2 + k); Grid.SetRow(h, 0); grid.Children.Add(h);
            }
            for (int r = 0; r < GeoInfo.Regions; ++r)
                for (int t = 0; t < GeoInfo.Tiers; ++t)
                {
                    ++row; grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    if (t == 0)
                    {
                        var n = new TextBlock { Text = GeoInfo.RegionNames[r], FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(n, row); Grid.SetColumn(n, 0); grid.Children.Add(n);
                    }
                    var tl = new TextBlock { Text = GeoInfo.TierNames[t], VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray, FontSize = 12 };
                    Grid.SetRow(tl, row); Grid.SetColumn(tl, 1); grid.Children.Add(tl);
                    for (int k = 0; k < GeoInfo.Rocks; ++k)
                    {
                        var cell = _g.Cells[(r * GeoInfo.Tiers + t) * GeoInfo.Rocks + k];
                        var tb = Num(() => cell.Weight, v => cell.Weight = v, double.NaN);
                        tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                        Grid.SetRow(tb, row); Grid.SetColumn(tb, 2 + k); grid.Children.Add(tb);
                    }
                    if (t == GeoInfo.Tiers - 1 && r < GeoInfo.Regions - 1)
                    {
                        ++row; grid.RowDefinitions.Add(new RowDefinition(new GridLength(6)));
                    }
                }
            root.Children.Add(grid);
            root.Children.Add(Note("Land: sedimentary on top, metamorphic below. Seabed: igneous rock. Uplift: the highest land. Volcanic: only where painted by hand " +
                "(Geology > Paint regions). Seabed is the ground under the water line (Water Mod) or under lakes if the planet has no water."));

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            var reset = new Button { Content = "Reset to defaults" };
            reset.Click += (_, _) =>
            {
                if (MessageBox.Show("Reset all recipes, uplift share and patch size?", "Geology", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                _g.UpliftPercent = 15; _g.PatchKm = 25; _g.ResetRecipes();
                Refresh(root);
            };
            var close = new Button { Content = "Close", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            close.Click += (_, _) => CloseWithResult(true);
            buttons.Children.Add(reset); buttons.Children.Add(close);
            root.Children.Add(buttons);
            Content = root;
        }

        static TextBlock Label(string t) => new TextBlock { Text = t, VerticalAlignment = VerticalAlignment.Center };
        static TextBlock Note(string t) => new TextBlock { Text = t, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray, FontSize = 12 };

        readonly System.Collections.Generic.List<Action> _refreshers = new();

        /// <summary>Number box: shows the value, writes valid input back when the text changes.</summary>
        TextBox Num(Func<double> get, Action<double> set, double width)
        {
            var tb = new TextBox { Text = get().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), MinWidth = 50 };
            if (!double.IsNaN(width)) tb.Width = width;
            tb.TextChanged += (_, _) =>
            {
                if (double.TryParse(tb.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v >= 0 && Math.Abs(v - get()) > 1e-9)
                    set(v);
            };
            _refreshers.Add(() => tb.Text = get().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            return tb;
        }

        void Refresh(Control _) { foreach (var a in _refreshers) a(); }
    }
}
