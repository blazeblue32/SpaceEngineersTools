using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// Default colors for the ore overlay on the map plus the user's own choices.
    /// The 48 defaults are picked to be far apart from each other (CIE Lab distance, closest pair ~23),
    /// strongly saturated so they stand out from the gray height/slope map, and far from the
    /// green tinted biome background and the red lake tint (dark and mid-tone greens are left out on purpose, they vanish on the biome view). They are ordered so that the
    /// first colors handed out are the most different from each other.
    /// </summary>
    public static class OrePalette
    {
        public static readonly string[] Hex =
        {
            "#0000ff", "#00ff00", "#ff7869", "#00f0ff", "#2d3c87", "#ffe11e", "#ff00c3", "#5a4b00",
            "#ffc3ff", "#3cd278", "#87004b", "#a569ff", "#ffd296", "#00a5f0", "#e18700", "#ff0f78",
            "#87d20f", "#d200ff", "#780087", "#e169b4", "#4b87ff", "#b4c35a", "#a55a5a", "#3c0fb4",
            "#9678b4", "#00ffd2", "#b4783c", "#5aff69", "#ffa5b4", "#ff78ff", "#b49600", "#1ea5b4",
            "#0f6996", "#c396ff", "#003cb4", "#5a2d5a", "#a5f078", "#005aff", "#b40fc3", "#b4c3ff",
            "#d20087", "#d24b69", "#873cff", "#ffe169", "#b4c300", "#69d2ff", "#b45ac3", "#ffa578"
        };

        public static int Count => Hex.Length;

        /// <summary>Default color for the n-th ore type. Wraps around after the last color.</summary>
        public static Color Default(int index) => Color.Parse(Hex[((index % Hex.Length) + Hex.Length) % Hex.Length]);

        // ---- user colors, saved per ore type name
        static readonly object _lock = new();
        static Dictionary<string, Color> _custom;
        static string FilePath => Path.Combine(PathHelper.UserConfigDirectory, "OreColors.xml");

        static Dictionary<string, Color> Custom
        {
            get
            {
                if (_custom != null) return _custom;
                _custom = new Dictionary<string, Color>();
                try
                {
                    if (File.Exists(FilePath))
                    {
                        var doc = XDocument.Load(FilePath);
                        foreach (var e in doc.Root?.Elements("Color") ?? Enumerable.Empty<XElement>())
                        {
                            var type = e.Attribute("Type")?.Value;
                            if (!string.IsNullOrEmpty(type) && Color.TryParse(e.Attribute("Value")?.Value, out var c))
                                _custom[type] = c;
                        }
                    }
                }
                catch { /* broken file: start empty */ }
                return _custom;
            }
        }

        public static bool TryGetCustom(string oreType, out Color color)
        {
            lock (_lock) return Custom.TryGetValue(oreType ?? "", out color);
        }

        /// <summary>Stores a custom color, or removes it when color is null. Saves immediately.</summary>
        public static void SetCustom(string oreType, Color? color)
        {
            if (string.IsNullOrEmpty(oreType)) return;
            lock (_lock)
            {
                if (color.HasValue) Custom[oreType] = color.Value; else Custom.Remove(oreType);
                try
                {
                    var root = new XElement("OreColors");
                    foreach (var kv in Custom.OrderBy(k => k.Key))
                        root.Add(new XElement("Color", new XAttribute("Type", kv.Key), new XAttribute("Value", ToHex(kv.Value))));
                    new XDocument(root).Save(FilePath);
                }
                catch { /* read-only config dir: keep working in memory */ }
            }
        }

        public static string ToHex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}";

        /// <summary>Black or white, whichever reads better on the given color.</summary>
        public static Color TextOn(Color c) =>
            (c.R * 299 + c.G * 587 + c.B * 114) / 1000 > 140 ? Color.FromRgb(0x22, 0x22, 0x22) : Colors.White;
    }
}
