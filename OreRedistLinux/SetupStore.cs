using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>One saved ore setup on disk (the last run or an autosave).</summary>
    public class SavedSetup
    {
        public string Path;
        public bool IsLastRun;
        public DateTime Saved;
        public int Ores;

        public string Title =>
            (IsLastRun ? "Last run" : "Autosave") + " - " + Saved.ToString("d MMM HH:mm", CultureInfo.CurrentCulture) + $" ({Ores} ore{(Ores == 1 ? "" : "s")})";
    }

    /// <summary>
    /// Autosaves of the redistribution setup (~/.config/OreRedistLinux/Setups/&lt;planet&gt;/): the setup of the last run
    /// (written when you confirm) plus the newest few autosaves (every few minutes while the setup window is open and
    /// when it is closed without confirming). They are never loaded automatically, the setup window offers them in "Load saved".
    /// Same file format as Export / Import.
    /// </summary>
    internal static class SetupStore
    {
        public const int MaxAutosaves = 5;
        const string LastRunFile = "lastrun.xml";

        static string Dir(string planet)
        {
            var bad = Path.GetInvalidFileNameChars();
            var name = string.Concat((string.IsNullOrWhiteSpace(planet) ? "_planet" : planet).Select(c => bad.Contains(c) ? '_' : c));
            var dir = Path.Combine(PathHelper.UserConfigDirectory, "Setups", name);
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        public static void SaveLastRun(string planet, XDocument doc)
        {
            try { doc.Save(Path.Combine(Dir(planet), LastRunFile)); } catch { }
        }

        /// <summary>Writes a new autosave and deletes the oldest ones beyond the limit.</summary>
        public static void SaveAutosave(string planet, XDocument doc)
        {
            try
            {
                var dir = Dir(planet);
                var file = Path.Combine(dir, $"autosave_{DateTime.Now:yyyyMMdd_HHmmss_fff}.xml");
                doc.Save(file);
                var old = Directory.GetFiles(dir, "autosave_*.xml").OrderByDescending(f => f, StringComparer.Ordinal).Skip(MaxAutosaves);
                foreach (var f in old) { try { File.Delete(f); } catch { } }
            }
            catch { }
        }

        /// <summary>Last run first, then autosaves, newest first.</summary>
        public static List<SavedSetup> List(string planet)
        {
            var result = new List<SavedSetup>();
            try
            {
                var dir = Dir(planet);
                var files = new List<string>();
                var last = Path.Combine(dir, LastRunFile);
                if (File.Exists(last)) files.Add(last);
                files.AddRange(Directory.GetFiles(dir, "autosave_*.xml").OrderByDescending(f => f, StringComparer.Ordinal));
                foreach (var f in files)
                {
                    var root = XDocument.Load(f).Root;
                    if (root == null) continue;
                    DateTime saved = DateTime.TryParse(root.Attribute("Saved")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : File.GetLastWriteTime(f);
                    result.Add(new SavedSetup { Path = f, IsLastRun = f == last, Saved = saved, Ores = root.Elements("OreInfo").Count() });
                }
            }
            catch { }
            return result;
        }
    }
}
