using SpaceEngineersToolsShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Where an ore may be placed relative to the water of the Water Mod.</summary>
    /// <remarks>The numbers are kept stable for older files (they are saved by name anyway).</remarks>
    public enum OreWaterMode
    {
        /// <summary>Land only: the whole ore layer has to be above the water line (no need to dig into water).</summary>
        AboveWater = 0,
        /// <summary>Ignore the water.</summary>
        Anywhere = 1,
        /// <summary>Only where the terrain is below the water line (sea floor, lake bed).</summary>
        UnderwaterOnly = 2,
        /// <summary>Both rules: on land the layer stays dry, under standing water anything goes.</summary>
        LandAndSeabed = 3,
    }

    public static class OreWaterModes
    {
        /// <summary>Order shown in the combo boxes.</summary>
        public static readonly OreWaterMode[] Order =
            { OreWaterMode.AboveWater, OreWaterMode.LandAndSeabed, OreWaterMode.UnderwaterOnly, OreWaterMode.Anywhere };

        public static readonly string[] Names =
            { "Land only (dry)", "Land + seabed", "Seabed only", "Anywhere (ignore water)" };

        public static string NameOf(OreWaterMode m) => Names[Math.Max(0, Array.IndexOf(Order, m))];
    }

    /// <summary>
    /// Water Mod rules for one planet.
    ///
    /// Facts this is based on (Water Mod scripts and the game's CreatePlanet code):
    ///   water radius      = MinimumRadius * Radius              (Radius is the WaterConfig value)
    ///   MinimumRadius     = R * (1 + HillParams.Min)            (R = average planet radius)
    ///   terrain radius    = R * (1 + Min + (Max - Min) * h)     (h = height map 0..1)
    ///   ore Start/Depth   = meters below the local surface, they do NOT scale with the planet
    /// So the height of the terrain above the water at a pixel is
    ///   R * (1 + Min + (Max - Min) * h) - R * (1 + Min) * Radius
    /// and an ore layer is dry if Start + Depth fits into that height minus a safety margin
    /// (tide + waves by default).
    /// </summary>
    public class WaterSettings : PropChangeNotifier
    {
        public const double DefaultPlanetRadius = 60000; // EarthLike: 120 km diameter

        public string PlanetName { get; set; }

        bool _enabled;
        /// <summary>Apply the water rules when redistributing ore.</summary>
        public bool Enabled { get => _enabled; set { if (SetProp(ref _enabled, value)) Changed(); } }

        double _radius = 1.0;
        /// <summary>Water radius as multiplier of the planet's minimum radius (WaterConfig Radius).</summary>
        public double Radius { get => _radius; set { if (SetProp(ref _radius, Math.Max(0, value))) Changed(); } }

        double _planetRadius = DefaultPlanetRadius;
        /// <summary>Average planet radius in meters. Not part of any definition file, chosen when the planet is spawned.</summary>
        public double PlanetRadius { get => _planetRadius; set { if (SetProp(ref _planetRadius, Math.Max(1, value))) Changed(); } }

        double _tideHeight;
        public double TideHeight { get => _tideHeight; set { if (SetProp(ref _tideHeight, Math.Max(0, value))) Changed(); } }

        double _waveHeight;
        public double WaveHeight { get => _waveHeight; set { if (SetProp(ref _waveHeight, Math.Max(0, value))) Changed(); } }

        double? _marginOverride;
        /// <summary>Own safety margin in meters, null = tide + wave height.</summary>
        public double? MarginOverride { get => _marginOverride; set { if (SetProp(ref _marginOverride, value)) Changed(); } }

        public double Margin => _marginOverride ?? (_tideHeight + _waveHeight);

        double _hillMin;
        public double HillMin { get => _hillMin; set { if (SetProp(ref _hillMin, value)) Changed(); } }

        double _hillMax;
        public double HillMax { get => _hillMax; set { if (SetProp(ref _hillMax, value)) Changed(); } }

        double _landPreference = 1.5;
        /// <summary>
        /// "By area" balance for Land + seabed ores: land pixels count this many times in the dice
        /// (1 = purely by area, 1.5 = slight preference for land, 0.5 = slight preference for the seabed).
        /// Land that is full or too low for the layer still sends the vein to the seabed.
        /// </summary>
        public double LandPreference { get => _landPreference; set { if (SetProp(ref _landPreference, Math.Clamp(value, 0.05, 50))) Changed(); } }

        bool _seabedByArea = true;
        /// <summary>
        /// For ores set to "Land + seabed": true = veins land where the dice put them (share follows the area of land and
        /// sea), false = a fixed share of the veins goes to the seabed (SeabedPercent), the rest to land.
        /// </summary>
        public bool SeabedByArea
        {
            get => _seabedByArea;
            set { if (SetProp(ref _seabedByArea, value)) { OnPropertyChanged(nameof(SeabedFixed)); Changed(); } }
        }
        public bool SeabedFixed { get => !_seabedByArea; set => SeabedByArea = !value; }

        double _seabedPercent = 50;
        public double SeabedPercent { get => _seabedPercent; set { if (SetProp(ref _seabedPercent, Math.Clamp(value, 0, 100))) Changed(); } }

        /// <summary>Wanted share (0..100) of land+seabed veins on the seabed, null = by area.</summary>
        public double? SeabedShare => _seabedByArea ? null : _seabedPercent;

        bool _keepRatios = true;
        /// <summary>
        /// Keep the ore ratios: first work out what the tool would place without water rules (target per ore and layer
        /// and the normal ore density), then top up what the rules took away on ground where it is allowed.
        /// </summary>
        public bool KeepRatios { get => _keepRatios; set { if (SetProp(ref _keepRatios, value)) Changed(); } }

        double _landDensityLimit = 1.25;
        /// <summary>
        /// Most ore allowed on land, as multiple of the normal density (ore pixels per surface pixel the tool
        /// produces without water rules), measured per block of 128 x 128 pixels. 0 = no limit.
        /// In Land + seabed mode veins that do not fit any more go to the seabed instead.
        /// </summary>
        public double LandDensityLimit { get => _landDensityLimit; set { if (SetProp(ref _landDensityLimit, Math.Max(0, value))) Changed(); } }

        bool _allowShallower;
        /// <summary>
        /// Off (default): a layer that does not fit is not used, the ore stays rarer there (reported).
        /// On: use the next shallower layer that fits, which makes shallow layers more common.
        /// </summary>
        public bool AllowShallower { get => _allowShallower; set { if (SetProp(ref _allowShallower, value)) Changed(); } }

        string _source = "";
        /// <summary>Human readable origin of the values, shown in the water window.</summary>
        public string Source { get => _source; set => SetProp(ref _source, value); }

        string _summary = "";
        /// <summary>Result of the last analysis of the height maps.</summary>
        public string Summary { get => _summary; set => SetProp(ref _summary, value); }

        /// <summary>Height range of the terrain in meters.</summary>
        public double ReliefMeters => (_hillMax - _hillMin) * _planetRadius;

        /// <summary>Where the water line is in the height map (0..1), may be outside that range.</summary>
        public double WaterLineFraction => _hillMax > _hillMin ? (1 + _hillMin) * (_radius - 1) / (_hillMax - _hillMin) : double.NaN;

        /// <summary>Hill range is needed to turn height map values into meters.</summary>
        public bool HasUsableHills => _hillMax > _hillMin;

        public double HeightAboveWater(ushort h16)
        {
            double h = h16 / 65535.0;
            double terrain = _planetRadius * (1 + _hillMin + (_hillMax - _hillMin) * h);
            double water = _planetRadius * (1 + _hillMin) * _radius;
            return terrain - water;
        }

        /// <summary>Depth in meters that can be dug before the water margin is reached.</summary>
        public double AllowedDepth(ushort h16) => HeightAboveWater(h16) - Margin;

        public bool IsUnderwater(ushort h16) => HeightAboveWater(h16) < 0;

        /// <summary>Smallest height map value (0..65536) at which AllowedDepth is at least the given depth.</summary>
        public int MinH16ForAllowed(double depth)
        {
            double t = ((depth + Margin) / _planetRadius + (1 + _hillMin) * (_radius - 1)) / (_hillMax - _hillMin);
            if (t <= 0) return 0;
            if (t > 1) return 65536;
            return (int)Math.Ceiling(t * 65535 - 1e-9);
        }

        /// <summary>Is a whole ore layer (Start..Start+Depth below the surface) above the water margin at this height?</summary>
        public bool LayerIsDry(OreMapping layer, ushort h16) => layer.Start + layer.Depth <= AllowedDepth(h16);

        /// <summary>Raised on every change so the map and the saved values follow.</summary>
        public event Action SettingsChanged;
        bool _loading;
        void Changed()
        {
            OnPropertyChanged(nameof(Margin));
            OnPropertyChanged(nameof(ReliefMeters));
            OnPropertyChanged(nameof(WaterLineFraction));
            OnPropertyChanged(nameof(HasUsableHills));
            if (!_loading) SettingsChanged?.Invoke();
        }

        // -------------------------------------------------------------------------------------
        // Loading / saving

        static string UserFile => Path.Combine(PathHelper.UserConfigDirectory, "water.xml");

        /// <summary>
        /// Settings for a planet: WaterConfig.xml next to the definition file (SubtypeId match) wins for the
        /// water values, own values saved earlier for this planet fill the rest.
        /// </summary>
        public static WaterSettings ForPlanet(PlanetDefinition planet, string definitionFile)
        {
            var w = new WaterSettings { PlanetName = planet.Name, _loading = true };
            w.HillMin = planet.HillMin ?? 0;
            w.HillMax = planet.HillMax ?? 0;
            var notes = new System.Collections.Generic.List<string>();
            if (planet.HillMin == null || planet.HillMax == null) notes.Add("no HillParams in the definition");

            // Own saved values
            XElement saved = null;
            try
            {
                if (File.Exists(UserFile))
                    saved = XDocument.Load(UserFile).Root?.Elements("Planet").FirstOrDefault(e => e.Attribute("Name")?.Value == planet.Name);
            }
            catch { }
            double D(XElement e, string a, double def) =>
                double.TryParse(e?.Attribute(a)?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
            if (saved != null)
            {
                w._planetRadius = D(saved, "PlanetRadius", DefaultPlanetRadius);
                w._seabedByArea = saved.Attribute("SeabedByArea")?.Value != "false";
                w._seabedPercent = D(saved, "SeabedPercent", 50);
                w._landPreference = D(saved, "LandPreference", 1.5);
                w._keepRatios = saved.Attribute("KeepRatios")?.Value != "false";
                w._landDensityLimit = D(saved, "LandDensityLimit", 1.25);
                w._allowShallower = saved.Attribute("AllowShallower")?.Value == "true";
                if (saved.Attribute("Margin") != null) w._marginOverride = D(saved, "Margin", 0);
                if (planet.HillMin == null && saved.Attribute("HillMin") != null) w._hillMin = D(saved, "HillMin", 0);
                if (planet.HillMax == null && saved.Attribute("HillMax") != null) w._hillMax = D(saved, "HillMax", 0);
                w.Source = "own values saved for this planet";
            }

            // WaterConfig.xml of the mod
            var cfg = FindConfig(planet.Name, definitionFile, out var cfgProblem);
            if (cfg != null)
            {
                // WaterConfig.xml stores the values as child elements: <Radius>1.025</Radius>
                double E(string name, double def) =>
                    double.TryParse(cfg.Element(name)?.Value?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
                w._enabled = true;
                w._radius = E("Radius", 1.0);
                w._waveHeight = E("WaveHeight", 0);
                w._tideHeight = E("TideHeight", 0);
                w.Source = "WaterConfig.xml next to the planet definition" + (saved != null ? " (plus your saved planet radius)" : "");
            }
            else if (saved != null)
            {
                w._enabled = saved.Attribute("Enabled")?.Value == "true";
                w._radius = D(saved, "Radius", 1.0);
                w._waveHeight = D(saved, "WaveHeight", 0);
                w._tideHeight = D(saved, "TideHeight", 0);
            }
            else
            {
                w.Source = cfgProblem != null ? cfgProblem + ". Set the values yourself." : "no WaterConfig.xml found for this planet, set the values yourself";
            }
            if (notes.Count > 0) w.Source += " (" + string.Join(", ", notes) + ")";
            w._loading = false;
            return w;
        }

        /// <summary>
        /// Looks for WaterConfig.xml next to the planet definition. Returns the WaterSettings element of the planet,
        /// or null with the reason in <paramref name="problem"/> (null if there simply is no file).
        /// </summary>
        static XElement FindConfig(string planetName, string definitionFile, out string problem)
        {
            problem = null;
            try
            {
                var dir = Path.GetDirectoryName(definitionFile);
                if (string.IsNullOrEmpty(dir)) return null;
                var file = PathHelper.ResolveCase(Path.Combine(dir, "WaterConfig.xml"));
                if (!File.Exists(file)) return null;
                var root = PathHelper.LoadXmlTolerant(file).Root;
                var configs = root?.Element("PlanetConfigs")?.Elements("PlanetConfig").ToList() ?? new List<XElement>();
                var cfg = configs.FirstOrDefault(e => string.Equals(e.Attribute("SubtypeId")?.Value, planetName, StringComparison.OrdinalIgnoreCase));
                if (cfg == null)
                {
                    var names = configs.Select(e => e.Attribute("SubtypeId")?.Value).Where(n => !string.IsNullOrEmpty(n)).ToList();
                    problem = $"WaterConfig.xml found, but it has no entry for \"{planetName}\"" + (names.Count > 0 ? " (it has: " + string.Join(", ", names) + ")" : "");
                    return null;
                }
                var ws = cfg.Element("WaterSettings");
                if (ws == null) problem = $"WaterConfig.xml has an entry for \"{planetName}\" without WaterSettings";
                return ws;
            }
            catch (Exception ex)
            {
                problem = "WaterConfig.xml found but could not be read: " + ex.Message;
                return null;
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(PlanetName)) return;
            try
            {
                XDocument doc = null;
                try { if (File.Exists(UserFile)) doc = XDocument.Load(UserFile); } catch { }
                doc ??= new XDocument(new XElement("Water"));
                var inv = CultureInfo.InvariantCulture;
                doc.Root.Elements("Planet").Where(e => e.Attribute("Name")?.Value == PlanetName).Remove();
                var el = new XElement("Planet",
                    new XAttribute("Name", PlanetName),
                    new XAttribute("Enabled", Enabled ? "true" : "false"),
                    new XAttribute("Radius", Radius.ToString(inv)),
                    new XAttribute("PlanetRadius", PlanetRadius.ToString(inv)),
                    new XAttribute("TideHeight", TideHeight.ToString(inv)),
                    new XAttribute("WaveHeight", WaveHeight.ToString(inv)),
                    new XAttribute("HillMin", HillMin.ToString(inv)),
                    new XAttribute("HillMax", HillMax.ToString(inv)),
                    new XAttribute("SeabedByArea", SeabedByArea ? "true" : "false"),
                    new XAttribute("SeabedPercent", SeabedPercent.ToString(inv)),
                    new XAttribute("LandPreference", LandPreference.ToString(inv)),
                    new XAttribute("KeepRatios", KeepRatios ? "true" : "false"),
                    new XAttribute("LandDensityLimit", LandDensityLimit.ToString(inv)),
                    new XAttribute("AllowShallower", AllowShallower ? "true" : "false"));
                if (MarginOverride.HasValue) el.Add(new XAttribute("Margin", MarginOverride.Value.ToString(inv)));
                doc.Root.Add(el);
                doc.Save(UserFile);
            }
            catch { }
        }

        /// <summary>Short description for dialogs and reports.</summary>
        public string Describe() =>
            $"water radius {Radius:0.###} x minimum radius, planet radius {PlanetRadius:0} m, hills {HillMin:0.###}..{HillMax:0.###}, " +
            $"relief {ReliefMeters:0} m, safety margin {Margin:0.#} m, water line at {WaterLineFraction * 100:0.#}% of the height range";
    }
}
