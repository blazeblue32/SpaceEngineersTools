using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// Small persisted switches (~/.config/OreRedistLinux/settings.xml).
    /// The "Legacy" switches bring back behavior of the original Windows tool that is almost
    /// certainly a bug, in case ore setups were tuned with it in mind. Both default to off (fixed).
    /// </summary>
    public static class AppSettings
    {
        static string FilePath => Path.Combine(PathHelper.UserConfigDirectory, "settings.xml");
        static bool _loaded;
        static bool _legacyDepthWeighting;
        static bool _legacyVeryDeepRange;
        static bool _openFolderWhenFinished;
        static readonly List<string> _recent = new();
        static string _lastOutputFolder;

        public const int MaxRecent = 10;

        /// <summary>Planet definition files opened lately, newest first.</summary>
        public static IReadOnlyList<string> RecentFiles { get { Load(); return _recent.ToList(); } }

        public static void AddRecent(string file)
        {
            if (string.IsNullOrEmpty(file)) return;
            Load();
            _recent.RemoveAll(f => string.Equals(f, file, StringComparison.Ordinal));
            _recent.Insert(0, file);
            if (_recent.Count > MaxRecent) _recent.RemoveRange(MaxRecent, _recent.Count - MaxRecent);
            Save();
        }

        /// <summary>Folder of the last "Redistribute Ore" output, the folder picker starts there.</summary>
        public static string LastOutputFolder
        {
            get { Load(); return _lastOutputFolder; }
            set { Load(); _lastOutputFolder = value; Save(); }
        }

        /// <summary>
        /// Original: the cumulative weight in the Preferred Depth pick was added twice per step
        /// (<c>weightSum += weightSum += w</c>), which skews picks toward shallow layers.
        /// </summary>
        public static bool LegacyDepthWeighting
        {
            get { Load(); return _legacyDepthWeighting; }
            set { Load(); _legacyDepthWeighting = value; Save(); }
        }

        /// <summary>
        /// Original: random depth without Preferred Depth used <c>VeryDeepOre ? 9 : 10</c> as exclusive
        /// upper bound (backwards): depth 9 never spawned when enabled, and could be picked when disabled.
        /// </summary>
        public static bool LegacyVeryDeepRange
        {
            get { Load(); return _legacyVeryDeepRange; }
            set { Load(); _legacyVeryDeepRange = value; Save(); }
        }

        /// <summary>Open the output folder in the file manager after "Redistribute Ore" (the original opened Explorer). Off by default.</summary>
        public static bool OpenFolderWhenFinished
        {
            get { Load(); return _openFolderWhenFinished; }
            set { Load(); _openFolderWhenFinished = value; Save(); }
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                var root = XDocument.Load(FilePath).Root;
                bool.TryParse(root?.Element("LegacyDepthWeighting")?.Value, out _legacyDepthWeighting);
                bool.TryParse(root?.Element("LegacyVeryDeepRange")?.Value, out _legacyVeryDeepRange);
                bool.TryParse(root?.Element("OpenFolderWhenFinished")?.Value, out _openFolderWhenFinished);
                _lastOutputFolder = root?.Element("LastOutputFolder")?.Value;
                foreach (var f in root?.Element("Recent")?.Elements("File") ?? Enumerable.Empty<XElement>())
                    if (!string.IsNullOrWhiteSpace(f.Value) && _recent.Count < MaxRecent) _recent.Add(f.Value);
            }
            catch { }
        }

        static void Save()
        {
            try
            {
                new XDocument(new XElement("Settings",
                    new XElement("LegacyDepthWeighting", _legacyDepthWeighting),
                    new XElement("LegacyVeryDeepRange", _legacyVeryDeepRange),
                    new XElement("OpenFolderWhenFinished", _openFolderWhenFinished),
                    new XElement("LastOutputFolder", _lastOutputFolder ?? ""),
                    new XElement("Recent", _recent.Select(f => new XElement("File", f))))).Save(FilePath);
            }
            catch { }
        }
    }
}
