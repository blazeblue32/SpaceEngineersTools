using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace SpaceEngineersOreRedistribution
{
    public partial class RedistributionSetup : DialogWindow
    {
        internal RedistributionSetupViewModel ViewModel { get; } = new RedistributionSetupViewModel();

        public RedistributionSetup()
        {
            DataContext = ViewModel;
            InitializeComponent();
            ViewModel.ConfirmAction = ConfirmAction;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.Parent = this;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedInfo))
            {
                UpdateDiagramClick(null, null);
            }
        }

        private void ButtonAbort_Click(object sender, RoutedEventArgs e) => CloseWithResult(false);

        private void ConfirmAction() => CloseWithResult(true);

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
