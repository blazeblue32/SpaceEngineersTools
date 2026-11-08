using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Globalization;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Weights of the five rock types for one ore (geology placement).</summary>
    public class RockWeightsWindow : DialogWindow
    {
        readonly RedistributionSetupViewModel.OreInfo _info;
        readonly System.Collections.Generic.List<TextBox> _boxes = new();

        internal RockWeightsWindow(RedistributionSetupViewModel.OreInfo info)
        {
            _info = info;
            Title = "Rock types of " + info.Name;
            Width = 520; SizeToContent = SizeToContent.Height; CanResize = false;

            var root = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            root.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray, FontSize = 12,
                Text = "How well the ore suits each kind of rock (0 = never forms in it, 100 = normal, higher = prefers it). A vein is placed where the rock of its " +
                       "layer suits the ore: an ore in high grade metamorphic rock is deep under ordinary land, but near the surface in uplift regions."
            });
            var grid = new Grid { ColumnSpacing = 8, RowSpacing = 4 };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(80)));
            for (int k = 0; k < GeoInfo.Rocks; ++k)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var name = new TextBlock { Text = GeoInfo.RockNames[k], VerticalAlignment = VerticalAlignment.Center, MinWidth = 160 };
                Grid.SetRow(name, k); grid.Children.Add(name);
                var rock = info.Rocks[k];
                var tb = new TextBox { Text = rock.Weight.ToString("0.##", CultureInfo.InvariantCulture) };
                tb.TextChanged += (_, _) =>
                {
                    if (double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0) rock.Weight = v;
                };
                Grid.SetRow(tb, k); Grid.SetColumn(tb, 1); grid.Children.Add(tb);
                _boxes.Add(tb);
            }
            root.Children.Add(grid);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(Btn("All equal", () => SetAll(100)));
            buttons.Children.Add(Btn("None", () => SetAll(0)));
            var close = Btn("Close", () => CloseWithResult(true)); close.MinWidth = 80;
            buttons.Children.Add(close);
            root.Children.Add(buttons);
            Content = root;
        }

        static Button Btn(string text, Action a)
        {
            var b = new Button { Content = text, HorizontalContentAlignment = HorizontalAlignment.Center };
            b.Click += (_, _) => a();
            return b;
        }

        void SetAll(double v)
        {
            for (int k = 0; k < _boxes.Count; ++k) { _info.Rocks[k].Weight = v; _boxes[k].Text = v.ToString("0.##", CultureInfo.InvariantCulture); }
        }
    }
}
