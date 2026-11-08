using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SpaceEngineersToolsShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

// The RED channel is the voxel material placement map. -> 82=Lake 43=Desert
// The GREEN channel is the foliage placement map (including voxel stones) -> Biome value, Material based on rules
// The BLUE channel is the ore placement map

namespace SpaceEngineersOreRedistribution
{
    internal class MainWindowViewModel : PropChangeNotifier
    {
        public List<int> TileWidths { get; } = new() { 512, 1024, 2048, 4096, 8192 };
        int _tileWidth = 2048;
        public int TileWidth
        {
            get => _tileWidth;
            set
            {
                if (value == _tileWidth) return;

                if (value > 2048)
                {
                    MessageBox.Show("Warning using tile widths above 2048 causes lighting glitches and bad server performance in SE1!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (PlanetDefinitions.Count > 0)
                {
                    var dlgRes = MessageBox.Show(
                        "Changing the tile width will unload all planet definitions. Continue?"
                        , "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (dlgRes != MessageBoxResult.OK)
                    {
                        // Reset to previous value
                        Dispatcher.UIThread.InvokeAsync(()=>{ OnPropertyChanged(); });
                        return;
                    }
                    SelectedPlanetDefinition = null;
                    PlanetDefinitions.Clear();
                    TileUp = null;
                    TileFront = null;
                    TileRight = null;
                    TileBack = null;
                    TileLeft = null;
                    TileDown = null;
                }
                _tileWidth = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PlanetDefinition> PlanetDefinitions { get; } = new();

        public ObservableCollection<OreDistributionStatViewModel> OreTypes { get; } = new();

        CancellationTokenSource _imageUpdateTcs;
        bool _updatingImages = false;

        bool _progressBarVisibility = false;
        public bool ProgressBarVisibility
        {
            get => _progressBarVisibility;
            set => SetProp(ref _progressBarVisibility, value);
        }

        bool _showDistByArea = true;
        public bool ShowOreDistByArea
        {
            get => _showDistByArea;
            set
            {
                if (SetProp(ref _showDistByArea, value))
                {
                    ShowDistByVolume = !value;
                    foreach (var item in OreTypes)
                        item.ShowByArea = value;
                    var list = OreTypes.ToList();
                    list.Sort((a, b) => b.Percentage.CompareTo(a.Percentage));
                    //if (list.Count > 0)
                    //{
                    //    var maxP = list.Max(x => x.Percentage);
                    //    foreach (var x in list)
                    //        x.ScaledPercentage = (int)(0.5 + x.Percentage * 100 / maxP);
                    //}
                    OreTypes.Clear();
                    foreach (var item in list)
                    {
                        OreTypes.Add(item);
                    }
                }
            }
        }

        bool _showDistByVolume;
        public bool ShowDistByVolume
        {
            get => _showDistByVolume;
            set
            {
                if (SetProp(ref _showDistByVolume, value))
                {
                    ShowOreDistByArea = !value;
                }
            }
        }

        PlanetDefinition _selectedPlanetDefinition;
        public PlanetDefinition SelectedPlanetDefinition
        {
            get => _selectedPlanetDefinition;
            set
            {
                if (!SetProp(ref _selectedPlanetDefinition, value)) return;
                RelayCommand.RequestRequery(); // Redistribute etc. depend on a selected planet

                if (_imageUpdateTcs != null)
                {
                    _imageUpdateTcs.Cancel();
                    _imageUpdateTcs.Dispose();
                    _imageUpdateTcs = null;
                }

                ClearImages();
                OreTypes.Clear();
                EnvironmentItems.Clear();
                ComplexMaterials.Clear();
                if (value == null)
                {
                    _oreTypeOrder = new();
                    Water = null;
                    return;
                }
                Water = WaterSettings.ForPlanet(value, _lastOpenedFile);
                // Order of first appearance in the definition: keeps each ore's default color stable
                _oreTypeOrder = value.OreMappings.Select(m => m.Type).Distinct().ToList();
                //HashSet<string> types = new();
                //foreach (var item in value.OreMappings)
                //{
                //    types.Add(item.Type);
                //}
                //foreach (var type in types)
                //    OreTypes.Add(type);
                foreach (var item in value.ComplexMaterials)
                {
                    ComplexMaterials.Add(item);
                }
                foreach (var item in value.EnvironmentItems)
                {
                    EnvironmentItems.Add(item);
                }

                _imageUpdateTcs = new();
                var token = _imageUpdateTcs.Token;
                _updatingImages = true;


                Task.Run(() =>
                {
                    try
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            ProgressBarVisibility = true;
                        });
                        Parallel.ForEach(Enum.GetValues(typeof(CubeMapFace)).Cast<CubeMapFace>(), f =>
                        {
                            if (token.IsCancellationRequested) return;
                            var info = GetInfo(f, token);
                            // Dictionary is not thread safe. Unlocked parallel writes could drop a cube face.
                            lock (_images) { _images[f] = info; }
                        });
                        if (token.IsCancellationRequested) return;

                        // Update ore
                        Dictionary<string, int> oreArea = new(); // Counts ore area
                        Dictionary<string, int> oreVolume = new(); // Counts ore volume
                        Parallel.ForEach(_images.Values, img =>
                        {
                            if (img == null) return;
                            for (int x = 0; x < TileWidth; ++x)
                                for (int y = 0; y < TileWidth; ++y)
                                {
                                    var blue = img.B[x, y];
                                    if (blue == 255) continue; // No ore here
                                    var mapping = value.OreMappings.FirstOrDefault(x => x.Value == blue);
                                    if (mapping != null)
                                    {
                                        lock (oreArea)
                                        {
                                            if (!oreArea.ContainsKey(mapping.Type))
                                            {
                                                oreArea[mapping.Type] = 1;
                                                oreVolume[mapping.Type] = mapping.Depth;
                                            }
                                            else
                                            {
                                                oreArea[mapping.Type] += 1;
                                                oreVolume[mapping.Type] += mapping.Depth;
                                            }
                                        }
                                    }
                                }
                        });
                        int oreSumArea = oreArea.Values.Sum();
                        int oreSumVolume = oreVolume.Values.Sum();
                        List<OreDistributionStatViewModel> list = new();
                        foreach (var key in oreArea.Keys)
                        {
                            var stat = new OreDistributionStatViewModel(
                                key,
                                oreArea[key] * 100.0 / oreSumArea,
                                oreVolume[key] * 100.0 / oreSumVolume, oreArea[key], oreVolume[key])
                            { ShowByArea = ShowOreDistByArea };
                            stat.InitColor(DefaultOreColor(key), OrePalette.TryGetCustom(key, out var custom) ? custom : null);
                            stat.ColorChanged += OreColorChanged;
                            list.Add(stat);
                        }
                        list.Sort((a, b) => b.Percentage.CompareTo(a.Percentage));
                        if (list.Count > 0)
                        {
                            var maxPArea = list.Max(x => x.PercentageArea);
                            var maxPVolume = list.Max(x => x.PercentageVolume);
                            foreach (var x in list)
                            {
                                x.ScaledPercentageArea = (int)(0.5 + x.PercentageArea * 100 / maxPArea);
                                x.ScaledPercentageVolume = (int)(0.5 + x.PercentageVolume * 100 / maxPVolume);
                            }
                        }
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            foreach (var item in list)
                            {
                                item.ShowByArea = ShowOreDistByArea; // Update scaled percentages
                                OreTypes.Add(item);
                            }
                        });

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            _updatingImages = false;
                            AnalyzeWater();
                            UpdateImages();
                            UpdateOverlays(true);
                        });
                    }
                    finally
                    {
                        ProgressBarVisibility = false;
                    }
                });


            }
        }

        List<Avalonia.Media.Color> _colors = new List<Avalonia.Media.Color>
        {
            Avalonia.Media.Colors.LawnGreen,
            Avalonia.Media.Colors.DeepSkyBlue,
            Avalonia.Media.Colors.Red,
            Avalonia.Media.Colors.Orange,
            Avalonia.Media.Colors.BlueViolet,
            Avalonia.Media.Colors.Magenta,
            Avalonia.Media.Colors.Green,
            Avalonia.Media.Colors.Gold,
            Avalonia.Media.Colors.MediumPurple,
            Avalonia.Media.Colors.Blue,
            Avalonia.Media.Colors.Firebrick,
            Avalonia.Media.Colors.Goldenrod,
            Avalonia.Media.Colors.MediumAquamarine,
            Avalonia.Media.Colors.MediumVioletRed,
            Avalonia.Media.Colors.OliveDrab,
        };
        Avalonia.Media.IBrush _defaultOreBrush = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Avalonia.Media.Color.FromRgb(0, 0xff, 0));
        OreDistributionStatViewModel _selectedOreType;
        public OreDistributionStatViewModel SelectedOreType
        {
            get => _selectedOreType;
            set
            {
                if (!SetProp(ref _selectedOreType, value)) return;
                OreMappings.Clear();
                if (value == null || SelectedPlanetDefinition == null)
                {
                    if (ShowOreLocations && !_updatingImages)
                        UpdateOverlays(false);
                    return;
                }

                // Reset colors so they don't appear in ore inspector
                foreach (var m in SelectedPlanetDefinition.OreMappings)
                {
                    m.MapRgbValue = new(0, 0, 64);
                }

                var mappings = SelectedPlanetDefinition.OreMappings.Where(x => x.Type == value.OreType);
                int colorIndex = 0;
                foreach (var mapping in mappings)
                {
                    if (colorIndex < _colors.Count)
                    {
                        var color = _colors[colorIndex++];
                        mapping.MapRgbValue = new(color.R, color.G, color.B);
                    }
                    else
                        mapping.MapRgbValue = new(0, 0, 255);
                    OreMappings.Add(mapping);
                }
                if (ShowOreLocations && !_updatingImages)
                    UpdateOverlays(false);
            }
        }

        int _oreDetectorRange = 150;
        public int OreDetectorRange
        {
            get => _oreDetectorRange;
            set
            {
                if (SetProp(ref _oreDetectorRange, value))
                    UpdateOverlays(false);
            }
        }
        bool _useOreDetectorRange = true;
        public bool UseOreDetectorRange
        {
            get => _useOreDetectorRange;
            set
            {
                if (SetProp(ref _useOreDetectorRange, value))
                    UpdateOverlays(false);
            }
        }

        public ObservableCollection<OreMapping> OreMappings { get; } = new();
        OreMapping _selectedOreMapping;
        public OreMapping SelectedOreMapping
        {
            get => _selectedOreMapping;
            set => SetProp(ref _selectedOreMapping, value);
        }
        bool _ignoreOreMappingsForRedestribution = true;
        public bool IgnoreOreMappingsForRedestribution
        {
            get => _ignoreOreMappingsForRedestribution;
            set => SetProp(ref _ignoreOreMappingsForRedestribution, value);
        }

        public ObservableCollection<ComplexMaterial> ComplexMaterials { get; } = new();
        ComplexMaterial _selectedComplexMaterial;
        int _lastComplexMaterial;
        public ComplexMaterial SelectedComplexMaterial
        {
            get => _selectedComplexMaterial;
            set
            {
                SetProp(ref _selectedComplexMaterial, value);
                var number = value?.Value ?? 0;
                if (ShowLakes && !_updatingImages && number != _lastComplexMaterial)
                    UpdateImages();
                _lastComplexMaterial = number;
            }
        }

        public ObservableCollection<EnvironmentItem> EnvironmentItems { get; } = new();
        int _lastBiome;
        EnvironmentItem _selectedEnvironmentItem;
        public EnvironmentItem SelectedEnvironmentItem
        {
            get => _selectedEnvironmentItem;
            set
            {
                SetProp(ref _selectedEnvironmentItem, value);
                var biome = value?.Biome ?? 0;
                if (ShowBiomes && !_updatingImages && biome != _lastBiome)
                    UpdateImages();
                _lastBiome = biome;
            }
        }

        bool _showOreLocations = true;
        public bool ShowOreLocations
        {
            get => _showOreLocations;
            set
            {
                if (!SetProp(ref _showOreLocations, value)) return;
                if (!_updatingImages)
                    UpdateOverlays(true);
            }
        }

        bool _showGradients;
        public bool ShowGradients
        {
            get => _showGradients;
            set
            {
                if (!SetProp(ref _showGradients, value)) return;
                if (!_updatingImages)
                    UpdateImages();
            }
        }

        bool _showLakes = true;
        public bool ShowLakes
        {
            get => _showLakes;
            set
            {
                if (!SetProp(ref _showLakes, value)) return;
                if (!_updatingImages)
                    UpdateImages();
            }
        }

        /// <summary>One line about a map pixel for the status bar: height, water, ore, biome, material.</summary>
        public string DescribePixel(CubeMapFace face, int x, int y)
        {
            ImageData img;
            lock (_images) { if (!_images.TryGetValue(face, out img) || img == null || img.B == null) return ""; }
            if (x < 0 || y < 0 || x >= img.TileWidth || y >= img.TileWidth) return "";
            var parts = new List<string> { $"{face}  x {x}  y {y}" };

            if (img.HasHeightMap)
            {
                var h = img.H16[x, y];
                string hs = $"height {h / 655.35:0.#}%";
                var w = Water;
                if (w != null && w.HasUsableHills)
                {
                    hs += $" = {w.PlanetRadius * (w.HillMax - w.HillMin) * h / 65535.0:0} m above the lowest terrain";
                    if (w.Enabled)
                    {
                        double above = w.HeightAboveWater(h);
                        hs += above >= 0 ? $", {above:0} m above the water" : $", {-above:0} m under water";
                    }
                }
                parts.Add(hs);
            }

            byte b = img.B[x, y];
            var mapping = SelectedPlanetDefinition?.OreMappings.FirstOrDefault(m => m.Value == b);
            parts.Add(mapping != null ? $"ore {mapping.Type}, {mapping.Start}-{mapping.Start + mapping.Depth} m deep (value {b})" : "no ore");

            byte g = img.G[x, y];
            var env = SelectedPlanetDefinition?.EnvironmentItems.FirstOrDefault(e => e.Biome == g);
            parts.Add(env != null ? $"biome {g} ({env.Material?.Name})" : $"biome {g}");
            byte r = img.R[x, y];
            var cm = SelectedPlanetDefinition?.ComplexMaterials.FirstOrDefault(c => c.Value == r);
            if (cm != null && r != 0) parts.Add($"material {r} ({cm.Name})");
            return string.Join("   |   ", parts);
        }

        // ------------------------------------------------------------------ water (Water Mod)
        WaterSettings _water;
        /// <summary>Water rules of the selected planet, null while no planet is selected.</summary>
        public WaterSettings Water
        {
            get => _water;
            private set
            {
                if (_water != null) _water.SettingsChanged -= WaterChanged;
                if (!SetProp(ref _water, value)) return;
                if (_water != null) _water.SettingsChanged += WaterChanged;
            }
        }

        bool _showWater;
        public bool ShowWater
        {
            get => _showWater;
            set
            {
                if (!SetProp(ref _showWater, value)) return;
                if (!_updatingImages) UpdateImages();
            }
        }

        DispatcherTimer _waterTimer;
        void WaterChanged()
        {
            _water?.Save();
            _waterTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _waterTimer.Tick -= WaterTimerTick;
            _waterTimer.Tick += WaterTimerTick;
            _waterTimer.Stop();
            _waterTimer.Start();
        }

        void WaterTimerTick(object sender, EventArgs e)
        {
            _waterTimer.Stop();
            AnalyzeWater();
            if (ShowWater && !_updatingImages) UpdateImages();
        }

        /// <summary>
        /// Looks at the height maps with the current water values: how much of the planet is underwater
        /// and how deep one can dig on the rest before reaching the water (+ margin).
        /// </summary>
        public void AnalyzeWater()
        {
            var water = _water;
            if (water == null) return;
            List<ImageData> images;
            lock (_images) images = _images.Values.Where(i => i != null && i.H16 != null).ToList();
            if (images.Count == 0) { water.Summary = ""; return; }
            if (!images.All(i => i.HasHeightMap)) { water.Summary = "A height map (front.png, ...) is missing for this planet. Water rules cannot be used."; return; }
            if (!water.HasUsableHills) { water.Summary = "Hill range (HillParams Min/Max) is empty, the height map has no effect. Enter the values the planet uses."; return; }
            int width = TileWidth;
            Task.Run(() =>
            {
                double[] depths = { 10, 25, 50, 100, 200, 400 };
                long under = 0, total = 0;
                long[] fit = new long[depths.Length];
                double best = double.MinValue;
                object gate = new();
                Parallel.ForEach(images, img =>
                {
                    long u = 0, t = 0; long[] f = new long[depths.Length]; double b = double.MinValue;
                    for (int x = 0; x < width; ++x)
                        for (int y = 0; y < width; ++y)
                        {
                            var h = img.H16[x, y];
                            ++t;
                            if (water.IsUnderwater(h)) ++u;
                            var allowed = water.AllowedDepth(h);
                            if (allowed > b) b = allowed;
                            for (int i = 0; i < depths.Length; ++i) if (allowed >= depths[i]) ++f[i];
                        }
                    lock (gate) { under += u; total += t; for (int i = 0; i < f.Length; ++i) fit[i] += f[i]; if (b > best) best = b; }
                });
                string pct(long v) => (100.0 * v / Math.Max(1, total)).ToString("0.#", CultureInfo.InvariantCulture) + "%";
                var text = $"Underwater: {pct(under)} of the surface. Water line at {water.WaterLineFraction * 100:0.#}% of the height range. " +
                           $"Deepest dry layer possible: {Math.Max(0, best):0} m (after {water.Margin:0.#} m margin).\n" +
                           "Surface where a layer ending at this depth stays dry:  " +
                           string.Join("   ", depths.Select((d, i) => $">= {d:0} m: {pct(fit[i])}"));
                Dispatcher.UIThread.Post(() => water.Summary = text);
            });
        }

        public ICommand OpenWaterSettingsCommand => new RelayCommand(o =>
        {
            var win = new WaterSettingsWindow { DataContext = Water };
            win.ShowDialogBlocking();
        }, o => Water != null);

        bool _showBiomes = false;
        public bool ShowBiomes
        {
            get => _showBiomes;
            set
            {
                if (!SetProp(ref _showBiomes, value)) return;
                if (!_updatingImages)
                    UpdateImages();
            }
        }

        public ICommand OpenPlanetDefinitionCommand => new RelayCommand(o =>
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "sbc Files|*.sbc";
            dlg.FileName = "PlanetGeneratorDefinitions.sbc";
            dlg.Multiselect = false;
            var lastFile = AppSettings.RecentFiles.FirstOrDefault();
            if (lastFile != null) dlg.InitialDirectory = Path.GetDirectoryName(lastFile);
            var res = dlg.ShowDialog();
            if (res != true) return;
            OpenPlanetDefinition(dlg.FileName);
        });

        string _lastOpenedFile;

        public void OpenPlanetDefinition(string file)
        {
            _lastOpenedFile = file;
            PlanetDefinitions.Clear();
            TileUp = null;
            TileFront = null;
            TileRight = null;
            TileBack = null;
            TileLeft = null;
            TileDown = null;

            try
            {
                PlanetDefinitions.Clear();

                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                if (!File.Exists(file)) return;
                var doc = XDocument.Load(file);
                var root = doc?.Root;
                if (root?.Name != "Definitions")
                {
                    MessageBox.Show("File does not contain definitions", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var definitions = root.Elements("Definition");
                foreach (var definition in definitions)
                {
                    string xsiTypeValue = definition.Attribute(xsi + "type")?.Value;
                    if (xsiTypeValue != "PlanetGeneratorDefinition") continue;
                    ReadPlanetDefinition(definition);
                }
                // Pertam & Triton use different format
                var defs = root.Elements("PlanetGeneratorDefinitions");
                foreach (var def in defs)
                {
                    var subDefs = def.Elements("PlanetGeneratorDefinition");
                    foreach (var subDef in subDefs)
                    {
                        ReadPlanetDefinition(subDef);
                    }
                }
                if (PlanetDefinitions.Count > 0) AppSettings.AddRecent(file);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        bool ReadPlanetDefinition(XElement definition)
        {
            var id = definition.Element("Id")?.Element("SubtypeId")?.Value;
            if (id == null) return false;
            var def = new PlanetDefinition();
            def.Name = id;
            var hill = definition.Element("HillParams");
            if (hill != null)
            {
                if (double.TryParse(hill.Attribute("Min")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hMin)) def.HillMin = hMin;
                if (double.TryParse(hill.Attribute("Max")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hMax)) def.HillMax = hMax;
            }
            PlanetDefinitions.Add(def);
            var oreMappings = definition.Element("OreMappings");
            if (oreMappings != null)
            {
                foreach (var oreMapping in oreMappings.Elements("Ore"))
                {
                    try
                    {
                        var ore = new OreMapping();
                        ore.Value = int.Parse(oreMapping.Attribute("Value")?.Value);
                        ore.Type = oreMapping.Attribute("Type")?.Value;
                        ore.Start = int.Parse(oreMapping.Attribute("Start")?.Value);
                        ore.Depth = int.Parse(oreMapping.Attribute("Depth")?.Value);
                        // optional:
                        ore.TargetColor = oreMapping.Attribute("TargetColor")?.Value;
                        if (ore.TargetColor != null && !ore.TargetColor.StartsWith("#")) ore.TargetColor = "#" + ore.TargetColor;
                        ore.ColorInfluence = oreMapping.Attribute("ColorInfluence")?.Value;
                        def.OreMappings.Add(ore);
                    }
                    catch { continue; }
                }
            }

            var complexMaterials = definition.Element("ComplexMaterials");
            if (complexMaterials != null)
            {
                foreach ( var complexMaterial in complexMaterials.Elements("MaterialGroup"))
                {
                    var name = complexMaterial.Attribute("Name")?.Value;
                    var value = complexMaterial.Attribute("Value")?.Value;
                    if (int.TryParse(value, out var iValue))
                    {
                        var cm = new ComplexMaterial { Name = name, Value = iValue };
                        def.ComplexMaterials.Add(cm);
                    }
                }
            }

            var environmentItems = definition.Element("EnvironmentItems");
            if (environmentItems != null)
            {
                foreach (var item in environmentItems.Elements("Item"))
                {
                    var env = new EnvironmentItem { Material = new() };
                    var biomes = item.Element("Biomes");
                    if (biomes != null)
                    {
                        var biome = biomes.Element("Biome");
                        if (biome != null)
                        {
                            if (int.TryParse(biome.Value, out var b)) env.Biome = b;
                        }
                    }
                    var materials = item.Element("Materials");
                    if (materials != null)
                    {
                        var material = materials.Element("Material");
                        if (material != null)
                        {
                            env.Material.Name = material.Value;
                            // TODO: Value, MaxDepth are stored in another node
                        }

                    }
                    var rule = item.Element("Rule");
                    if (rule != null)
                    {
                        double min, max;
                        RuleGetMinMax(rule.Element("Height"), out min, out max);
                        env.MinHeight = min; env.MaxHeight = max;
                        RuleGetMinMax(rule.Element("Latitude"), out min, out max);
                        env.MinLatitude = min; env.MaxLatitude = max;
                        RuleGetMinMax(rule.Element("Slope"), out min, out max);
                        env.MinSlope = min; env.MaxSlope = max;
                    }
                    def.EnvironmentItems.Add(env);
                }
            }

            return true;
        }

        void RuleGetMinMax(XElement element, out double min, out double max)
        {
            min = 0;
            max = 0;
            if (element == null) return;
            var eMin = element.Attribute("Min");
            if (eMin != null) double.TryParse(eMin.Value, CultureInfo.InvariantCulture, out min);
            var eMax = element.Attribute("Max");
            if (eMax != null) double.TryParse(eMax.Value, CultureInfo.InvariantCulture, out max);
        }

        Bitmap _tileUp;
        public Bitmap TileUp
        {
            get => _tileUp;
            set => SetProp(ref _tileUp, value);
        }

        Bitmap _tileFront;
        public Bitmap TileFront
        {
            get => _tileFront;
            set => SetProp(ref _tileFront, value);
        }

        Bitmap _tileRight;
        public Bitmap TileRight
        {
            get => _tileRight;
            set => SetProp(ref _tileRight, value);
        }
        Bitmap _tileBack;
        public Bitmap TileBack
        {
            get => _tileBack;
            set => SetProp(ref _tileBack, value);
        }
        Bitmap _tileLeft;
        public Bitmap TileLeft
        {
            get => _tileLeft;
            set => SetProp(ref _tileLeft, value);
        }
        Bitmap _tileDown;
        public Bitmap TileDown
        {
            get => _tileDown;
            set => SetProp(ref _tileDown, value);
        }

        WriteableBitmap _tileUpOverlay;
        public WriteableBitmap TileUpOverlay
        {
            get => _tileUpOverlay;
            set => SetProp(ref _tileUpOverlay, value);
        }
        WriteableBitmap _tileFrontOverlay;
        public WriteableBitmap TileFrontOverlay
        {
            get => _tileFrontOverlay;
            set => SetProp(ref _tileFrontOverlay, value);
        }
        WriteableBitmap _tileRightOverlay;
        public WriteableBitmap TileRightOverlay
        {
            get => _tileRightOverlay;
            set => SetProp(ref _tileRightOverlay, value);
        }
        WriteableBitmap _tileBackOverlay;
        public WriteableBitmap TileBackOverlay
        {
            get => _tileBackOverlay;
            set => SetProp(ref _tileBackOverlay, value);
        }
        WriteableBitmap _tileLeftOverlay;
        public WriteableBitmap TileLeftOverlay
        {
            get => _tileLeftOverlay;
            set => SetProp(ref _tileLeftOverlay, value);
        }
        WriteableBitmap _tileDownOverlay;
        public WriteableBitmap TileDownOverlay
        {
            get => _tileDownOverlay;
            set => SetProp(ref _tileDownOverlay, value);
        }


        int _oreInspectorSize = 20;
        public int OreInspectorSize
        {
            get => _oreInspectorSize;
            set => SetProp(ref _oreInspectorSize, value);
        }

        void ClearImages()
        {
            foreach (var info in _images.Values)
            {
                if (info != null) info.Dispose();
            }
            _images.Clear();
        }

        public bool OpenFolderWhenFinished
        {
            get => AppSettings.OpenFolderWhenFinished;
            set { if (AppSettings.OpenFolderWhenFinished == value) return; AppSettings.OpenFolderWhenFinished = value; OnPropertyChanged(); }
        }

        public bool LegacyDepthWeighting
        {
            get => AppSettings.LegacyDepthWeighting;
            set { if (AppSettings.LegacyDepthWeighting == value) return; AppSettings.LegacyDepthWeighting = value; OnPropertyChanged(); }
        }

        public bool LegacyVeryDeepRange
        {
            get => AppSettings.LegacyVeryDeepRange;
            set { if (AppSettings.LegacyVeryDeepRange == value) return; AppSettings.LegacyVeryDeepRange = value; OnPropertyChanged(); }
        }

        List<string> _oreTypeOrder = new();

        Avalonia.Media.Color DefaultOreColor(string type)
        {
            int index = _oreTypeOrder.IndexOf(type);
            return OrePalette.Default(index < 0 ? 0 : index);
        }

        Avalonia.Media.Color OreColor(string type) =>
            OrePalette.TryGetCustom(type, out var c) ? c : DefaultOreColor(type);

        DispatcherTimer _colorTimer;
        void OreColorChanged(OreDistributionStatViewModel stat)
        {
            // The color picker fires on every drag step: save once it settles, redraw quickly
            OrePalette.SetCustom(stat.OreType, stat.IsCustom ? stat.Color : null);
            _colorTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _colorTimer.Tick -= ColorTimerTick;
            _colorTimer.Tick += ColorTimerTick;
            _colorTimer.Stop();
            _colorTimer.Start();
        }

        void ColorTimerTick(object sender, EventArgs e)
        {
            _colorTimer.Stop();
            if (!_updatingImages && SelectedPlanetDefinition != null && ShowOreLocations)
                UpdateOverlays(false);
        }

        /// <summary>Raised after overlay pixels changed in place; the view has to repaint the overlay images.</summary>
        public event Action OverlaysChanged;

        static WriteableBitmap NewOverlay(int width) =>
            new WriteableBitmap(new PixelSize(width, width), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);

        void UpdateOverlays(bool clear)
        {
            if (clear || TileBackOverlay == null)
            {
                TileBackOverlay = NewOverlay(TileWidth);
                TileDownOverlay = NewOverlay(TileWidth);
                TileFrontOverlay = NewOverlay(TileWidth);
                TileLeftOverlay = NewOverlay(TileWidth);
                TileRightOverlay = NewOverlay(TileWidth);
                TileUpOverlay = NewOverlay(TileWidth);
            }
            if (!ShowOreLocations || SelectedPlanetDefinition == null)
            {
                return;
            }
            var keys = _images.Keys;
            List<OreMapping> oreMappings = null;
            if (SelectedOreType != null && OreMappings != null && OreMappings.Count > 0)
            {
                oreMappings = OreMappings.ToList();
            }
            // value (blue channel) -> mapping / color, so the per pixel work is an array lookup
            var byValue = new OreMapping[256];
            foreach (var m in SelectedPlanetDefinition.OreMappings)
                if (m.Value >= 0 && m.Value < 256 && byValue[m.Value] == null) byValue[m.Value] = m;
            OreMapping[] selectedByValue = null;
            if (oreMappings != null)
            {
                selectedByValue = new OreMapping[256];
                foreach (var m in oreMappings)
                    if (m.Value >= 0 && m.Value < 256 && selectedByValue[m.Value] == null) selectedByValue[m.Value] = m;
            }
            var colorByValue = new Avalonia.Media.Color?[256];
            var typeColors = new Dictionary<string, Avalonia.Media.Color>();
            for (int i = 0; i < 256; ++i)
            {
                if (byValue[i] == null) continue;
                var type = byValue[i].Type;
                if (!typeColors.TryGetValue(type, out var tc)) typeColors[type] = tc = OreColor(type);
                colorByValue[i] = tc;
            }
            foreach (var key in keys)
            {
                if (!_images.TryGetValue(key, out var value)) continue;
                if (value == null) continue;
                WriteableBitmap bitmap = null;
                switch (key)
                {
                    case CubeMapFace.Back: bitmap = TileBackOverlay; break;
                    case CubeMapFace.Down: bitmap = TileDownOverlay; break;
                    case CubeMapFace.Front: bitmap = TileFrontOverlay; break;
                    case CubeMapFace.Left: bitmap = TileLeftOverlay; break;
                    case CubeMapFace.Right: bitmap = TileRightOverlay; break;
                    case CubeMapFace.Up: bitmap = TileUpOverlay; break;
                }
                if (bitmap == null) continue;
                try
                {
                    using var fb = bitmap.Lock();
                    unsafe
                    {
                        foreach (var tuple in value.OrePixels)
                        {
                            int v = tuple.Item3;
                            int x = tuple.Item1;
                            int y = tuple.Item2;
                            if (x >= fb.Size.Width || y >= fb.Size.Height) continue;

                            var mapping = byValue[v];
                            // Value without an ore mapping = no ore (Typhoon and some other mods use 0 for "empty", not 255)
                            if (mapping == null) continue;
                            byte a = 255;
                            if (UseOreDetectorRange && mapping != null && mapping.Start > OreDetectorRange)
                                a = 32;

                            byte r, g, b;
                            if (selectedByValue != null)
                            {
                                // An ore is selected: old style, its depth tiers are colored, everything else is dimmed
                                var sel = selectedByValue[v];
                                if (sel != null) { r = sel.MapRgbValue.R; g = sel.MapRgbValue.G; b = sel.MapRgbValue.B; }
                                else { r = 0; g = 0; b = 64; }
                            }
                            else
                            {
                                // Nothing selected: every ore type has its own color
                                var c = colorByValue[v];
                                if (c.HasValue) { r = c.Value.R; g = c.Value.G; b = c.Value.B; }
                                else { r = 0; g = 255; b = 0; }
                            }

                            byte* pixel = (byte*)fb.Address + y * fb.RowBytes + x * 4;
                            // Bgra8888, not premultiplied
                            pixel[0] = b;
                            pixel[1] = g;
                            pixel[2] = r;
                            pixel[3] = a;
                        }
                    }
                }
                catch { continue; }
            }
            OverlaysChanged?.Invoke();
        }

        void UpdateImages()
        {
            TileUp = null;
            TileFront = null;
            TileRight = null;
            TileBack = null;
            TileLeft = null;
            TileDown = null;

            var keys = _images.Keys;
            foreach (var key in keys)
            {
                if (SelectedPlanetDefinition == null) return;
                if (!_images.TryGetValue(key, out var value)) return;
                if (value == null) return;

                int? biome = null;
                if (SelectedEnvironmentItem != null) biome = SelectedEnvironmentItem.Biome;
                int? complexMaterial = null;
                if (SelectedComplexMaterial != null) complexMaterial = SelectedComplexMaterial.Value;
                var image = value.CreateBitmapImage(!ShowGradients, ShowLakes, complexMaterial, ShowBiomes, biome, ShowWater && Water != null && Water.Enabled && Water.HasUsableHills ? Water : null);

                switch (key)
                {
                    case CubeMapFace.Back: TileBack = image; break;
                    case CubeMapFace.Down: TileDown = image; break;
                    case CubeMapFace.Front: TileFront = image; break;
                    case CubeMapFace.Left: TileLeft = image; break;
                    case CubeMapFace.Right: TileRight = image; break;
                    case CubeMapFace.Up: TileUp = image; break;
                }
            }
        }

        Dictionary<CubeMapFace, ImageData> _images = new();

        public ImageData GetInfo(CubeMapFace face, CancellationToken token)
        {
            var dir = Path.GetDirectoryName(_lastOpenedFile);
            var imageDir = PathHelper.ResolveCase(Path.Combine(dir, "PlanetDataFiles", SelectedPlanetDefinition.Name));
            if (!Directory.Exists(imageDir)) return null;
            return ImageData.Create(imageDir, face, TileWidth, token);
        }

        Dictionary<CubeMapFace, ImageData> _materialMaps = new();
        Dictionary<CubeMapFace, ImageData> _heightMaps = new();

        public ICommand NoiseMapGeneratorCommand => new RelayCommand(o =>
        {
            //var ng = new NoiseGeneratorView();
            //ng.Show();
        });

        bool _redistributing;

        public ICommand RedistributeOreCommand => new RelayCommand(o => { _ = RedistributeAsync(); },
            o => SelectedPlanetDefinition != null && !_redistributing);

        async System.Threading.Tasks.Task RedistributeAsync()
        {
            if (_redistributing) return;
            _redistributing = true;
            RelayCommand.RequestRequery();
            try { await RedistributeCoreAsync(); }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                CrashLog.Write(ex, "Redistribute");
                MessageBox.Show("The redistribution failed:\r\n" + ex.Message + "\r\n\r\nDetails: " + CrashLog.FilePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _redistributing = false;
                RelayCommand.RequestRequery();
            }
        }

        /// <summary>Outcome of the background work, shown to the user afterwards.</summary>
        class RunResult
        {
            public string Message;
            public List<string> Failed = new();
        }

        async System.Threading.Tasks.Task RedistributeCoreAsync()
        {
            // Command line arg -ignoretiers to not reduce tier jumps
            bool ignoreTiers = Environment.GetCommandLineArgs().Any(x => x.Equals("-ignoretiers", StringComparison.InvariantCultureIgnoreCase));

            // Collect ore info
            var setup = new RedistributionSetup();
            setup.ViewModel.Water = Water;
            setup.ViewModel.PlanetName = SelectedPlanetDefinition?.Name;
            foreach (var oredef in OreTypes)
            {
                var info = new RedistributionSetupViewModel.OreInfo { Name = oredef.OreType, SpawnWeight = (int)(oredef.Percentage + .5) };
                info.AddDefaultMapping();
                setup.ViewModel.OreInfos.Add(info);
            }
            if (setup.ShowDialogBlocking() != true)
                return;

            // Update ore mapping values
            var nextOreValue = setup.ViewModel.FinalizeList();
            if (nextOreValue >= 256)
            {
                MessageBox.Show("Too many ore definitions in oremappings.xml. This will not work!",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Output folder: asked now (after the setup), starts where the last run went
            var folderDlg = new FolderDialog
            {
                Title = "Select output folder for the new material maps (*_mat.png) and oremappings.xml",
                InitialDirectory = AppSettings.LastOutputFolder,
            };
            if (folderDlg.ShowDialog() != true)
                return;
            var directory = folderDlg.SelectedPath;
            AppSettings.LastOutputFolder = directory;

            Dictionary<string, List<OreMapping>> mappingsDictionary = new();
            foreach (var info in setup.ViewModel.OreInfos)
            {
                var list = new List<OreMapping>();
                list.AddRange(info.OreMappings);
                mappingsDictionary[info.Name] = list;
                foreach (var mapping in list)
                    if (mapping.Value > 0) mapping.Type = info.Name;
            }

            // Fixed face order: the faces load in parallel and the dictionary order depends on who finished first,
            // which changed the random number stream and made the same seed give different maps.
            var keys = Enum.GetValues(typeof(CubeMapFace)).Cast<CubeMapFace>().Where(f => _images.ContainsKey(f) && _images[f] != null).ToList();
            var sources = keys.ToDictionary(k => k, k => _images[k]);

            // Water Mod rules
            var water = Water;
            bool useWater = water != null && water.Enabled;
            if (useWater && (!water.HasUsableHills || !sources.Values.All(i => i.HasHeightMap)))
            {
                MessageBox.Show("The water rules are switched on, but they cannot be used:\r\n" +
                                (water.HasUsableHills ? "a height map is missing for this planet." : "the hill range (HillParams Min/Max) is empty.") +
                                "\r\nOre will be placed without water rules.", "Water rules", MessageBoxButton.OK, MessageBoxImage.Warning);
                useWater = false;
            }
            var report = new Dictionary<string, OreReportRow>();
            foreach (var oi in setup.ViewModel.OreInfos)
                report[oi.Name] = new OreReportRow { Name = oi.Name, Mode = useWater ? oi.WaterMode : OreWaterMode.Anywhere };

            // Existing files: ask once, before anything is generated
            var xmlFileName = Path.Combine(directory, "oremappings.xml");
            var existing = new[] { xmlFileName }.Concat(keys.Select(k => Path.Combine(directory, k.ToString().ToLower() + "_mat.png")))
                .Where(File.Exists).Select(Path.GetFileName).ToList();
            if (existing.Count > 0)
            {
                if (MessageBox.Show("These files already exist in\r\n" + directory + "\r\n\r\n" + string.Join("\r\n", existing) +
                        "\r\n\r\nOverwrite them?\r\nNothing is changed if you choose No.", "Files found", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            var ctx = new PlacementContext
            {
                Setup = setup.ViewModel, Mappings = mappingsDictionary, TileWidth = TileWidth,
                IgnoreTiers = ignoreTiers, Water = water, UseWater = useWater,
            };

            // The heavy part runs in the background with a progress window; files are written at the very end,
            // so cancelling leaves the output folder untouched.
            var progress = new ProgressWindow();
            var cts = new System.Threading.CancellationTokenSource();
            progress.CancelRequested += () => cts.Cancel();
            var owner = UiBlocking.MainWindow;
            System.Threading.Tasks.Task modal = owner != null && owner.IsVisible ? progress.ShowDialog(owner) : ShowAndWait(progress);

            long lastPost = 0;
            void Post(double fraction, string status, bool force = false)
            {
                long now = Environment.TickCount64;
                if (!force && now - lastPost < 80) return; // do not flood the UI thread
                lastPost = now;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => progress.Report(fraction, status));
            }

            RunResult result = null;
            bool cancelled = false;
            try
            {
                result = await System.Threading.Tasks.Task.Run(() =>
                    Redistribute(ctx, sources, report, directory, xmlFileName, setup.ViewModel.Seed, keys, Post,
                        () => Avalonia.Threading.Dispatcher.UIThread.Post(() => progress.DisableCancel("Writing the files...")), cts.Token));
            }
            catch (OperationCanceledException) { cancelled = true; }
            finally
            {
                progress.Finish();
                try { await modal; } catch { }
            }
            if (cancelled)
            {
                MessageBox.Show("Cancelled. No files were changed.", "Ore Redistribution", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string failed = result.Failed.Count > 0 ? "\r\n\r\nThese files could not be written:\r\n" + string.Join("\r\n", result.Failed) : "";
            MessageBox.Show("Finished!\r\nPlease copy the pngs to the 'PlanetDataFiles/PlanetName' folder.\r\nThen open the planet definition SBC\r\nand copy the content of 'oremappings.xml' to the corresponding section." + result.Message + failed,
                "Ore Redistribution", MessageBoxButton.OK, result.Failed.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            if (AppSettings.OpenFolderWhenFinished) PathHelper.RevealInFileManager(xmlFileName);
        }

        static System.Threading.Tasks.Task ShowAndWait(Avalonia.Controls.Window w)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource();
            w.Closed += (_, _) => tcs.TrySetResult();
            w.Show();
            return tcs.Task;
        }

        /// <summary>
        /// The actual work, on a background thread: optional reference run, the real run, top-up, then writes
        /// oremappings.xml, the six maps and the report. Throws OperationCanceledException if cancelled before the writing starts.
        /// </summary>
        RunResult Redistribute(PlacementContext ctx, Dictionary<CubeMapFace, ImageData> sources, Dictionary<string, OreReportRow> report,
            string directory, string xmlFileName, int seed, List<CubeMapFace> keys,
            Action<double, string, bool> post, Action beforeWriting, System.Threading.CancellationToken token)
        {
            var setupVm = ctx.Setup;
            var water = ctx.Water;
            bool useWater = ctx.UseWater;
            bool waterRules = useWater && setupVm.OreInfos.Any(i => i.WaterMode != OreWaterMode.Anywhere);
            bool useReference = waterRules && water.KeepRatios;
            int n = keys.Count;

            // progress units: reference faces + real faces + top-up (2) + writing (n * 0.5)
            double total = (useReference ? n : 0) + n + (useReference ? 2 : 0) + n * 0.5;
            double done = 0;
            void Rep(string status, double fraction, bool force = false) => post((done + fraction) / total, status, force);

            // Reference: what the tool places without water rules. Gives the target per ore and layer
            // and the normal ore density (the land density limit is a multiple of it).
            Dictionary<CubeMapFace, long[]> reference = null;
            double normalDensity = 0;
            if (useReference)
            {
                var dummyReport = setupVm.OreInfos.ToDictionary(oi => oi.Name, oi => new OreReportRow { Name = oi.Name });
                var refPlacer = new OrePlacer(ctx, seed, false, dummyReport) { Token = token };
                reference = new();
                long orePixels = 0, allPixels = 0;
                int i = 0;
                foreach (var key in keys)
                {
                    int face = ++i;
                    refPlacer.Progress = f => Rep($"Reference run without water rules: face {face} of {n}", f);
                    var refImage = refPlacer.GenerateFace(sources[key], key);
                    var counts = OrePlacer.CountValues(refImage);
                    reference[key] = counts;
                    orePixels += counts.Sum() - counts[255];
                    allPixels += (long)ctx.TileWidth * ctx.TileWidth;
                    done += 1;
                }
                normalDensity = allPixels > 0 ? (double)orePixels / allPixels : 0;
            }

            var placer = new OrePlacer(ctx, seed, waterRules, report) { ReferenceDensity = normalDensity, Token = token };
            var results = new Dictionary<CubeMapFace, ImageData>();
            {
                int i = 0;
                foreach (var key in keys)
                {
                    int face = ++i;
                    placer.Progress = f => Rep($"Placing ore: face {face} of {n}", f);
                    results[key] = placer.GenerateFace(sources[key], key);
                    done += 1;
                }
            }
            if (reference != null)
            {
                placer.Progress = f => Rep("Making up for ore the water rules took away", f);
                placer.TopUp(results, reference);
                done += 2;
            }
            placer.Progress = null;
            placer.FillLayerStats(results, reference);
            string densityLine = placer.DensityLine(results);
            token.ThrowIfCancellationRequested();

            // From here on nothing can be cancelled: write everything
            beforeWriting();
            var run = new RunResult();
            try
            {
                var node = new XElement("OreMappings");
                foreach (var info in setupVm.OreInfos)
                    foreach (var mapping in ctx.Mappings[info.Name])
                        if (mapping.Value > 0) node.Add(mapping.ToXElement());
                new XDocument(node).Save(xmlFileName);
            }
            catch (Exception ex) { run.Failed.Add("oremappings.xml (" + ex.Message + ")"); }
            foreach (var key in keys)
            {
                Rep($"Writing {key.ToString().ToLower()}_mat.png", 0, true);
                if (!results[key].SaveMaterialMap(Path.Combine(directory, key.ToString().ToLower() + "_mat.png")))
                    run.Failed.Add(key.ToString().ToLower() + "_mat.png");
                done += 0.5;
            }

            string reportText = WaterPlacement.BuildReport(water, useWater, report.Values, densityLine, reference != null);
            try { File.WriteAllText(Path.Combine(directory, "placement_report.txt"), reportText); } catch { }
            if (useWater)
            {
                var shortfalls = WaterPlacement.Shortfalls(report.Values, reference != null);
                run.Message = "\r\n\r\nWater rules were applied. " + (shortfalls.Count == 0
                    ? "Ore ratios were kept."
                    : "Some ores stay below their target:\r\n" + string.Join("\r\n", shortfalls) +
                      "\r\nLower the water radius, raise the density limit, or set these ores to Land + seabed.") +
                    "\r\nDetails: placement_report.txt in the output folder.";
            }
            Rep("Done", 1, true);
            return run;
        }

        public ICommand DeselectOreTypeCommand => new RelayCommand(o =>
        {
            SelectedOreType = null;
        }, o => SelectedOreType != null);

        public ICommand DeselectEvironmentItemCommand => new RelayCommand(o =>
        {
            SelectedEnvironmentItem = null;
        }, o=> SelectedEnvironmentItem != null);

        public ICommand CreateOcclusionMapsCommand => new RelayCommand(o =>
        {
            var dir = Path.GetDirectoryName(_lastOpenedFile);
            var imageDir = PathHelper.ResolveCase(Path.Combine(dir, "PlanetDataFiles", SelectedPlanetDefinition.Name));
            var msgRes = MessageBox.Show("This will create the '*_add.png' files.\r\nThey don't seem to have any effect, but whatever...\r\nContinue?", "Warning", MessageBoxButton.OKCancel);
            if (msgRes != MessageBoxResult.OK) return;

            // Write files

            foreach (var face in Enum.GetValues(typeof(CubeMapFace)).Cast<CubeMapFace>())
            {
                var faceName = face.ToString().ToLower();
                var materialImg = SixLabors.ImageSharp.Image.Load(Path.Combine(imageDir, faceName + "_mat.png"));
                var material = materialImg as Image<Rgb24>;
                if (material == null)
                {
                    material = materialImg.CloneAs<Rgb24>();
                    materialImg.Dispose(); // Dispose original image, we don't need it anymore
                }
                var w = material.Width;
                var h = material.Height;
                var addImage = new SixLabors.ImageSharp.Image<Rgba32>(w, h);
                material.ProcessPixelRows(row =>
                {
                    for (int y = 0; y < h; ++y)
                    {
                        var span = row.GetRowSpan(y);
                        for (int x = 0; x < w; ++x)
                        {
                            var blue = span[x].B;
                            if (blue == 255)
                                addImage[x, y] = new Rgba32(0, 0, 0);
                            else
                                addImage[x, y] = new Rgba32(144, 0, 0);
                        }
                    }
                });
                addImage.SaveAsPng(Path.Combine(imageDir, faceName + "_add.png"));
                material.Dispose();
            }
            MessageBox.Show("Finished");
        }, o=> SelectedPlanetDefinition != null);

        public ICommand RewriteBiomesCommand => new RelayCommand(o =>
        {
            var msgRes = MessageBox.Show("This will override the biomes of the currently selected planet and save the files as '*_mat2.png'.\r\nOnly recommended for EarthLike and Alien\r\nThe tile width of the original files is used.\r\nContinue?", "Warning", MessageBoxButton.OKCancel);
            if (msgRes != MessageBoxResult.OK) return;

            var dir = Path.GetDirectoryName(_lastOpenedFile);
            var imageDir = PathHelper.ResolveCase(Path.Combine(dir, "PlanetDataFiles", SelectedPlanetDefinition.Name));
            if (!Directory.Exists(imageDir))
            {
                MessageBox.Show("Cannot find PlanetDataFiles folder.\r\nMake sure it exists!\r\n" + imageDir);
                return;
            }

            HashSet<int> allBiomes = new HashSet<int>();
            // Collect all required values:
            foreach (var envItem in EnvironmentItems)
            {
                allBiomes.Add(envItem.Biome);
            }

            // Analyze existing files by latitude
            // Remember all Biome values by number and quantity
            Dictionary<int, Dictionary<int, int>> biomeDic = new();
            int tileWidth = 2048;
            foreach (var face in Enum.GetValues(typeof(CubeMapFace)).Cast<CubeMapFace>())
            {
                var faceName = face.ToString().ToLower();
                var materialImg = SixLabors.ImageSharp.Image.Load(Path.Combine(imageDir, faceName + "_mat.png"));
                var material = materialImg as Image<Rgb24>;
                if (material == null)
                {
                    material = materialImg.CloneAs<Rgb24>();
                    materialImg.Dispose(); // Dispose original image, we don't need it anymore
                }
                tileWidth = material.Width;
                material.ProcessPixelRows(row =>
                {
                    for (int y = 0; y < tileWidth; ++y)
                    {
                        var span = row.GetRowSpan(y);
                        for (int x = 0; x < tileWidth; ++x)
                        {
                            int val = span[x].G;
                            if (!allBiomes.Contains(val))
                                val = -1; // Count how many pixels don't have biome def

                            var point = CoordinateHelper.GetNormalizedSphereCoordinates(face, x, y, tileWidth); // Based on tileWidth*tileWidth!!
                            var latitude = CoordinateHelper.ToLongitudeLatitude(point).latitude; // -90 to 90
                            var bucket = GetLatitudeBucket(latitude);
                            if (!biomeDic.ContainsKey(bucket)) { biomeDic[bucket] = new(); }
                            if (!biomeDic[bucket].ContainsKey(val))
                                biomeDic[bucket][val] = 1;
                            else
                                biomeDic[bucket][val]++;
                        }
                    }
                });
            }

            // EarthLike: +- 5° has only 113 + 85, ignore others
            foreach (var key in biomeDic[0].Keys.ToList())
            {
                if (key != 113 && key != 85 && key != -1) biomeDic[0].Remove(key);
            }

            // Generate new biome map
            byte[] planetSurface = new byte[tileWidth * tileWidth * 6];

            int surfaceBiomeCount = 0;
            var random = new Random(0);
            Action<int,int> placeBiome = (biomeIndex, biomeValue) =>
            {
                lock (planetSurface)
                {
                    if (biomeValue > 0)
                    {
                        if (planetSurface[biomeIndex] == 0) ++surfaceBiomeCount;
                        planetSurface[biomeIndex] = (byte)biomeValue;
                    }
                    else
                    {
                        if (planetSurface[biomeIndex] > 0) --surfaceBiomeCount;
                        planetSurface[biomeIndex] = 0;
                    }
                }
            };

            // Strategy: Drop random seeds of biomes that grow until map is filled to 83% (same value as vanilla)

            // Part 1: place seeds on 4% of all pixels
            double seedPercentage = 0.09;
            while (surfaceBiomeCount < planetSurface.Length * seedPercentage)
            {
                var index = random.NextTS(planetSurface.Length);
                var coord = PlanetSurfaceToCoordinates(index, tileWidth);
                var point = CoordinateHelper.GetNormalizedSphereCoordinates(coord.face, coord.x, coord.y, tileWidth);
                var latitude = CoordinateHelper.ToLongitudeLatitude(point).latitude; // -90 to 90
                var bucket = GetLatitudeBucket(latitude);
                var dic = biomeDic[bucket];
                placeBiome(index, RandomBiomePick(dic, random));
            }

            // Part 2: Let the seeds grow until surface is covered
            int xx = 0;
            while (surfaceBiomeCount < planetSurface.Length * 0.97 && ++xx < 8)
            {
                // Find all current seeds
                List<(int surfaceIndex, byte value)> seeds = new();
                for (int i = 0; i < planetSurface.Length; i++)
                {
                    var surfaceValue = planetSurface[i];
                    if (surfaceValue == 0) continue;
                    seeds.Add((i, surfaceValue));
                }
                // Randonly pick seeds and let them grow
                Parallel.For(0, seeds.Count, seedIndex =>
                {
                    (int surfaceIndex, byte value) seed;
                    lock (planetSurface)
                    {
                        seed = seeds[seedIndex];
                        if (seed.value != planetSurface[seed.surfaceIndex])
                            return; // Seed got destroyed
                    }

                    var coords = PlanetSurfaceToCoordinates(seed.surfaceIndex, tileWidth);
                    var pt = new CubeMapPointLight { Face = coords.face, X = coords.x, Y = coords.y, TileWidth=(ushort)tileWidth };
                    int[] nmap = new int[4];
                    // Get all four neigbors
                    var nUp = CubeMapPointLight.GetPointRelativeTo(pt, 0, 1);
                    var indexUp = CoordinatesToPlanetSurface(nUp.Face, nUp.X, nUp.Y, tileWidth);
                    nmap[0] = planetSurface[indexUp];
                    var nDown = CubeMapPointLight.GetPointRelativeTo(pt, 0, -1);
                    var indexDown = CoordinatesToPlanetSurface(nDown.Face, nDown.X, nDown.Y, tileWidth);
                    nmap[1] = planetSurface[indexDown];
                    var nLeft = CubeMapPointLight.GetPointRelativeTo(pt, -1, 0);
                    var indexLeft = CoordinatesToPlanetSurface(nLeft.Face, nLeft.X, nLeft.Y, tileWidth);
                    nmap[2] = planetSurface[indexLeft];
                    var nRight = CubeMapPointLight.GetPointRelativeTo(pt, 1, 0);
                    var indexRight = CoordinatesToPlanetSurface(nRight.Face, nRight.X, nRight.Y, tileWidth);
                    nmap[3] = planetSurface[indexRight];

                    Func<int, int> NeighborMapIndexToSurfaceMapIndex = (nIndex) =>
                    {
                        if (nIndex == 0) return indexUp;
                        if (nIndex == 1) return indexDown;
                        if (nIndex == 2) return indexLeft;
                        return indexRight;
                    };

                    // Rules:
                    // - Prefer to grow into unoccupied spaces
                    // - Never grow into same biome
                    List<int> emptyNeighbors = new List<int>();
                    List<int> differentNeighbors = new List<int>();
                    for (int i = 0; i < nmap.Length; ++i)
                    {
                        if (nmap[i] > 0)
                        {
                            if (nmap[i] != seed.value)
                            {
                                differentNeighbors.Add(i);
                            }
                        }
                        else emptyNeighbors.Add(i);
                    }
                    if (emptyNeighbors.Count > 0)
                    {
                        // Grow into unoccupied space
                        var growTo = emptyNeighbors[random.NextTS(emptyNeighbors.Count)];
                        placeBiome(NeighborMapIndexToSurfaceMapIndex(growTo), seed.value);
                    }
                });
            }

            // TODO: Don't leave black spots. Ore display bug. Fill them randomly
            for (int i = 0; i < planetSurface.Length; ++i)
            {
                if (planetSurface[i] != 0) continue;
                var coords = PlanetSurfaceToCoordinates(i, tileWidth);

                var neigbors = new CubeMapPointLight[]
                {
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, -1, 0),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, -1, 1),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, -1, -1),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, 1, 0),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, 1, 1),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, 1, -1),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, 0, 1),
                    CubeMapPointLight.GetPointRelativeTo(new() { X = coords.x, Y = coords.y, Face = coords.face }, 0, -1),
                };
                var neighborsWithValue = neigbors.Where(x =>
                {
                    var index = CoordinatesToPlanetSurface(x.Face, x.X, x.Y, tileWidth);
                    var value = planetSurface[index];
                    return value != 0;
                }).ToList();
                Debug.Assert(neighborsWithValue.Count > 0);
                var randomNeighbor = neighborsWithValue[random.NextTS(neighborsWithValue.Count)];
                var indexOfRnd = CoordinatesToPlanetSurface(randomNeighbor.Face, randomNeighbor.X, randomNeighbor.Y, tileWidth);
                var value = planetSurface[indexOfRnd];
                foreach (var x in neigbors)
                {
                    var index = CoordinatesToPlanetSurface(x.Face, x.X, x.Y, tileWidth);
                    if (planetSurface[index] == 0)
                    {
                        planetSurface[index] = value;
                    }
                }
                planetSurface[i] = value;
            }

            // Write files

            foreach (var face in Enum.GetValues(typeof(CubeMapFace)).Cast<CubeMapFace>())
            {
                var faceName = face.ToString().ToLower();
                var image = new SixLabors.ImageSharp.Image<Rgb24>(tileWidth, tileWidth);
                var materialImg = SixLabors.ImageSharp.Image.Load(Path.Combine(imageDir, faceName + "_mat.png"));
                var material = materialImg as Image<Rgb24>;
                if (material == null)
                {
                    material = materialImg.CloneAs<Rgb24>();
                    materialImg.Dispose(); // Dispose original image, we don't need it anymore
                }
                image.ProcessPixelRows(row =>
                {
                    for (int y = 0; y < tileWidth; ++y)
                    {
                        var span = row.GetRowSpan(y);
                        for (int x = 0; x < tileWidth; ++x)
                        {
                            var surfaceVal = planetSurface[CoordinatesToPlanetSurface(face, x, y, tileWidth)];
                            span[x] = new Rgb24(material[x, y].R, surfaceVal, material[x, y].B);
                        }
                    }
                });
                image.SaveAsPng(Path.Combine(imageDir, faceName + "_mat2.png"));
                material.Dispose();
                image.Dispose();
            }

            MessageBox.Show("Finished. Files were saved as '*_mat2.png' to preserve the original files.");
        }, o => SelectedPlanetDefinition != null);

        // Use a biome dictionary for a latitude.
        // Choose a random biome from that latitude.
        // Take relative quantities of different biomes into account
        int RandomBiomePick(Dictionary<int,int> dic, Random rnd)
        {
            // Sum up all biomes
            var sum = 0;
            var kvps = dic.ToList();
            foreach (var kvp in kvps)
            {
                sum += kvp.Value;
            }
            // Pick random number
            var pick = rnd.NextTS(sum);

            // Use counts like an index in a list
            // This should preserve the relative amounts of different biomes
            sum = 0;
            for (int i=0;i< kvps.Count; i++)
            {
                sum += kvps[i].Value;
                if (pick < sum) return kvps[i].Key;
            }
            // Fallback. Should not be hit.
            Debug.Assert(false);
            return kvps[0].Key;
        }

        (CubeMapFace face, int x, int y) PlanetSurfaceToCoordinates(int surfaceIndex, int tileWidth)
        {
            var twSquared = tileWidth * tileWidth;
            var div = surfaceIndex / (twSquared);
            var start = twSquared * div;
            var posRel = surfaceIndex % (twSquared);
            var y = posRel / tileWidth;
            var x = surfaceIndex % tileWidth;
            return ((CubeMapFace)div, x, y);
        }

        int CoordinatesToPlanetSurface(CubeMapFace face, int x, int y, int tileWidth)
        {
            return (int)face * tileWidth * tileWidth + x + y * tileWidth;
        }

        // Turn latitude values into dictionary keys
        // Bucket size is width of longitude area to cover for one key.
        // Keens SBC use a granularity of 5°.
        int GetLatitudeBucket(double latitude, int bucketsize = 5)
        {
            for (int i = -90; i <= 90; i += bucketsize)
                if (latitude < i) return i;
            return 90;
        }

        public ICommand PlusCommand => new RelayCommand(o =>
        {
            if (OreInspectorSize < 100) OreInspectorSize += 10;
        });

        public ICommand MinusCommand => new RelayCommand(o =>
        {
            if (OreInspectorSize > 10) OreInspectorSize -= 10;
        });

        public ICommand CalcOreAmountCommand => new RelayCommand(o =>
        {
            // TODO: Calculate sum of ore patch * depth to get total volume of ore
        });
    }
}
