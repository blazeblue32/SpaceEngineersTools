using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using SpaceEngineersToolsShared;

namespace SpaceEngineersOreRedistribution
{
    // TODO Allow to edit ore mappings for each ore type so tiers can be adjusted

    internal class RedistributionSetupViewModel : PropChangeNotifier
    {
        public ObservableCollection<OreInfo> OreInfos { get; } = new();

        /// <summary>Water rules of the planet (shared with the main window). Null when none are loaded.</summary>
        WaterSettings _water;
        public WaterSettings Water
        {
            get => _water;
            set
            {
                if (_water != null) _water.PropertyChanged -= Water_PropertyChanged;
                _water = value;
                if (_water != null) _water.PropertyChanged += Water_PropertyChanged;
                OnPropertyChanged(); OnPropertyChanged(nameof(WaterEnabled)); OnPropertyChanged(nameof(WaterInfo));
                ApplyScale(); // the planet size may be different from the reference
            }
        }

        void Water_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WaterSettings.PlanetRadius)) ApplyScale();
        }

        public bool WaterEnabled => _water != null && _water.Enabled;

        int _bulkWaterModeIndex = 1; // Land + seabed
        /// <summary>Choice in the "all ores" combo box above the list.</summary>
        public int BulkWaterModeIndex
        {
            get => _bulkWaterModeIndex;
            set => SetProp(ref _bulkWaterModeIndex, value);
        }

        public ICommand ApplyWaterModeToAllCommand => new RelayCommand(o =>
        {
            foreach (var info in OreInfos) info.WaterModeIndex = _bulkWaterModeIndex;
        }, o => OreInfos.Count > 0 && WaterEnabled);

        public string WaterInfo => _water == null ? "" :
            _water.Enabled ? "Water rules ON: " + _water.Describe() :
            "Water rules are off (menu Water in the main window). The Water column has no effect.";
        OreInfo _selectedInfo;
        public OreInfo SelectedInfo
        {
            get => _selectedInfo;
            set => SetProp(ref _selectedInfo, value);
        }

        public ICommand AddOreCommand => new RelayCommand(o =>
        {
            var info = new OreInfo();
            info.AddDefaultMapping();
            OreInfos.Add(info);
            SelectedInfo = info;
            RaiseCount();
        });

        public ICommand ShowOverviewCommand => new RelayCommand(o =>
        {
            var overview = new OreOverviewWindow();
            overview.DataContext = new OreOverviewViewModel(this);
            if (Parent != null) overview.Show(Parent); else overview.Show();
        });

        public ICommand SortCommand => new RelayCommand(o =>
        {
            var list = OreInfos.ToList();
            list.Sort((a, b) =>
            {
                if (a.ExpectedRatio == b.ExpectedRatio) return 0;
                return a.ExpectedRatio < b.ExpectedRatio ? 1 : -1;
            });
            OreInfos.Clear();
            foreach (var item in list)
            {
                OreInfos.Add(item);
            }
            OnPropertyChanged(nameof(OreInfos));
        });

        public ICommand RemoveOreCommand => new RelayCommand(o =>
        {
            OreInfos.Remove(SelectedInfo);
            SelectedInfo = null;
        }, o => SelectedInfo != null);

        /// <summary>Planet this setup is for (names the autosave folder).</summary>
        public string PlanetName { get; set; }

        /// <summary>The whole setup as xml: Export file, last run and autosaves all use this.</summary>
        public XDocument BuildDocument(bool withTimestamp = true)
        {
            var root = new XElement("OreInfos");
            root.Add(new XAttribute("StdDev", StdDev));
            root.Add(new XAttribute("StdDevDepth", StdDevDepth));
            root.Add(new XAttribute("OreSpawnRate", OreSpawnRate));
            root.Add(new XAttribute("PlanetRadius", PlanetRadius.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            root.Add(new XAttribute("TileWidth", TileWidth));
            root.Add(new XAttribute("Seed", Seed));
            root.Add(new XAttribute("SmallPlanet", SmallPlanetMode));
            if (withTimestamp) root.Add(new XAttribute("Saved", DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)));
            foreach (var oreInfo in OreInfos)
                root.Add(oreInfo.ToXElement());
            return new XDocument(root);
        }

        /// <summary>Replaces the current setup with a saved one.</summary>
        public void LoadDocument(XDocument doc)
        {
            var root = doc.Root;
            if (root?.Name != "OreInfos") throw new Exception("Invalid file!");
            if (int.TryParse(root.Attribute("StdDev")?.Value, out var stdDev)) StdDev = stdDev;
            if (int.TryParse(root.Attribute("StdDevDepth")?.Value, out var stdDevDepth)) StdDevDepth = stdDevDepth;
            if (int.TryParse(root.Attribute("OreSpawnRate")?.Value, out var oresp))
            {
                // The saved number was for the planet size of that time: keep its distance in meters on this planet.
                // Files without a size (older exports) are taken as the reference size.
                double savedRadius = double.TryParse(root.Attribute("PlanetRadius")?.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sr) && sr > 0 ? sr : ReferenceRadius;
                int savedTile = int.TryParse(root.Attribute("TileWidth")?.Value, out var st) && st > 0 ? st : ReferenceTileWidth;
                _gap = oresp * Scale(savedRadius, savedTile);
                ApplyScale();
            }
            if (int.TryParse(root.Attribute("Seed")?.Value, out var seed)) Seed = seed;
            if (bool.TryParse(root.Attribute("SmallPlanet")?.Value, out var spm)) SmallPlanetMode = spm;
            OreInfos.Clear();
            foreach (var node in root.Elements("OreInfo"))
                OreInfos.Add(OreInfo.FromXElement(node));
            SelectedInfo = OreInfos.FirstOrDefault();
        }

        public ICommand ExportCommand => new RelayCommand(o =>
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "XML Files|*.xml",
                FileName = "OreInfos.xml"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                BuildDocument().Save(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, o => OreInfos.Count > 0);

        public ICommand ImportCommand => new RelayCommand(o =>
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "XML Files|*.xml",
                FileName = "OreInfos.xml"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                LoadDocument(XDocument.Load(dlg.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        int _stdDev = 10;
        public int StdDev
        {
            get => _stdDev;
            set
            {
                if (SetProp(ref _stdDev, value))
                {
                    if (value < 0) StdDev = 0;
                    if (value > 50) StdDev = 50;
                }
            }
        }

        int _stdDevDepth = 4;
        public int StdDevDepth
        {
            get => _stdDevDepth;
            set
            {
                if (SetProp(ref _stdDevDepth, value))
                {
                    if (value < 1) StdDevDepth = 1;
                    if (value > 10) StdDevDepth = 10;
                }
            }
        }

        // In original file each ore deposit is about 28 pixel away from the next deposit
        // in a checkboard pattern.
        // Lets randomize this a bit:
        // Square of 2*28 pixels width and length with 1 ore means:
        // 1 ore in 3136 pixels
        //
        // The number of pixels between ore patches means a different distance on every planet size: a pixel is
        // about 42 m wide on a 60 km radius planet and 4 x that on a 240 km one. The value is therefore tied to the
        // planet size: 1000 is the default for the EarthLike size (60 km radius = 120 km diameter, 2048 pixel tiles),
        // and when the planet size (or tile width) changes the number is recalculated so the ore patches stay the
        // same distance apart in meters. What is kept is _gap, the distance squared in "pixels x (radius / tile width)^2".
        public const double ReferenceRadius = 60000;
        public const int ReferenceTileWidth = 2048;
        public const int DefaultSpawnRate = 1000;
        public const int MinSpawnRate = 20;
        public const int MaxSpawnRate = 2000000;

        static double Scale(double radius, double tileWidth) => (radius / tileWidth) * (radius / tileWidth);

        double _gap = DefaultSpawnRate * Scale(ReferenceRadius, ReferenceTileWidth);
        bool _adjusting;
        double _localRadius = ReferenceRadius;
        int _tileWidth = ReferenceTileWidth;

        /// <summary>Tile width of the maps (pixels per cube face side), set by the main window.</summary>
        public int TileWidth
        {
            get => _tileWidth;
            set { if (value > 0 && value != _tileWidth) { _tileWidth = value; ApplyScale(); } }
        }

        /// <summary>Planet radius in meters (shared with the Water settings of the planet).</summary>
        public double PlanetRadius => _water?.PlanetRadius ?? _localRadius;

        public double PlanetRadiusKm => PlanetRadius / 1000;

        /// <summary>Planet diameter in km. Editing it changes the planet radius and with it the spawn rate below.</summary>
        public double PlanetDiameterKm
        {
            get => Math.Round(PlanetRadius * 2 / 1000, 3);
            set
            {
                if (!(value > 0)) return;
                if (_water != null) _water.PlanetRadius = value * 500; // raises Water_PropertyChanged -> ApplyScale
                else { _localRadius = value * 500; ApplyScale(); }
            }
        }

        /// <summary>Recalculates the spawn rate for the current planet size, keeping the distance in meters.</summary>
        void ApplyScale()
        {
            double n = _gap / Scale(PlanetRadius, _tileWidth);
            int v = (int)Math.Clamp(Math.Round(n), MinSpawnRate, MaxSpawnRate);
            _adjusting = true;
            try { OreSpawnRate = v; }
            finally { _adjusting = false; }
            OnPropertyChanged(nameof(PlanetDiameterKm));
            OnPropertyChanged(nameof(PlanetRadiusKm));
            OnPropertyChanged(nameof(SpawnInfo));
            OnPropertyChanged(nameof(SpawnDensity));
        }

        int _oreSpawnRate = DefaultSpawnRate;
        public int OreSpawnRate
        {
            get => _oreSpawnRate;
            set
            {
                if (!SetProp(ref _oreSpawnRate, value)) return;
                if (!_adjusting) _gap = value * Scale(PlanetRadius, _tileWidth); // typed by the user: this is the new wanted distance
                OnPropertyChanged(nameof(SpawnInfo));
                OnPropertyChanged(nameof(SpawnDensity));
            }
        }

        /// <summary>Pixels between ore patches that is "normal" (density 1) for the current planet size and tile width.</summary>
        double DefaultSpawnRateForSize => DefaultSpawnRate * Scale(ReferenceRadius, ReferenceTileWidth) / Scale(PlanetRadius, _tileWidth);

        /// <summary>
        /// Ore density as multiple of the default for this planet size (1 = default, 2 = twice as many ore patches per area).
        /// Linked to OreSpawnRate: density = default pixels / pixels. EarthLike: 2 = 500 pixels, 0.5 = 2000 pixels.
        /// It stays the same when the planet size changes, because the pixel number follows the size.
        /// </summary>
        public double SpawnDensity
        {
            get
            {
                double scale = Scale(PlanetRadius, _tileWidth);
                int expected = (int)Math.Clamp(Math.Round(_gap / scale), MinSpawnRate, MaxSpawnRate);
                // Normally the wanted distance gives the density exactly (the pixel number is only that rounded to a whole number);
                // if the number is being limited, show what the limited number really gives.
                double n = expected == _oreSpawnRate ? _gap / scale : _oreSpawnRate;
                return Math.Round(DefaultSpawnRateForSize / Math.Max(1, n), 3);
            }
            set
            {
                if (!(value > 0)) return;
                int n = (int)Math.Clamp(Math.Round(DefaultSpawnRateForSize / value), MinSpawnRate, MaxSpawnRate);
                OreSpawnRate = n; // a user change: sets the wanted distance and refreshes this property
                OnPropertyChanged(); // show the density that the (limited) pixel number really gives
            }
        }

        /// <summary>The distance between ore patches in meters that the number means for this planet size.</summary>
        public string SpawnInfo
        {
            get
            {
                double pixel = PlanetRadius * Math.Sqrt(2 * Math.PI / 3) / _tileWidth; // a cube face covers 4 pi r^2 / 6
                double gap = Math.Sqrt(Math.Max(1, _oreSpawnRate)) * pixel;
                string g = gap >= 1000 ? $"{gap / 1000:0.0} km" : $"{gap:0} m";
                return $"= about {g} between ore patches ({pixel:0.#} m per pixel)";
            }
        }

        int _seed;
        public int Seed
        {
            get => _seed;
            set => SetProp(ref _seed, value);
        }

        bool _smallPlanetMode;
        public bool SmallPlanetMode
        {
            get => _smallPlanetMode;
            set => SetProp(ref _smallPlanetMode, value);
        }

        public Action ConfirmAction;
        public ICommand ConfirmCommand => new RelayCommand(o =>
        {
            ConfirmAction?.Invoke();
        },
        // TODO: Value should be buffered
        o => OreInfos.Count > 0 && ValuesCount >= 1 && ValuesCount <= 254);

        public int ValuesCount
        {
            get { return FinalizeList(); }
        }

        void RaiseCount()
        {
            OnPropertyChanged(nameof(ValuesCount));
            OnPropertyChanged(nameof(ConfirmProblem));
            OnPropertyChanged(nameof(ValuesCountBrush));
        }

        /// <summary>Why Confirm is not possible (empty = it is).</summary>
        public string ConfirmProblem
        {
            get
            {
                if (OreInfos.Count == 0) return "Add at least one ore.";
                int n = ValuesCount;
                if (n > 254) return $"Too many values: {n}, the maximum is 254. Switch layers off or remove ores.";
                if (n == 0) return "No layer is switched on.";
                return "";
            }
        }

        /// <summary>Red when over the limit.</summary>
        public Avalonia.Media.IBrush ValuesCountBrush => ValuesCount > 254 ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.Black;

        public class OreTierItem
        {
            public int Start;
            public int Depth;
        }
        public class OreTierListContainer
        {
            public string Name { get; set; }
            public List<OreTierItem> Items { get; } = new List<OreTierItem>();

            public XElement ToXelement()
            {
                var elem = new XElement("OreTierPreset", new XAttribute("Name", Name));
                foreach (var item in Items)
                {
                    elem.Add(
                        new XElement("OreTierItem",
                            new XAttribute("Start", item.Start),
                            new XAttribute("Depth", item.Depth)));
                }
                return elem;
            }
        }

        OreTierListContainer _selectedOreMappingPreset;
        public OreTierListContainer SelectedOreMappingPreset
        {
            get => _selectedOreMappingPreset;
            set => SetProp(ref _selectedOreMappingPreset, value);
        }
        public ObservableCollection<OreTierListContainer> OreMappingPresets { get; } = new();

        public RedistributionSetupViewModel()
        {
            OreInfos.CollectionChanged += OreInfos_CollectionChanged;
            LoadPresets();
            SelectedOreMappingPreset = OreMappingPresets.FirstOrDefault();
        }

        void LoadPresets()
        {
            try
            {
                OreMappingPresets.Clear();
                SelectedOreMappingPreset = null;
                {
                    var path = PathHelper.PresetFile;
                    if (File.Exists(path))
                    {
                        var doc = XDocument.Load(path);
                        if (doc.Root?.Name == "OreTierPresets")
                        {
                            try
                            {
                                foreach (var elem in doc.Root.Elements("OreTierPreset"))
                                {
                                    OreTierListContainer container = new();
                                    container.Name = elem.Attribute("Name")?.Value ?? "Undefined";
                                    foreach (var item in elem.Elements("OreTierItem"))
                                    {
                                        var tierItem = new OreTierItem
                                        {
                                            Start = int.Parse(item?.Attribute("Start")?.Value ?? "1"),
                                            Depth = int.Parse(item?.Attribute("Depth")?.Value ?? "1")
                                        };
                                        container.Items.Add(tierItem);
                                    }
                                    OreMappingPresets.Add(container);
                                }
                            }
                            catch { /* silent */ }
                        }
                    }
                }
                SelectedOreMappingPreset = OreMappingPresets.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ICommand LoadPresetCommand => new RelayCommand(o =>
        {
            var items = SelectedOreMappingPreset.Items;
            for (int i = 0; i < items.Count && i < SelectedInfo.OreMappings.Count; ++i)
            {
                SelectedInfo.OreMappings[i].Start = items[i].Start;
                SelectedInfo.OreMappings[i].Depth = items[i].Depth;
            }

            // Causes redraw of diagram
            var info = SelectedInfo;
            SelectedInfo = null;
            SelectedInfo = info;

        }, o => SelectedOreMappingPreset != null && SelectedInfo != null);

        public ICommand LoadPresetAllCommand => new RelayCommand(o =>
        {
            foreach (var info in OreInfos)
            {
                var items = SelectedOreMappingPreset.Items;
                for (int i = 0; i < items.Count && i < info.OreMappings.Count; ++i)
                {
                    info.OreMappings[i].Start = items[i].Start;
                    info.OreMappings[i].Depth = items[i].Depth;
                }
            }

            // Causes redraw of diagram
            var infoX = SelectedInfo;
            SelectedInfo = null;
            SelectedInfo = infoX;

        }, o => SelectedOreMappingPreset != null && SelectedInfo != null);

        public DialogWindow Parent;
        public ICommand SavePresetCommand => new RelayCommand(o =>
        {
            try
            {
                var path = PathHelper.PresetFile;
                XDocument doc;
                try
                {
                    doc = XDocument.Load(path);
                    if (doc.Root.Name != "OreTierPresets") throw new Exception();
                }
                catch
                {
                    var elem = new XElement("OreTierPresets");
                    doc = new XDocument(elem);
                }

                TextInputPopup popup = new TextInputPopup();
                var res = popup.ShowDialogBlocking(Parent);
                if (res != true) return;

                OreTierListContainer container = new();
                foreach (var item in SelectedInfo.OreMappings)
                {
                    container.Items.Add(new OreTierItem { Start = item.Start, Depth = item.Depth });
                }
                container.Name = popup.Text;
                doc.Root.Add(container.ToXelement());
                doc.Save(path);

                OreMappingPresets.Add(container);
                SelectedOreMappingPreset = container;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, o => SelectedInfo != null);

        private void OreInfos_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    ((OreInfo)item).PropertyChanged += OreItem_PropertyChanged;
                }
            }
            // Minor memory leak prevention
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    ((OreInfo)item).PropertyChanged -= OreItem_PropertyChanged;
                }
            }
            CalcRatios();
            RaiseCount();
        }

        private void OreItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // "Layers" is only the display of the result of FinalizeList, reacting to it would loop
            if (e.PropertyName == nameof(OreInfo.Layers)) return;
            RaiseCount();
            CalcRatios();
        }

        void CalcRatios()
        {
            // Calc rations
            double sum = 0;
            foreach (var item in OreInfos)
            {
                sum += item.TypicalSize * item.SpawnWeight;
            }
            foreach (var item in OreInfos)
            {
                item.ExpectedRatio = (item.TypicalSize * item.SpawnWeight) / sum;
            }
            RatiosCalculated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RatiosCalculated;

        public class OreInfo : PropChangeNotifier
        {
            string _name;
            public string Name
            {
                get => _name;
                set => SetProp(ref _name, value);
            }

            int _spawnWeight = 1;
            public int SpawnWeight
            {
                get => _spawnWeight;
                set => SetProp(ref _spawnWeight, value);
            }

            int _typicalSize = 15;
            public int TypicalSize
            {
                get => _typicalSize;
                set
                {
                    if (SetProp(ref _typicalSize, value))
                    {
                        if (value > 500) _typicalSize = 500;
                        else if (value < 0) _typicalSize = 0;
                    }
                }
            }

            double _expectedRatio;
            public double ExpectedRatio
            {
                get => _expectedRatio;
                set => SetProp(ref _expectedRatio, value);
            }

            int _typicalDepthMin = -1;
            public int TypicalDepthMin
            {
                get => _typicalDepthMin;
                set
                {
                    if (SetProp(ref _typicalDepthMin, value))
                    {
                        if (value > 8)
                        {
                            if (VeryDeepOre) TypicalDepthMin = 9;
                            else TypicalDepthMin = 8;
                        }
                        else if (value < -1) TypicalDepthMin = -1;
                        if (value != -1 && value > TypicalDepthMax && TypicalDepthMax != -1) TypicalDepthMin = TypicalDepthMax;
                    }
                }
            }

            int _preferredDepth = -1;
            public int PreferredDepth
            {
                get => _preferredDepth;
                set => SetProp(ref _preferredDepth, value);
            }

            int _typicalDepthMax = -1;
            public int TypicalDepthMax
            {
                get => _typicalDepthMax;
                set
                {
                    if (SetProp(ref _typicalDepthMax, value))
                    {
                        if (value > 8)
                        {
                            if (VeryDeepOre) TypicalDepthMax = 9;
                            else TypicalDepthMax = 8;
                        }
                        else if (value < -1) TypicalDepthMax = -1;
                        if (value != -1 && value < TypicalDepthMin) TypicalDepthMax = TypicalDepthMin;
                    }
                }
            }

            OreWaterMode _waterMode = OreWaterMode.AboveWater;
            /// <summary>Only used when the planet has water rules enabled.</summary>
            public OreWaterMode WaterMode
            {
                get => _waterMode;
                set
                {
                    if (SetProp(ref _waterMode, value))
                    {
                        OnPropertyChanged(nameof(WaterModeIndex));
                        OnPropertyChanged(nameof(LandShareEnabled));
                    }
                }
            }

            double? _landShare;
            /// <summary>
            /// Land / seabed override of this ore (Land + seabed only): percent of its veins that start on land.
            /// null = the baseline of the Water window. 0 = seabed only, 100 = land only, in between = preference.
            /// A layer can override this again (see OreMapping.LandShare).
            /// </summary>
            public double? LandShare
            {
                get => _landShare;
                set => SetProp(ref _landShare, value.HasValue ? Math.Clamp(value.Value, 0, 100) : null);
            }

            /// <summary>The overrides only matter for ores set to Land + seabed.</summary>
            public bool LandShareEnabled => _waterMode == OreWaterMode.LandAndSeabed;

            /// <summary>For the combo box, see OreWaterModes.Order.</summary>
            public int WaterModeIndex
            {
                get => Math.Max(0, Array.IndexOf(OreWaterModes.Order, _waterMode));
                set => WaterMode = OreWaterModes.Order[Math.Clamp(value, 0, OreWaterModes.Order.Length - 1)];
            }

            bool _veryDeepOre = true;
            public bool VeryDeepOre
            {
                get => _veryDeepOre;
                set
                {
                    if (SetProp(ref _veryDeepOre, value))
                    {
                        if (!value)
                        {
                            if (TypicalDepthMax == 9)
                                TypicalDepthMax = 8;
                            if (TypicalDepthMin == 9)
                                TypicalDepthMin = 8;
                        }
                    }
                }
            }

            public ObservableCollection<OreMapping> OreMappings { get; } = new();

            public OreInfo()
            {
                OreMappings.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                        foreach (OreMapping m in e.NewItems) m.PropertyChanged += Mapping_PropertyChanged;
                    if (e.OldItems != null)
                        foreach (OreMapping m in e.OldItems) m.PropertyChanged -= Mapping_PropertyChanged;
                };
            }

            void Mapping_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                // A layer was switched on or off: the numbers have to be handed out again
                if (e.PropertyName == nameof(OreMapping.LayerEnabled)) OnPropertyChanged("LayerToggled");
            }

            /// <summary>Layers that get a number, e.g. "7/10". Updated by FinalizeList.</summary>
            public string Layers => $"{OreMappings.Count(m => m.Value > 0)}/{OreMappings.Count}";
            public void RefreshLayers() => OnPropertyChanged(nameof(Layers));

            public void AddDefaultMapping()
            {
                OreMappings.Clear();
                List<OreMapping> mList = new();
                // Tier 1: Flat surface ore. Depth: 3m - 15m
                OreMappings.Add(new OreMapping { Tier = 1, Value = 0, Type = Name, Start = 3, Depth = 3, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 1, Value = 1, Type = Name, Start = 5, Depth = 5, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 1, Value = 2, Type = Name, Start = 8, Depth = 7, ColorInfluence = "15", TargetColor = "#616c83" });
                // Tier 2: Medium deep veins. Depth: 40m - 102m
                OreMappings.Add(new OreMapping { Tier = 2, Value = 3, Type = Name, Start = 40, Depth = 12, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 2, Value = 4, Type = Name, Start = 50, Depth = 22, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 2, Value = 5, Type = Name, Start = 70, Depth = 32, ColorInfluence = "15", TargetColor = "#616c83" });
                // Tier 3: Very deep veins. Depth: 140m - 372m
                OreMappings.Add(new OreMapping { Tier = 3, Value = 6, Type = Name, Start = 140, Depth = 62, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 3, Value = 7, Type = Name, Start = 200, Depth = 72, ColorInfluence = "15", TargetColor = "#616c83" });
                OreMappings.Add(new OreMapping { Tier = 3, Value = 8, Type = Name, Start = 270, Depth = 102, ColorInfluence = "15", TargetColor = "#616c83" });
                // Bonus: Very deep ore. Depth: 370m - 450m
                OreMappings.Add(new OreMapping { Tier = 4, Value = 9, Type = Name, Start = 370, Depth = 80, ColorInfluence = "15", TargetColor = "#616c83" });
            }

            public static OreInfo FromXElement(XElement node)
            {
                var info = new OreInfo();
                info.Name = node.Element("Name")?.Value ?? "";
                int tmp;
                if (int.TryParse(node.Element("SpawnWeight")?.Value, out tmp)) info.SpawnWeight = tmp;
                if (int.TryParse(node.Element("TypicalSize")?.Value, out tmp)) info.TypicalSize = tmp;
                if (int.TryParse(node.Element("TypicalDepthMin")?.Value, out tmp)) info.TypicalDepthMin = tmp;
                if (int.TryParse(node.Element("TypicalDepthMax")?.Value, out tmp)) info.TypicalDepthMax = tmp;
                if (int.TryParse(node.Element("PreferredDepth")?.Value, out tmp)) info.PreferredDepth = tmp;
                if (bool.TryParse(node.Element("VeryDeepOre")?.Value, out var bb)) info.VeryDeepOre = bb;
                if (Enum.TryParse<OreWaterMode>(node.Element("WaterMode")?.Value, out var wm)) info.WaterMode = wm;

                var mappings = node.Element("Mappings");
                if (mappings != null)
                {
                    info.OreMappings.Clear();
                    foreach (var item in mappings.Elements())
                    {
                        if (item.Name == "Ore")
                        {
                            info.OreMappings.Add(OreMapping.FromXElement(item));
                        }
                    }
                }

                // Fallback if mapping is incomplete
                if (info.OreMappings.Count != 10) info.AddDefaultMapping();

                info.OreMappings[0].Tier = info.OreMappings[1].Tier = info.OreMappings[2].Tier = 1;
                info.OreMappings[3].Tier = info.OreMappings[4].Tier = info.OreMappings[5].Tier = 2;
                info.OreMappings[6].Tier = info.OreMappings[7].Tier = info.OreMappings[8].Tier = 3;
                info.OreMappings[9].Tier = 4;

                // Land / seabed overrides
                static double? Share(string t) => double.TryParse(t?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
                info.LandShare = Share(node.Element("LandShare")?.Value);
                var layerShares = node.Element("LayerLandShares")?.Value?.Split(';');
                if (layerShares != null && layerShares.Length == info.OreMappings.Count)
                    for (int i = 0; i < layerShares.Length; ++i) info.OreMappings[i].LandShare = Share(layerShares[i]);

                // Switched off layers (flags in layer order)
                var flags = node.Element("Layers")?.Value?.Trim();
                if (flags != null && flags.Length == info.OreMappings.Count)
                    for (int i = 0; i < flags.Length; ++i) info.OreMappings[i].LayerEnabled = flags[i] != '0';

                return info;
            }

            public XElement ToXElement()
            {
                var elem = new XElement(
                    "OreInfo",
                    new XElement("Name", Name),
                    new XElement("SpawnWeight", SpawnWeight),
                    new XElement("TypicalSize", TypicalSize),
                    new XElement("TypicalDepthMin", TypicalDepthMin),
                    new XElement("TypicalDepthMax", TypicalDepthMax),
                    new XElement("PreferredDepth", PreferredDepth),
                    new XElement("VeryDeepOre", VeryDeepOre),
                    new XElement("WaterMode", WaterMode),
                    new XElement("Layers", string.Concat(OreMappings.Select(m => m.LayerEnabled ? '1' : '0'))),
                    new XElement("LandShare", LandShare.HasValue ? LandShare.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""),
                    new XElement("LayerLandShares", string.Join(";", OreMappings.Select(m => m.LandShare.HasValue ? m.LandShare.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""))));

                if (OreMappings.Count > 0)
                {
                    var mappings = new XElement("Mappings");
                    foreach (var mapping in OreMappings)
                    {
                        mapping.Type = Name;
                        mappings.Add(mapping.ToXElement());
                    }
                    elem.Add(mappings);
                }
                return elem;
            }
        }

        public int WeightSum;
        public int FinalizeList()
        {
            // Weight Sum
            var weights = OreInfos.Select(o => o.SpawnWeight).ToList();
            int weightSum = 0;
            foreach (var weight in weights) weightSum += weight;
            WeightSum = weightSum;

            int value = 1;
            // Update ore mapping values
            foreach (var info in OreInfos)
            {
                foreach (var mapping in info.OreMappings)
                {
                    mapping.Value = mapping.LayerEnabled ? 0 : -1; // 0 = enabled, gets a number below
                }

                // If deep ore deselected, ignore entry
                if (!info.VeryDeepOre) info.OreMappings[9].Value = -1; // disabled
                if (info.TypicalDepthMin > 0)
                {
                    for (int i=0; i<info.TypicalDepthMin; ++i)
                        info.OreMappings[i].Value = -1;
                }
                if (info.TypicalDepthMax >= 0) // 0 is a valid maximum (surface layer only); -1 = no limit
                {
                    for (int i = 9; i > info.TypicalDepthMax; --i)
                        info.OreMappings[i].Value = -1;
                }

                foreach (var mapping in info.OreMappings)
                {
                    if (mapping.Value == 0)
                        mapping.Value = value++;
                }
                info.RefreshLayers();
            }
            return value - 1;
        }
        public OreInfo PickRandomOreWeighted(Random rnd)
        {
            if (WeightSum == 0) return null;
            var pick = rnd.Next(WeightSum) + 1;

            int currentPos = 0;
            foreach (var ore in OreInfos)
            {
                currentPos += ore.SpawnWeight;
                if (currentPos >= pick) return ore;
            }
            return null;
        }

    }

}
