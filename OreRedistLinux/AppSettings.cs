using System;
using System.IO;
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
                    new XElement("OpenFolderWhenFinished", _openFolderWhenFinished))).Save(FilePath);
            }
            catch { }
        }
    }
}
