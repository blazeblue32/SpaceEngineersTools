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
        /// <summary>Veins that wanted land but were sent to the seabed (no fitting or no room on land).</summary>
        public int Diverted;
        /// <summary>Extra veins placed afterwards to make up for lost pixels, and the pixels they added.</summary>
        public int TopUpVeins;
        public long TopUpPixels;
        /// <summary>Pixels per layer: what the tool places without water rules (target) and what the map got.</summary>
        public long[] LayerTarget = new long[10];
        public long[] LayerAchieved = new long[10];
        public long Pixels;
        /// <summary>Single pixels of veins that were made shallower.</summary>
        public long ClampedPixels;
        /// <summary>Single pixels of veins that were left out because the layer would be wet.</summary>
        public long SkippedPixels;
    }

    /// <summary>Text of the placement report and the short shortfall list. The placement itself is in OrePlacer.</summary>
    internal static class WaterPlacement
    {
        public static string BuildReport(WaterSettings w, bool waterUsed, IEnumerable<OreReportRow> rows, string densityLine = "", bool hasReference = false)
        {
            rows = rows.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Ore placement report");
            if (waterUsed)
            {
                sb.AppendLine("Water rules: ON  (" + w.Describe() + ")");
                if (!string.IsNullOrWhiteSpace(w.Summary)) sb.AppendLine(w.Summary);
                if (w.SeabedShare.HasValue) sb.AppendLine($"Land + seabed ores: {w.SeabedShare.Value:0}% of their veins are sent to the seabed.");
                sb.AppendLine($"Keep ratios: {(hasReference ? "on (reference run + top-up)" : "off")}, land density limit: {(w.LandDensityLimit > 0 ? w.LandDensityLimit.ToString("0.##") + "x normal" : "none")}, " +
                              $"shallower layers allowed: {(w.AllowShallower ? "yes" : "no")}");
                if (!string.IsNullOrEmpty(densityLine)) sb.AppendLine(densityLine);
            }
            else sb.AppendLine("Water rules: off");
            sb.AppendLine();
            sb.AppendLine("Ore".PadRight(18) + "Water mode".PadRight(26) + "Veins".PadLeft(7) + "Placed".PadLeft(8) + "Dropped".PadLeft(9) +
                          "Shallower".PadLeft(11) + "On land".PadLeft(9) + "Seabed".PadLeft(8) + "Diverted".PadLeft(10) + "Top-up".PadLeft(8) +
                          "Pixels".PadLeft(9) + "Skipped px".PadLeft(12));
            foreach (var r in rows.OrderBy(r => r.Name))
            {
                sb.AppendLine(r.Name.PadRight(18) + OreWaterModes.NameOf(r.Mode).PadRight(26) + r.Events.ToString().PadLeft(7) + r.Placed.ToString().PadLeft(8) +
                              r.Dropped.ToString().PadLeft(9) + r.ClampedVeins.ToString().PadLeft(11) +
                              r.VeinsLand.ToString().PadLeft(9) + r.VeinsSea.ToString().PadLeft(8) + r.Diverted.ToString().PadLeft(10) + r.TopUpVeins.ToString().PadLeft(8) +
                              r.Pixels.ToString().PadLeft(9) + r.SkippedPixels.ToString().PadLeft(12));
            }
            sb.AppendLine();
            sb.AppendLine("Veins: wanted by the random placement. Dropped: no suitable site found. Shallower: layer made shallower (only with that option).");
            sb.AppendLine("On land / Seabed: where the vein started. Diverted: wanted land, sent to the seabed (no fitting land with room). Top-up: extra veins placed afterwards.");

            if (hasReference)
            {
                sb.AppendLine();
                sb.AppendLine("Pixels per layer in the map as % of the target (what the tool places without water rules). 100% = ratio kept.");
                sb.AppendLine("Ore".PadRight(18) + string.Concat(Enumerable.Range(0, 10).Select(i => ("L" + i).PadLeft(7))));
                foreach (var r in rows.OrderBy(r => r.Name))
                {
                    if (r.Mode == OreWaterMode.Anywhere) { sb.AppendLine(r.Name.PadRight(18) + "(ignores the water, not compensated)"); continue; }
                    sb.AppendLine(r.Name.PadRight(18) + string.Concat(Enumerable.Range(0, 10).Select(i =>
                        r.LayerTarget[i] <= 0 ? "-".PadLeft(7) : (100.0 * r.LayerAchieved[i] / r.LayerTarget[i]).ToString("0", CultureInfo.InvariantCulture).PadLeft(6) + "%")));
                }
                sb.AppendLine("Below 100% where no ground is left that is dry enough (or not full yet) for that layer: lower the water radius,");
                sb.AppendLine("raise the density limit, or set the ore to Land + seabed so it can use the seabed.");
            }
            return sb.ToString();
        }

        /// <summary>Ores that lost veins, or whose layers stay clearly below the target, as short lines for the message box.</summary>
        public static List<string> Shortfalls(IEnumerable<OreReportRow> rows, bool hasReference, double minShare = 0.05)
        {
            var lines = new List<string>();
            foreach (var r in rows.OrderBy(r => r.Name))
            {
                if (r.Events > 0 && r.Dropped > r.Events * minShare)
                    lines.Add($"{r.Name}: {100.0 * r.Placed / r.Events:0}% of its veins placed ({r.Dropped} of {r.Events} found no site)");
                if (hasReference && r.Mode != OreWaterMode.Anywhere)
                {
                    long t = r.LayerTarget.Sum(), a2 = 0;
                    var low = new List<string>();
                    for (int i = 0; i < 10; ++i)
                    {
                        a2 += Math.Min(r.LayerAchieved[i], r.LayerTarget[i]);
                        if (r.LayerTarget[i] > 0 && r.LayerAchieved[i] < 0.85 * r.LayerTarget[i]) low.Add($"L{i} {100.0 * r.LayerAchieved[i] / r.LayerTarget[i]:0}%");
                    }
                    if (low.Count > 0) lines.Add($"{r.Name}: below target on {string.Join(", ", low)}");
                }
            }
            return lines;
        }
    }
}
