using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SpaceEngineersToolsShared;
using System;
using System.Linq;
using System.Threading;

namespace SpaceEngineersOreRedistribution
{
    public partial class MainWindow : DialogWindow
    {
        internal MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        Point _origin;
        Point _start;

        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();

            ImageView.PointerWheelChanged += ImageView_MouseWheel;
            BorderView.PointerPressed += ImageView_MouseLeftButtonDown;
            BorderView.PointerReleased += ImageView_PointerReleased;
            BorderView.PointerMoved += ImageView_MouseMove;
            BorderView.PointerExited += (_, _) => UpdateOreInspectRect(new Point(-1, -1));
            OreInspectorCb.IsCheckedChanged += OreInspectorCb_Checked;
            LinkButton.Click += Hyperlink_Click;
        }

        Matrix CurrentMatrix => ImageView.RenderTransform?.Value ?? Matrix.Identity;

        private void OreInspectorCb_Checked(object sender, RoutedEventArgs e)
        {
            UpdateOreInspectRect(new Point(0, 0));
        }

        void UpdateOreInspectRect(Point p)
        {
            Matrix m = CurrentMatrix;
            if (p.X >= 0 && p.Y >= 0 && p.X < BorderView.Bounds.Width && p.Y < BorderView.Bounds.Height && OreInspectorCb.IsChecked == true
                && ViewModel.SelectedPlanetDefinition != null)
            {
                OreInspectorRect.IsVisible = true;
                // Canvas Top/Left is relative to the Canvas, and the Canvas is render transformed.
                // Need to convert mouse coordinates. Offset in matrix is not scaled.
                double top = (p.Y - m.M32) / m.M11 - OreInspectorRect.Height;
                double left = (p.X - m.M31) / m.M11 - OreInspectorRect.Width;
                if (left < 0) left = 0;
                if (top < 0) top = 0;
                if (left > ImageView.Width - OreInspectorRect.Width) left = ImageView.Width - OreInspectorRect.Width;
                if (top > ImageView.Height - OreInspectorRect.Height) top = ImageView.Height - OreInspectorRect.Height;
                Canvas.SetTop(OreInspectorRect, top);
                Canvas.SetLeft(OreInspectorRect, left);
            }
            else
            {
                OreInspectorRect.IsVisible = false;
            }
        }

        private void OpenOreInspector()
        {
            if (OreInspectorCb.IsChecked != true || ViewModel.SelectedPlanetDefinition == null) return;
            var x = (int)(Canvas.GetLeft(OreInspectorRect) + 0.5);
            var y = (int)(Canvas.GetTop(OreInspectorRect) + 0.5);
            // Check which image:
            CubeMapFace face;
            if (x >= 0 && y >= 0 && x <= 2048 && y <= 2048)
            {
                face = CubeMapFace.Up;
            }
            else if (x >= 0 && y >= 2048 && x <= 2048 && y <= 4096)
            {
                face = CubeMapFace.Front;
                y -= 2048;
            }
            else if (x >= 2048 && y >= 2048 && x <= 4096 && y <= 4096)
            {
                face = CubeMapFace.Right;
                x -= 2048;
                y -= 2048;
            }
            else if (x >= 4096 && y >= 2048 && x <= 6144 && y <= 4096)
            {
                face = CubeMapFace.Back;
                x -= 4096;
                y -= 2048;
            }
            else if (x >= 6144 && y >= 2048 && x <= 8192 && y <= 4096)
            {
                face = CubeMapFace.Left;
                x -= 6144;
                y -= 2048;
            }
            else if (x >= 4096 && y >= 4096 && x <= 6144 && y <= 6144)
            {
                face = CubeMapFace.Down;
                x -= 4096;
                y -= 4096;
            }
            else
                return;

            // Copy pixels into matrix
            var width = (int)(OreInspectorRect.Width + .5);
            var height = (int)(OreInspectorRect.Height + .5);
            OreMapping[,] oreMap = new OreMapping[width, height];

            var info = ViewModel.GetInfo(face, CancellationToken.None);
            if (info == null) return;
            for (int xx = 0; xx < width; ++xx)
            {
                for (int yy = 0; yy < height; ++yy)
                {
                    var xxx = xx + x;
                    var yyy = yy + y;
                    if (xxx >= 0 && xxx < 2048 && yyy >= 0 && yyy < 2048)
                    {
                        var color = info.B[xxx, yyy];
                        var mapping = ViewModel.SelectedPlanetDefinition.OreMappings.FirstOrDefault(o => o.Value == color);
                        oreMap[xx, yy] = mapping;
                    }
                }
            }

            var win = new Ore3dView();
            win.SetRectSize(ViewModel.OreInspectorSize);
            win.ViewModel.AddCuboids(oreMap);
            win.ShowDialogBlocking(this);
        }

        bool _moved = false;
        private void ImageView_MouseMove(object sender, PointerEventArgs e)
        {
            if (e.Pointer.Captured != BorderView)
            {
                UpdateOreInspectRect(e.GetPosition(BorderView));
                return;
            }
            if (!_moved)
            {
                // For some reason we get a mouse moved event upon first mouse_down.
                // Ignore this or image will jump
                _moved = true;
                return;
            }
            Point p = e.GetPosition(BorderView);
            Matrix m = CurrentMatrix;
            m = new Matrix(m.M11, m.M12, m.M21, m.M22, _origin.X + (p.X - _start.X), _origin.Y + (p.Y - _start.Y));
            ImageView.RenderTransform = new MatrixTransform(m);
            UpdateOreInspectRect(p);
        }

        private void ImageView_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                if (e.Pointer.Captured == BorderView) return;
                OpenOreInspector();
                return;
            }
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                e.Pointer.Capture(null);
                _moved = false;
            }
        }

        private void ImageView_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(BorderView).Properties.IsLeftButtonPressed) return;
            if (e.Pointer.Captured == BorderView) return;
            e.Pointer.Capture(BorderView);
            _moved = false;
            _start = e.GetPosition(BorderView);
            var m = CurrentMatrix;
            _origin = new Point(m.M31, m.M32);
        }

        private void ImageView_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            Point p = e.GetPosition(ImageView);
            double factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
            // Prepend a scale around the mouse position (in canvas coordinates)
            var scaleAt = Matrix.CreateTranslation(-p.X, -p.Y) * Matrix.CreateScale(factor, factor) * Matrix.CreateTranslation(p.X, p.Y);
            ImageView.RenderTransform = new MatrixTransform(scaleAt * CurrentMatrix);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sInfo = new System.Diagnostics.ProcessStartInfo("https://github.com/michi84o/SpaceEngineersTools")
                {
                    UseShellExecute = true,
                };
                System.Diagnostics.Process.Start(sInfo);
            }
            catch { /* no browser / xdg-open available */ }
        }
    }
}
