using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>Simple pie chart. Ported from the WPF Canvas based control.</summary>
    public class PieChart : Control
    {
        public static readonly StyledProperty<BindingList<PieChartDataItem>> ItemsSourceProperty =
            AvaloniaProperty.Register<PieChart, BindingList<PieChartDataItem>>(nameof(ItemsSource));

        public static readonly StyledProperty<IPen> BorderPenProperty =
            AvaloniaProperty.Register<PieChart, IPen>(nameof(BorderPen), new Avalonia.Media.Immutable.ImmutablePen(Brushes.Black.ToImmutable(), 1));

        public static readonly StyledProperty<IBrush> FillProperty =
            AvaloniaProperty.Register<PieChart, IBrush>(nameof(Fill));

        public BindingList<PieChartDataItem> ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IPen BorderPen
        {
            get => GetValue(BorderPenProperty);
            set => SetValue(BorderPenProperty, value);
        }

        public IBrush Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        static PieChart()
        {
            AffectsRender<PieChart>(ItemsSourceProperty, BorderPenProperty, FillProperty);
            ItemsSourceProperty.Changed.AddClassHandler<PieChart>((c, e) =>
            {
                if (e.OldValue is BindingList<PieChartDataItem> old) old.ListChanged -= c.ItemsSourceOnListChanged;
                if (e.NewValue is BindingList<PieChartDataItem> val) val.ListChanged += c.ItemsSourceOnListChanged;
            });
        }

        void ItemsSourceOnListChanged(object sender, ListChangedEventArgs e) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual);

        public override void Render(DrawingContext dc)
        {
            base.Render(dc);
            var rect = new Rect(Bounds.Size);
            if (rect.Width < 1 || rect.Height < 1) return;

            double dx = rect.Width / 2;
            double dy = rect.Height / 2;
            var center = new Point(dx, dy);

            try // List might be changed while rendering
            {
                if (Fill != null)
                    dc.DrawEllipse(Fill, null, center, dx, dy);

                var source = ItemsSource;
                if (source != null && source.Count > 0)
                {
                    float max = source.Sum(item => item.Value > 0 ? item.Value : 0);
                    double currentValue = 0;

                    foreach (var item in source.ToList())
                    {
                        double value = item.Value > 0 ? item.Value : 0;
                        if (value == 0) continue;

                        double percentage = value / max;
                        double overallPercentage = currentValue / max;
                        currentValue += value;
                        double startDegrees = -90 + (360 * overallPercentage);
                        double sweepDegrees = 360 * percentage;
                        double startRadians = startDegrees * Math.PI / 180.0;

                        // Fix percentage = 1.0
                        if (sweepDegrees >= 359.999)
                        {
                            dc.DrawEllipse(item.FillBrush, null, center, dx, dy);
                            continue;
                        }

                        double sweepRadians = sweepDegrees * Math.PI / 180.0;
                        var start = new Point(dx + Math.Cos(startRadians) * dx, dy + Math.Sin(startRadians) * dy);
                        var end = new Point(dx + Math.Cos(startRadians + sweepRadians) * dx, dy + Math.Sin(startRadians + sweepRadians) * dy);

                        var geo = new StreamGeometry();
                        using (var ctx = geo.Open())
                        {
                            ctx.BeginFigure(center, true);
                            ctx.LineTo(start);
                            ctx.ArcTo(end, new Size(dx, dy), 0, sweepDegrees > 180, SweepDirection.Clockwise);
                            ctx.EndFigure(true);
                        }
                        dc.DrawGeometry(item.FillBrush, null, geo);
                    }
                }
                if (BorderPen != null)
                    dc.DrawEllipse(null, BorderPen, center, dx, dy);
            }
            catch { }
        }
    }

    /// <summary>Data item for Pie Charts.</summary>
    public class PieChartDataItem : PropChangeNotifier
    {
        float _value;
        IBrush _fillBrush;

        public float Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public IBrush FillBrush
        {
            get => _fillBrush;
            set
            {
                if (Equals(value, _fillBrush)) return;
                _fillBrush = value;
                OnPropertyChanged();
            }
        }

        public PieChartDataItem()
        {
            Value = 0;
            FillBrush = Brushes.White;
        }

        public PieChartDataItem(float value, IBrush brush)
        {
            _value = value;
            _fillBrush = brush;
        }
    }
}
