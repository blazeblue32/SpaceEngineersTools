using SpaceEngineersToolsShared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    // ==========================================================================================================
    // Simulated geology. Not the exact rock (granite, quartz...), only the kind of rock and where it is found:
    //
    //   regions  (map):   Land, Seabed, Uplift (mountains), Volcanic (painted by hand)
    //   rock layers:      Surface (ore layers 1-3), Layer 1 (4-6), Layer 2 (7-10)
    //   rock types:       sedimentary, metamorphic low / high grade, igneous extrusive / intrusive
    //
    // Every region has a recipe: for each rock layer the weights of the rock types. The rock at a place is drawn from the
    // recipe of its region with smooth noise (so rocks form patches). Every ore has weights per rock type. A vein is placed
    // at a place and layer where the rock of that layer suits the ore, so e.g. an ore that lives in high grade
    // metamorphic rock is deep under the land but at the surface in uplift regions.
    // ==========================================================================================================

    public enum RockType { Sedimentary = 0, MetamorphicLow = 1, MetamorphicHigh = 2, IgneousExtrusive = 3, IgneousIntrusive = 4 }

    public enum GeoRegion { Land = 0, Seabed = 1, Uplift = 2, Volcanic = 3 }

    public static class GeoInfo
    {
        public const int Rocks = 5, Regions = 4, Tiers = 3;

        public static readonly string[] RockNames =
            { "Sedimentary", "Metamorphic, low grade", "Metamorphic, high grade", "Igneous extrusive", "Igneous intrusive" };
        public static readonly string[] RockShort = { "Sed", "MetL", "MetH", "Extr", "Intr" };
        public static readonly string[] RegionNames = { "Land", "Seabed", "Uplift", "Volcanic" };
        public static readonly string[] TierNames = { "Surface", "Layer 1", "Layer 2" };
        public static readonly string[] TierLayers = { "layers 1-3", "layers 4-6", "layers 7-10" };

        /// <summary>The ten ore layers (index 0-9) form three rock layers: surface 0-2, layer 1 3-5, layer 2 6-9.</summary>
        public static int TierOf(int layerIndex) => layerIndex <= 2 ? 0 : layerIndex <= 5 ? 1 : 2;
    }

    /// <summary>One weight of the recipe table (UI binding).</summary>
    public class RecipeCell : PropChangeNotifier
    {
        public GeoRegion Region { get; }
        public int Tier { get; }
        public RockType Rock { get; }
        double _weight;
        public double Weight { get => _weight; set => SetProp(ref _weight, Math.Clamp(value, 0, 1000)); }
        public RecipeCell(GeoRegion region, int tier, RockType rock, double weight) { Region = region; Tier = tier; Rock = rock; _weight = weight; }
    }

    /// <summary>Geology settings of a planet: the recipes and the size of the patches. Saved per planet in the config folder.</summary>
    public class GeologySettings : PropChangeNotifier
    {
        /// <summary>Share of the land (in percent, by height) that counts as uplift. 0 = none.</summary>
        double _upliftPercent = 15;
        public double UpliftPercent { get => _upliftPercent; set { if (SetProp(ref _upliftPercent, Math.Clamp(value, 0, 90))) Changed?.Invoke(); } }

        /// <summary>Size of the rock patches in km.</summary>
        double _patchKm = 25;
        public double PatchKm { get => _patchKm; set { if (SetProp(ref _patchKm, Math.Clamp(value, 0.5, 5000))) Changed?.Invoke(); } }

        public List<RecipeCell> Cells { get; } = new();
        public event Action Changed;

        public GeologySettings() { ResetRecipes(); }

        public double Weight(GeoRegion region, int tier, RockType rock) =>
            Cells[((int)region * GeoInfo.Tiers + tier) * GeoInfo.Rocks + (int)rock].Weight;

        /// <summary>
        /// Defaults after the geology guide: ocean floors are igneous extrusive over intrusive; the top of a continent is
        /// sedimentary (some extrusive) over metamorphic rock that gets higher grade with depth; uplift shows metamorphic or
        /// intrusive rock at the surface; volcanic areas are extrusive with intrusive below and the occasional dike.
        /// </summary>
        public void ResetRecipes()
        {
            //                Sed  MetL MetH Extr Intr
            double[][][] d =
            {
                // Land
                new[] { new double[] { 80, 0, 0, 20, 0 }, new double[] { 15, 70, 0, 0, 15 }, new double[] { 0, 10, 55, 0, 35 } },
                // Seabed
                new[] { new double[] { 0, 0, 0, 100, 0 }, new double[] { 0, 0, 0, 20, 80 }, new double[] { 0, 0, 30, 0, 70 } },
                // Uplift
                new[] { new double[] { 0, 40, 20, 0, 40 }, new double[] { 0, 0, 60, 0, 40 }, new double[] { 0, 0, 50, 0, 50 } },
                // Volcanic
                new[] { new double[] { 0, 0, 0, 90, 10 }, new double[] { 0, 0, 0, 40, 60 }, new double[] { 0, 0, 20, 0, 80 } },
            };
            bool first = Cells.Count == 0;
            for (int r = 0; r < GeoInfo.Regions; ++r)
                for (int t = 0; t < GeoInfo.Tiers; ++t)
                    for (int k = 0; k < GeoInfo.Rocks; ++k)
                    {
                        int i = (r * GeoInfo.Tiers + t) * GeoInfo.Rocks + k;
                        if (first)
                        {
                            var cell = new RecipeCell((GeoRegion)r, t, (RockType)k, d[r][t][k]);
                            cell.PropertyChanged += (_, _) => Changed?.Invoke();
                            Cells.Add(cell);
                        }
                        else Cells[i].Weight = d[r][t][k];
                    }
            Changed?.Invoke();
        }

        // ----- persistence: ~/.config/OreRedistLinux/Geology/<planet>/geology.xml
        public static string FolderFor(string planet)
        {
            var bad = Path.GetInvalidFileNameChars();
            var name = string.Concat((string.IsNullOrWhiteSpace(planet) ? "_planet" : planet).Select(c => bad.Contains(c) ? '_' : c));
            var dir = Path.Combine(PathHelper.UserConfigDirectory, "Geology", name);
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        public void Save(string planet)
        {
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var root = new XElement("Geology",
                    new XAttribute("UpliftPercent", UpliftPercent.ToString(inv)),
                    new XAttribute("PatchKm", PatchKm.ToString(inv)),
                    Cells.Select(c => new XElement("W", new XAttribute("R", (int)c.Region), new XAttribute("T", c.Tier), new XAttribute("K", (int)c.Rock),
                        new XAttribute("V", c.Weight.ToString(inv)))));
                new XDocument(root).Save(Path.Combine(FolderFor(planet), "geology.xml"));
            }
            catch { }
        }

        public static GeologySettings Load(string planet)
        {
            var g = new GeologySettings();
            try
            {
                var file = Path.Combine(FolderFor(planet), "geology.xml");
                if (!File.Exists(file)) return g;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var root = XDocument.Load(file).Root;
                if (double.TryParse(root.Attribute("UpliftPercent")?.Value, System.Globalization.NumberStyles.Float, inv, out var up)) g._upliftPercent = Math.Clamp(up, 0, 90);
                if (double.TryParse(root.Attribute("PatchKm")?.Value, System.Globalization.NumberStyles.Float, inv, out var pk)) g._patchKm = Math.Clamp(pk, 0.5, 5000);
                foreach (var e in root.Elements("W"))
                    if (int.TryParse(e.Attribute("R")?.Value, out var r) && int.TryParse(e.Attribute("T")?.Value, out var t) && int.TryParse(e.Attribute("K")?.Value, out var k)
                        && double.TryParse(e.Attribute("V")?.Value, System.Globalization.NumberStyles.Float, inv, out var v)
                        && r >= 0 && r < GeoInfo.Regions && t >= 0 && t < GeoInfo.Tiers && k >= 0 && k < GeoInfo.Rocks)
                        g.Cells[(r * GeoInfo.Tiers + t) * GeoInfo.Rocks + k].Weight = v;
            }
            catch { }
            return g;
        }
    }

    /// <summary>
    /// Region map of a planet at low resolution (512 x 512 cells per cube face, laid out like the map in the main window:
    /// one 2048 x 1536 image). Automatic regions (seabed, uplift) plus what was painted by hand; paint wins.
    /// </summary>
    public class RegionMap
    {
        public const int Res = 512;
        public const int Width = 4 * Res, Height = 3 * Res;

        /// <summary>0 land, 1 seabed, 2 uplift (computed).</summary>
        public byte[] Auto = new byte[Width * Height];
        /// <summary>0 = nothing painted, else region + 1 (1 land, 2 seabed, 3 uplift, 4 volcanic).</summary>
        public readonly byte[] Paint = new byte[Width * Height];

        public static (int x, int y) Offset(CubeMapFace face) => face switch
        {
            CubeMapFace.Up => (0, 0),
            CubeMapFace.Front => (0, Res),
            CubeMapFace.Right => (Res, Res),
            CubeMapFace.Back => (2 * Res, Res),
            CubeMapFace.Left => (3 * Res, Res),
            _ => (2 * Res, 2 * Res), // Down
        };

        /// <summary>Is this cell of the cross layout part of one of the six faces?</summary>
        public static bool InFace(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            int col = x / Res, row = y / Res;
            return (row == 0 && col == 0) || (row == 1) || (row == 2 && col == 2);
        }

        public static bool TryCell(CubeMapFace face, int px, int py, int tileWidth, out int index)
        {
            var (ox, oy) = Offset(face);
            int cx = (int)((long)px * Res / tileWidth), cy = (int)((long)py * Res / tileWidth);
            index = (oy + Math.Clamp(cy, 0, Res - 1)) * Width + ox + Math.Clamp(cx, 0, Res - 1);
            return true;
        }

        public GeoRegion RegionAtIndex(int i) => Paint[i] > 0 ? (GeoRegion)(Paint[i] - 1) : (GeoRegion)Auto[i];

        public GeoRegion At(CubeMapFace face, int px, int py, int tileWidth)
        {
            TryCell(face, px, py, tileWidth, out int i);
            return RegionAtIndex(i);
        }

        public bool HasPaint => Paint.Any(b => b != 0);

        /// <summary>Share of each region over all faces.</summary>
        public double[] Shares()
        {
            var counts = new long[GeoInfo.Regions]; long total = 0;
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                {
                    if (!InFace(x, y)) continue;
                    ++counts[(int)RegionAtIndex(y * Width + x)]; ++total;
                }
            return counts.Select(c => total == 0 ? 0 : c / (double)total).ToArray();
        }

        /// <summary>
        /// Computes the automatic regions from the maps. Seabed: under the water line if the Water Mod rules are on, otherwise the lakes
        /// (red channel value lakeValue, the "Lake areas" of the vanilla planets). Uplift: the highest upliftPercent of the land by height.
        /// </summary>
        internal void RebuildAuto(IDictionary<CubeMapFace, ImageData> images, WaterSettings water, int lakeValue, double upliftPercent, int tileWidth)
        {
            var auto = new byte[Width * Height]; // swapped in at the end: readers never see a half built map
            bool useWater = water != null && water.Enabled && water.HasUsableHills;
            var landHeights = new List<ushort>();
            foreach (var kv in images)
            {
                var img = kv.Value; if (img?.R == null) continue;
                var (ox, oy) = Offset(kv.Key);
                for (int cy = 0; cy < Res; ++cy)
                    for (int cx = 0; cx < Res; ++cx)
                    {
                        int px = Math.Min(tileWidth - 1, cx * tileWidth / Res + tileWidth / (2 * Res));
                        int py = Math.Min(tileWidth - 1, cy * tileWidth / Res + tileWidth / (2 * Res));
                        bool sea = useWater && img.HasHeightMap ? water.IsUnderwater(img.H16[px, py]) : (!useWater && img.R[px, py] == lakeValue && lakeValue > 0);
                        int i = (oy + cy) * Width + ox + cx;
                        if (sea) auto[i] = 1;
                        else if (img.HasHeightMap) landHeights.Add(img.H16[px, py]);
                    }
            }
            if (upliftPercent <= 0 || landHeights.Count == 0) { Auto = auto; return; }
            landHeights.Sort();
            ushort threshold = landHeights[(int)Math.Clamp((1 - upliftPercent / 100) * landHeights.Count, 0, landHeights.Count - 1)];
            foreach (var kv in images)
            {
                var img = kv.Value; if (img?.R == null || !img.HasHeightMap) continue;
                var (ox, oy) = Offset(kv.Key);
                for (int cy = 0; cy < Res; ++cy)
                    for (int cx = 0; cx < Res; ++cx)
                    {
                        int i = (oy + cy) * Width + ox + cx;
                        if (auto[i] != 0) continue;
                        int px = Math.Min(tileWidth - 1, cx * tileWidth / Res + tileWidth / (2 * Res));
                        int py = Math.Min(tileWidth - 1, cy * tileWidth / Res + tileWidth / (2 * Res));
                        if (img.H16[px, py] > threshold) auto[i] = 2;
                    }
            }
            Auto = auto;
        }

        // ----- paint file: one 2048 x 1536 gray image, value = painted region + 1 (0 = nothing)
        public void SavePaint(string file)
        {
            try
            {
                if (!HasPaint) { if (File.Exists(file)) File.Delete(file); return; }
                using var img = new Image<L8>(Width, Height);
                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                        img[x, y] = new L8(Paint[y * Width + x]);
                img.SaveAsPng(file);
            }
            catch { }
        }

        public bool LoadPaint(string file)
        {
            Array.Clear(Paint, 0, Paint.Length);
            try
            {
                if (!File.Exists(file)) return false;
                using var img = Image.Load<L8>(file);
                if (img.Width != Width || img.Height != Height) return false;
                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                    {
                        byte v = img[x, y].PackedValue;
                        Paint[y * Width + x] = v <= 4 && InFace(x, y) ? v : (byte)0;
                    }
                return true;
            }
            catch { return false; }
        }
    }

    /// <summary>What the placement engine asks: the region and the rock at a place.</summary>
    internal class GeologyModel
    {
        public readonly RegionMap Map;
        readonly double[][][] _cum = new double[GeoInfo.Regions][][];   // cumulative recipe weights [region][tier][rock]
        readonly GlobField[] _noise = new GlobField[GeoInfo.Tiers];
        readonly int _tileWidth;

        public GeologyModel(RegionMap map, GeologySettings settings, double planetRadius, int tileWidth, int seed)
        {
            Map = map; _tileWidth = tileWidth;
            for (int r = 0; r < GeoInfo.Regions; ++r)
            {
                _cum[r] = new double[GeoInfo.Tiers][];
                for (int t = 0; t < GeoInfo.Tiers; ++t)
                {
                    var c = new double[GeoInfo.Rocks]; double sum = 0;
                    for (int k = 0; k < GeoInfo.Rocks; ++k) { sum += Math.Max(0, settings.Weight((GeoRegion)r, t, (RockType)k)); c[k] = sum; }
                    if (sum <= 0) { c = new double[] { 1, 1, 1, 1, 1 }; sum = 1; } // empty recipe: sedimentary
                    for (int k = 0; k < c.Length; ++k) c[k] /= sum;
                    _cum[r][t] = c;
                }
            }
            for (int t = 0; t < GeoInfo.Tiers; ++t)
                _noise[t] = new GlobField(GlobField.StableHash("rock" + t, seed), planetRadius, settings.PatchKm * 1000, 0.2);
        }

        public GeoRegion RegionAt(CubeMapFace face, int x, int y) => Map.At(face, x, y, _tileWidth);

        public RockType RockAt(CubeMapFace face, int x, int y, int tier)
        {
            var region = RegionAt(face, x, y);
            var p = CoordinateHelper.GetNormalizedSphereCoordinates(face, x, y, _tileWidth);
            var f = _noise[tier];
            double u = f.Rank(f.Value(p.X, p.Y, p.Z));
            var c = _cum[(int)region][tier];
            for (int k = 0; k < c.Length; ++k) if (u <= c[k]) return (RockType)k;
            return RockType.Sedimentary;
        }
    }
}
