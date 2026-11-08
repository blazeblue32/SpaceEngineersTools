using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>One ore of a data pack: how common it is in the Earth's crust and which rocks it forms in. Both are optional.</summary>
    public class OrePackOre
    {
        public string Name;
        /// <summary>ppm by mass in the Earth's crust, null = not given.</summary>
        public double? Abundance;
        /// <summary>Suitability for Sed, MetL, MetH, Extr, Intr (0-1000), null = not given.</summary>
        public double[] Rocks;
        /// <summary>Regional strength in percent (0 = evenly spread, 100 = only inside its regions), region size in km and the share of the planet in percent inside the regions. Null = not given.</summary>
        public double? Glob, GlobSize, GlobCoverage;
    }

    /// <summary>A data pack: ore data loaded from an XML file of the DataPacks folder.</summary>
    public class OrePack
    {
        public string FileName;
        public string Name, Author, Description;
        public int Order;
        public List<OrePackOre> Ores = new();
        public override string ToString() => Name;
    }

    /// <summary>Data of one ore after all selected packs are combined.</summary>
    public class OreData
    {
        public double? Abundance;
        public double[] Rocks;
        public double? Glob, GlobSize, GlobCoverage;
    }

    /// <summary>
    /// Ore data packs (~/.config/OreRedistLinux/DataPacks/*.xml). A pack is plain data: it is read with an XML reader that prohibits DTDs and
    /// external resources, has to start with the header &lt;OreDataPack Format="1" Tool="OreRedistLinux"&gt;, is size limited, and every value is
    /// checked (names against a strict pattern, numbers clamped). Nothing in a pack is ever executed or used as a path.
    /// The built-in packs (embedded in the program) are written to the folder when they are missing.
    /// </summary>
    public static class OrePacks
    {
        public const string Header = "OreDataPack", ToolName = "OreRedistLinux", FormatVersion = "1";
        public const long MaxFileBytes = 1_000_000;
        public const int MaxOres = 5000, MaxPacks = 100;
        const string ResourcePrefix = "DataPacks.";

        static readonly Regex OreNamePattern = new(@"^[A-Za-z0-9][A-Za-z0-9 _.\-+()]{0,63}$", RegexOptions.Compiled);

        public static string Folder
        {
            get
            {
                var dir = Path.Combine(PathHelper.UserConfigDirectory, "DataPacks");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        /// <summary>Writes the built-in packs that do not exist in the folder (never overwrites a pack that is there, edited or not).</summary>
        public static void EnsureDefaults()
        {
            try
            {
                var folder = Folder;
                var asm = typeof(OrePacks).Assembly;
                foreach (var res in asm.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                {
                    var file = Path.Combine(folder, res.Substring(ResourcePrefix.Length));
                    if (File.Exists(file)) continue;
                    try
                    {
                        using var input = asm.GetManifestResourceStream(res);
                        using var output = new FileStream(file, FileMode.CreateNew, FileAccess.Write);
                        input.CopyTo(output);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>Loads every valid pack of the folder (after restoring missing built-in packs), sorted by Order. skipped = files that are not packs.</summary>
        public static List<OrePack> LoadAll(out List<string> skipped)
        {
            EnsureDefaults();
            skipped = new List<string>();
            var packs = new List<OrePack>();
            try
            {
                foreach (var path in Directory.EnumerateFiles(Folder, "*.xml").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    if (packs.Count >= MaxPacks) { skipped.Add("(more than " + MaxPacks + " files)"); break; }
                    var pack = TryLoad(path);
                    if (pack != null) packs.Add(pack); else skipped.Add(Path.GetFileName(path));
                }
            }
            catch { }
            return packs.OrderBy(p => p.Order).ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Loads one file. Null if it is not a valid pack (wrong header, too large, DTD, malformed...).</summary>
        public static OrePack TryLoad(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.LinkTarget != null || info.Length > MaxFileBytes) return null; // symbolic links are not followed
                byte[] bytes;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using var ms = new MemoryStream();
                    var buf = new byte[16384]; int n;
                    while ((n = fs.Read(buf, 0, buf.Length)) > 0)
                    {
                        ms.Write(buf, 0, n);
                        if (ms.Length > MaxFileBytes) return null;
                    }
                    bytes = ms.ToArray();
                }
                return Parse(bytes, Path.GetFileName(path));
            }
            catch { return null; }
        }

        internal static OrePack Parse(byte[] bytes, string fileName)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, // no DOCTYPE, no entities
                XmlResolver = null,                       // never load anything external
                MaxCharactersInDocument = MaxFileBytes * 2,
                MaxCharactersFromEntities = 0,
                IgnoreComments = true, IgnoreProcessingInstructions = true,
            };
            XDocument doc;
            using (var reader = XmlReader.Create(new MemoryStream(bytes), settings))
                doc = XDocument.Load(reader, LoadOptions.None);
            var root = doc.Root;
            if (root == null || root.Name.NamespaceName != "" || root.Name.LocalName != Header) return null;
            if ((string)root.Attribute("Format") != FormatVersion || (string)root.Attribute("Tool") != ToolName) return null;

            var pack = new OrePack
            {
                FileName = fileName,
                Name = Clean((string)root.Attribute("Name"), 60),
                Author = Clean((string)root.Attribute("Author"), 60),
                Description = Clean((string)root.Attribute("Description"), 300),
                Order = (int)Math.Clamp(Num((string)root.Attribute("Order")) ?? 100, -1000, 1000),
            };
            if (pack.Name.Length == 0) pack.Name = Path.GetFileNameWithoutExtension(fileName);
            foreach (var e in root.Elements("Ore"))
            {
                if (pack.Ores.Count >= MaxOres) break;
                var name = ((string)e.Attribute("Name") ?? "").Trim();
                if (!OreNamePattern.IsMatch(name)) continue;
                var ore = new OrePackOre { Name = name };
                var ab = Num((string)e.Attribute("Abundance"));
                if (ab.HasValue && ab.Value > 0) ore.Abundance = Math.Clamp(ab.Value, 1e-9, 1e9);
                var rocks = ((string)e.Attribute("Rocks") ?? "").Split(';');
                if (rocks.Length == GeoInfo.Rocks)
                {
                    var w = new double[GeoInfo.Rocks]; bool ok = true;
                    for (int i = 0; i < w.Length; ++i)
                    {
                        var v = Num(rocks[i]);
                        if (!v.HasValue) { ok = false; break; }
                        w[i] = Math.Clamp(v.Value, 0, 1000);
                    }
                    if (ok) ore.Rocks = w;
                }
                var gl = Num((string)e.Attribute("Glob"));
                if (gl.HasValue) ore.Glob = Math.Clamp(gl.Value, 0, 100);
                var gs = Num((string)e.Attribute("GlobSize"));
                if (gs.HasValue && gs.Value > 0) ore.GlobSize = Math.Clamp(gs.Value, 0.5, 5000);
                var gc = Num((string)e.Attribute("GlobCoverage"));
                if (gc.HasValue && gc.Value > 0) ore.GlobCoverage = Math.Clamp(gc.Value, 2, 90);
                if (ore.Abundance.HasValue || ore.Rocks != null || ore.Glob.HasValue) pack.Ores.Add(ore);
            }
            return pack;
        }

        static double? Num(string s) =>
            double.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !double.IsNaN(d) && !double.IsInfinity(d) ? d : null;

        /// <summary>Text for display only: control characters removed, length limited.</summary>
        static string Clean(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var t = new string(s.Where(c => !char.IsControl(c)).ToArray()).Trim();
            return t.Length > max ? t.Substring(0, max) : t;
        }

        /// <summary>Name without the version suffix: "Iron_02" -> "Iron".</summary>
        public static string BaseOreName(string name) => Regex.Replace((name ?? "").Trim(), @"[_\s]*\d+$", "");

        /// <summary>Combines the packs (in the order given) for one ore: a later pack overrides an earlier one, per value. An exact name beats the base name.</summary>
        public static OreData Resolve(string oreName, IEnumerable<OrePack> packs)
        {
            var result = new OreData();
            var bare = BaseOreName(oreName);
            foreach (var pack in packs)
            {
                var hit = pack.Ores.FirstOrDefault(o => string.Equals(o.Name, oreName, StringComparison.OrdinalIgnoreCase))
                          ?? pack.Ores.FirstOrDefault(o => string.Equals(o.Name, bare, StringComparison.OrdinalIgnoreCase));
                if (hit == null) continue;
                if (hit.Abundance.HasValue) result.Abundance = hit.Abundance;
                if (hit.Rocks != null) result.Rocks = hit.Rocks;
                if (hit.Glob.HasValue) { result.Glob = hit.Glob; result.GlobSize = hit.GlobSize; result.GlobCoverage = hit.GlobCoverage; }
            }
            return result;
        }
    }
}
