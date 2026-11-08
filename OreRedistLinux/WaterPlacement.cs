using SpaceEngineersToolsShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Statistics of one ore for the placement report.</summary>
    public class OreReportRow
    {
        public string Name;
        public OreWaterMode Mode;
        /// <summary>Veins the dice wanted to place.</summary>
        public int Events;
        /// <summary>Veins that found a site (maybe after looking elsewhere).</summary>
        public int Placed;
        /// <summary>Veins for which no suitable site was found on their cube face.</summary>
        public int Dropped;
        /// <summary>Veins that had to move to another site because the first one was too wet.</summary>
        public int Moved;
        /// <summary>Veins whose depth layer was made shallower to stay above the water.</summary>
        public int ClampedVeins;
        /// <summary>Veins whose first site was on land / on the seabed (only counted when water rules apply).</summary>
        public int VeinsLand, VeinsSea;
        public long Pixels;
        /// <summary>Single pixels of veins that were made shallower.</summary>
        public long ClampedPixels;
        /// <summary>Single pixels of veins that were left out because the layer would be wet.</summary>
        public long SkippedPixels;
    }

    /// <summary>
    /// Water rules for ore placement, see WaterSettings for the geometry.
    ///
    /// - AboveWater: the whole layer (Start..Start+Depth) has to end above water + margin. If the wanted
    ///   layer is too deep for a pixel the next shallower allowed layer is used ("clamp shallower").
    ///   If no allowed layer fits, the pixel is not used.
    /// - UnderwaterOnly: only pixels where the terrain is below the water line.
    /// - LandAndSeabed: both at once, decided per pixel: land pixels follow the AboveWater rule, pixels under
    ///   standing water take any layer. Veins can cross the shore without stepping back.
    /// - Anywhere: unchanged behavior.
    /// New veins that start on a pixel where nothing fits look for another site on the same cube face,
    /// so ore keeps its share and is not simply lost on faces with a lot of ocean.
    /// </summary>
    internal static class WaterPlacement
    {
        public const int SiteTries = 400;

        /// <summary>
        /// Index of the layer to use at this pixel (never deeper than idx, never shallower than minIdx), or -1.
        /// </summary>
        public static int IndexAt(WaterSettings w, OreWaterMode mode, ImageData img, IList<OreMapping> mappings, int x, int y, int idx, int minIdx)
        {
            if (idx < 0 || idx >= mappings.Count) return -1;
            if (mode == OreWaterMode.Anywhere) return idx;
            var h = img.H16[x, y];
            if (mode == OreWaterMode.UnderwaterOnly) return w.IsUnderwater(h) ? idx : -1;
            if (mode == OreWaterMode.LandAndSeabed && w.IsUnderwater(h)) return idx;

            double allowed = w.AllowedDepth(h);
            if (minIdx < 0) minIdx = 0;
            for (int i = idx; i >= minIdx; --i)
            {
                var m = mappings[i];
                if (m.Value > 0 && m.Start + m.Depth <= allowed) return i;
            }
            return -1;
        }

        /// <summary>
        /// First try the pixel the dice chose, then random pixels of the same cube face.
        /// For Land + seabed with a fixed seabed share the dice first decide land or seabed for this vein
        /// (and fall back to the other if the face has none). Returns false if nothing was found.
        /// </summary>
        public static bool FindSite(WaterSettings w, OreWaterMode mode, ImageData img, IList<OreMapping> mappings,
            int x, int y, int idx, int minIdx, Random rnd, int tileWidth,
            out int siteX, out int siteY, out int siteIdx, out bool siteIsSea)
        {
            bool? wantSea = null;
            if (mode == OreWaterMode.LandAndSeabed && w.SeabedShare.HasValue)
                wantSea = rnd.NextDouble() * 100 < w.SeabedShare.Value;

            for (int pass = 0; pass < (wantSea.HasValue ? 2 : 1); ++pass)
            {
                // pass 1 (only with a wish): give up the wish, any suitable site will do
                bool? want = pass == 0 ? wantSea : null;
                siteX = x; siteY = y;
                for (int i = 0; i <= SiteTries; ++i)
                {
                    if (i > 0) { siteX = rnd.Next(tileWidth); siteY = rnd.Next(tileWidth); }
                    bool sea = w.IsUnderwater(img.H16[siteX, siteY]);
                    if (want.HasValue && sea != want.Value) continue;
                    siteIdx = IndexAt(w, mode, img, mappings, siteX, siteY, idx, minIdx);
                    if (siteIdx >= 0) { siteIsSea = sea; return true; }
                }
            }
            siteX = x; siteY = y; siteIdx = -1; siteIsSea = false;
            return false;
        }

        public static string BuildReport(WaterSettings w, bool waterUsed, IEnumerable<OreReportRow> rows)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Ore placement report");
            if (waterUsed)
            {
                sb.AppendLine("Water rules: ON  (" + w.Describe() + ")");
                if (w.SeabedShare.HasValue) sb.AppendLine($"Land + seabed ores: {w.SeabedShare.Value:0}% of their veins are sent to the seabed.");
                sb.AppendLine(string.IsNullOrWhiteSpace(w.Summary) ? "" : w.Summary);
            }
            else sb.AppendLine("Water rules: off");
            sb.AppendLine();
            sb.AppendLine("Ore".PadRight(18) + "Water mode".PadRight(26) + "Veins".PadLeft(7) + "Placed".PadLeft(8) + "Dropped".PadLeft(9) + "Moved".PadLeft(7) +
                          "Shallower".PadLeft(11) + "On land".PadLeft(9) + "Seabed".PadLeft(8) + "Pixels".PadLeft(9) + "Skipped px".PadLeft(12));
            foreach (var r in rows.OrderBy(r => r.Name))
            {
                sb.AppendLine(r.Name.PadRight(18) + OreWaterModes.NameOf(r.Mode).PadRight(26) + r.Events.ToString().PadLeft(7) + r.Placed.ToString().PadLeft(8) +
                              r.Dropped.ToString().PadLeft(9) + r.Moved.ToString().PadLeft(7) + r.ClampedVeins.ToString().PadLeft(11) +
                              r.VeinsLand.ToString().PadLeft(9) + r.VeinsSea.ToString().PadLeft(8) +
                              r.Pixels.ToString().PadLeft(9) + r.SkippedPixels.ToString().PadLeft(12));
            }
            sb.AppendLine();
            sb.AppendLine("Veins: how many the random placement wanted. Dropped: no suitable site on the cube face (shortfall).");
            sb.AppendLine("Moved: first site was not allowed, another site on the same face was used. Shallower: layer made shallower to stay dry.");
            sb.AppendLine("On land / Seabed: where the vein started (land = terrain above the water line).");
            return sb.ToString();
        }

        /// <summary>Ores that lost a noticeable part of their veins, as short lines for the message box.</summary>
        public static List<string> Shortfalls(IEnumerable<OreReportRow> rows, double minShare = 0.05)
        {
            return rows.Where(r => r.Events > 0 && r.Dropped > r.Events * minShare)
                .OrderByDescending(r => (double)r.Dropped / r.Events)
                .Select(r => $"{r.Name}: {100.0 * r.Placed / r.Events:0}% of its veins placed ({r.Dropped} of {r.Events} found no dry site)")
                .ToList();
        }
    }
}
