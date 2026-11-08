using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SpaceEngineersOreRedistribution
{
    public partial class RedistributionSetup : DialogWindow
    {
        internal RedistributionSetupViewModel ViewModel { get; } = new RedistributionSetupViewModel();

        /// <summary>How often the setup is autosaved while this window is open (static so tests can shorten it).</summary>
        public static TimeSpan AutosaveInterval = TimeSpan.FromMinutes(5);

        DispatcherTimer _autosaveTimer;
        string _lastSavedKey;

        public RedistributionSetup()
        {
            DataContext = ViewModel;
            InitializeComponent();
            ViewModel.ConfirmAction = ConfirmAction;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Parent = this;
            Opened += (_, _) => StartAutosave();
            Closed += (_, _) =>
            {
                _autosaveTimer?.Stop();
                if (DialogResult != true) Autosave(); // closed without confirming: keep what was typed
            };
        }

        string SetupKey() => ViewModel.BuildDocument(false).ToString(SaveOptions.DisableFormatting);

        void StartAutosave()
        {
            _lastSavedKey = SetupKey(); // what is on screen now is not worth saving until something changes
            _autosaveTimer = new DispatcherTimer { Interval = AutosaveInterval };
            _autosaveTimer.Tick += (_, _) => Autosave();
            _autosaveTimer.Start();
        }

        /// <summary>Saves the setup as new autosave if it changed since the last save.</summary>
        void Autosave()
        {
            if (ViewModel.OreInfos.Count == 0) return;
            var key = SetupKey();
            if (key == _lastSavedKey) return;
            SetupStore.SaveAutosave(ViewModel.PlanetName, ViewModel.BuildDocument());
            _lastSavedKey = key;
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var items = new List<Control>();
            var saved = SetupStore.List(ViewModel.PlanetName);
            foreach (var sv in saved)
            {
                var path = sv.Path;
                var mi = new MenuItem { Header = sv.Title };
                mi.Click += (_, _) => LoadSaved(path);
                items.Add(mi);
            }
            if (saved.Count == 0) items.Add(new MenuItem { Header = "Nothing saved yet for this planet", IsEnabled = false });
            items.Add(new Separator());
            var fromFile = new MenuItem { Header = "From a file..." };
            fromFile.Click += (_, _) => ViewModel.ImportCommand.Execute(null);
            items.Add(fromFile);
            var cm = new ContextMenu { ItemsSource = items };
            LoadButton.ContextMenu = cm;
            cm.Open(LoadButton);
        }

        internal void LoadSaved(string path)
        {
            try
            {
                Autosave(); // the current setup is replaced, do not lose it
                ViewModel.LoadDocument(XDocument.Load(path));
                _lastSavedKey = SetupKey();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could not load the saved setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedInfo))
            {
                UpdateDiagramClick(null, null);
            }
        }

        /// <summary>Enter in a text box that applies on focus loss: take the focus away so the value is applied.</summary>
        private void ApplyOnEnter(object sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key != Avalonia.Input.Key.Enter) return;
            GetTopLevel(this)?.FocusManager?.ClearFocus();
            e.Handled = true;
        }

        private void ButtonAbort_Click(object sender, RoutedEventArgs e) => CloseWithResult(false);

        private void ConfirmAction()
        {
            SetupStore.SaveLastRun(ViewModel.PlanetName, ViewModel.BuildDocument()); // the "last run"
            CloseWithResult(true);
        }

        private void UpdateDiagramClick(object sender, RoutedEventArgs e)
        {
            var colors = new List<Color>
            {
                Colors.LawnGreen,
                Colors.DeepSkyBlue,
                Colors.Red,
                Colors.Orange,
                Colors.BlueViolet,
                Colors.Magenta,
                Colors.Green,
                Colors.Gold,
                Colors.MediumPurple,
                Colors.Blue,
            };
            ViewModel.FinalizeList();

            MyCanvas.Children.Clear();
            if (ViewModel.SelectedInfo == null) return;
            double depthMax = 0;
            foreach (var mapping in ViewModel.SelectedInfo.OreMappings)
            {
                if (mapping.Start + mapping.Depth > depthMax) depthMax = mapping.Start + mapping.Depth;
            }
            bool[] enabledTable = new bool[10];
            int i = -1;
            // Canvas is 320x240
            // Each rect must be 32 pixels
            foreach (var mapping in ViewModel.SelectedInfo.OreMappings)
            {
                ++i;
                if (i > 9) break;
                if (mapping.Value < 1)
                {
                    continue;
                }
                enabledTable[i] = true;
                var rect = new Rectangle();
                rect.Width = 28;
                rect.Height = 240 * mapping.Depth / depthMax;
                rect.Fill = new SolidColorBrush(colors[i]);
                MyCanvas.Children.Add(rect);
                Canvas.SetLeft(rect, i * 28 + 40);
                Canvas.SetTop(rect, 240 * mapping.Start / depthMax);
            }

            // Draw Probabilities for depth values
            double[] probs = new double[10];
            double probMax = 0;
            for (i = 0; i < 10; ++i)
            {
                double stdDev = ViewModel.StdDevDepth;
                var prob = 1.0;
                if (ViewModel.SelectedInfo.PreferredDepth > -1)
                {
                    var divider = stdDev * Math.Sqrt(2 * Math.PI);
                    var eTerm = -0.5 * Math.Pow((i - ViewModel.SelectedInfo.PreferredDepth) / stdDev, 2);
                    prob = Math.Exp(eTerm) / divider;
                }
                if (prob > probMax) probMax = prob;
                probs[i] = prob;
            }
            for (i = 0; i < 10; ++i)
            {
                if (!enabledTable[i]) continue;
                var rect = new Rectangle();
                rect.Width = 40 * probs[i] / probMax;
                rect.Height = 24;
                rect.Fill = Brushes.Gray;
                MyCanvas.Children.Add(rect);
                Canvas.SetLeft(rect, 0);
                Canvas.SetTop(rect, 24 * i);
            }
        }
    }
}
