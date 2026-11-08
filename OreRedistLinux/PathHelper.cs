using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    public static class PathHelper
    {
        /// <summary>
        /// Linux file systems are case sensitive, Windows mods often are not consistent
        /// ("PlanetDataFiles" vs "Planetdatafiles", "Front_mat.png" vs "front_mat.png").
        /// Returns the path with each segment matched case-insensitively if the exact path does not exist.
        /// Returns the input unchanged if nothing matches.
        /// </summary>
        public static string ResolveCase(string path)
        {
            if (string.IsNullOrEmpty(path) || File.Exists(path) || Directory.Exists(path)) return path;
            try
            {
                var full = Path.GetFullPath(path);
                var root = Path.GetPathRoot(full);
                var current = root;
                var segments = full.Substring(root.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    var candidate = Path.Combine(current, seg);
                    if (File.Exists(candidate) || Directory.Exists(candidate))
                    {
                        current = candidate;
                        continue;
                    }
                    if (!Directory.Exists(current)) return path;
                    var match = Directory.EnumerateFileSystemEntries(current)
                        .FirstOrDefault(e => string.Equals(Path.GetFileName(e), seg, StringComparison.OrdinalIgnoreCase));
                    if (match == null) return path;
                    current = match;
                }
                return current;
            }
            catch
            {
                return path;
            }
        }

        /// <summary>Writable per-user folder for presets (~/.config/OreRedistLinux).</summary>
        public static string UserConfigDirectory
        {
            get
            {
                var baseDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(baseDir))
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                var dir = Path.Combine(baseDir, "OreRedistLinux");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        /// <summary>
        /// Path of the user's preset file. Seeded from the shipped OreTierPresets.xml on first use,
        /// so the install folder can be read-only.
        /// </summary>
        public static string PresetFile
        {
            get
            {
                var userFile = Path.Combine(UserConfigDirectory, "OreTierPresets.xml");
                try
                {
                    var shipped = Path.Combine(AppContext.BaseDirectory, "OreTierPresets.xml");
                    if (!File.Exists(userFile) && File.Exists(shipped))
                        File.Copy(shipped, userFile);
                }
                catch { }
                return userFile;
            }
        }

        /// <summary>Opens a folder (or file's folder) in the default file manager. Never throws.</summary>
        public static void RevealInFileManager(string fileOrFolder)
        {
            try
            {
                var dir = Directory.Exists(fileOrFolder) ? fileOrFolder : Path.GetDirectoryName(fileOrFolder);
                Process.Start(new ProcessStartInfo("xdg-open", $"\"{dir}\"") { UseShellExecute = false });
            }
            catch { /* no xdg-open: nothing to do */ }
        }
    }
}
