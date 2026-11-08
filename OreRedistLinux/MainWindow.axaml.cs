using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SpaceEngineersToolsShared;
using System;
using System.Collections.Generic;
using System.IO;
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
            BorderView.PointerExited += (_, _) => { UpdateOreInspectRect(new Point(-1, -1)); StatusText.Text = StatusHint; };
            OreInspectorCb.IsCheckedChanged += OreInspectorCb_Checked;
            // Overlay pixels are changed in place; Avalonia does not notice, so repaint the images
            ViewModel.OverlaysChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var img in ImageView.Children.OfType<Image>()) img.InvalidateVisual();
            });
            LinkButton.Click += Hyperlink_Click;

            // Fit the map once when the maps of a planet show up; status bar hint
            StatusText.Text = StatusHint;
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.SelectedPlanetDefinition)) _fitPending = true;
                else if (e.PropertyName == nameof(MainWindowViewModel.TileUp) && _fitPending && ViewModel.TileUp != null)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (FitToWindow()) _fitPending = false; });
            };
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.D0, KeyModifiers.Control), Command = new RelayCommand(_ => FitToWindow()) });
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.D1, KeyModifiers.Control), Command = new RelayCommand(_ => ZoomActual()) });

            // Recent files (rebuilt every time the File menu opens) and drag and drop of .sbc files
            FileMenu.SubmenuOpened += (_, _) => RebuildRecentMenu();
            RebuildRecentMenu();
            AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AddHandler(DragDrop.DropEvent, OnDrop);
            InitGeology();
            // Ctrl+O opens a planet definition
            KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.O, KeyModifiers.Control), Command = ViewModel.OpenPlanetDefinitionCommand });
        }


        // ------------------------------------------------------------------ geology: region overlay and painter
        const int Zoom = 4; // region cells -> canvas pixels (2048 canvas px per face / 512 cells)
        WriteableBitmap _regionBmp;
        bool _painting;
        (int x, int y) _lastCell;

        void InitGeology()
        {
            _regionBmp = new WriteableBitmap(new PixelSize(RegionMap.Width, RegionMap.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            RegionImage.Source = _regionBmp;
            ViewModel.RegionsChanged += () => DrawRegions(0, 0, RegionMap.Width - 1, RegionMap.Height - 1);
            ViewModel.RegionsVisibilityChanged += UpdateRegionVisibility;
            ViewModel.Painter.HistoryChanged += () => { UndoButton.IsEnabled = ViewModel.Painter.CanUndo; RedoButton.IsEnabled = ViewModel.Painter.CanRedo; };
            UndoButton.IsEnabled = RedoButton.IsEnabled = false;
            foreach (var (rb, tool) in new[] { (ToolSeabed, PaintTool.Seabed), (ToolUplift, PaintTool.Uplift), (ToolVolcanic, PaintTool.Volcanic), (ToolLand, PaintTool.Land), (ToolErase, PaintTool.Erase) })
            {
                var t = tool;
                rb.IsCheckedChanged += (_, _) => { if (rb.IsChecked == true) { ViewModel.Painter.Tool = t; UpdateOreInspectRect(new Point(-1, -1)); } };
            }
            BrushSlider.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) UpdateBrushText(); };
            UpdateBrushText();
        }

        void UpdateBrushText()
        {
            ViewModel.Painter.Radius = (int)Math.Round(BrushSlider.Value);
            double radius = ViewModel.Water?.PlanetRadius ?? 60000;
            double cellKm = Math.PI * radius / 2 / RegionMap.Res / 1000;
            BrushText.Text = $"{(2 * ViewModel.Painter.Radius + 1) * cellKm:0.#} km";
        }

        void UpdateRegionVisibility() => RegionImage.IsVisible = ViewModel.ShowRegions || PainterBar.IsVisible;

        private void PaintRegions_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedPlanetDefinition == null) { MessageBox.Show("Open a planet definition first.", "Geology"); return; }
            PainterBar.IsVisible = true;
            if (ViewModel.Painter.Tool == PaintTool.None) ToolSeabed.IsChecked = true; else ViewModel.Painter.Tool = ViewModel.Painter.Tool;
            UpdateRegionVisibility();
        }

        private void PaintDone_Click(object sender, RoutedEventArgs e)
        {
            PainterBar.IsVisible = false;
            ViewModel.Painter.Tool = PaintTool.None;
            foreach (var rb in new[] { ToolSeabed, ToolUplift, ToolVolcanic, ToolLand, ToolErase }) rb.IsChecked = false;
            BrushCircle.IsVisible = false;
            UpdateRegionVisibility();
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => AfterEdit(ViewModel.Painter.Undo(ViewModel.Regions));
        private void Redo_Click(object sender, RoutedEventArgs e) => AfterEdit(ViewModel.Painter.Redo(ViewModel.Regions));
        private void ClearPaint_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.Regions.HasPaint) return;
            if (MessageBox.Show("Remove all painted regions of this planet? (Undo brings them back.)", "Geology", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            AfterEdit(ViewModel.Painter.ClearAll(ViewModel.Regions));
        }

        void AfterEdit((int x0, int y0, int x1, int y1)? dirty)
        {
            if (dirty.HasValue) { DrawRegions(dirty.Value.x0, dirty.Value.y0, dirty.Value.x1, dirty.Value.y1); ViewModel.PaintFinished(); }
        }

        bool CellAt(PointerEventArgs e, out int cx, out int cy)
        {
            var p = e.GetPosition(ImageView);
            cx = (int)Math.Floor(p.X / Zoom); cy = (int)Math.Floor(p.Y / Zoom);
            return true;
        }

        void BeginPaint(PointerPressedEventArgs e)
        {
            if (e.Pointer.Captured == BorderView) return;
            e.Pointer.Capture(BorderView);
            _painting = true;
            ViewModel.Painter.BeginStroke();
            CellAt(e, out int cx, out int cy);
            _lastCell = (cx, cy);
            AfterDraw(ViewModel.Painter.PaintAt(ViewModel.Regions, cx, cy));
            UpdateBrush(e.GetPosition(ImageView));
        }

        void PaintMove(PointerEventArgs e)
        {
            CellAt(e, out int cx, out int cy);
            if ((cx, cy) != _lastCell)
            {
                AfterDraw(ViewModel.Painter.PaintLine(ViewModel.Regions, _lastCell.x, _lastCell.y, cx, cy));
                _lastCell = (cx, cy);
            }
            UpdateBrush(e.GetPosition(ImageView));
        }

        void EndPaint(PointerReleasedEventArgs e)
        {
            _painting = false;
            e.Pointer.Capture(null);
            ViewModel.Painter.EndStroke();
            ViewModel.PaintFinished();
        }

        void AfterDraw((int x0, int y0, int x1, int y1)? dirty)
        {
            if (dirty.HasValue) DrawRegions(dirty.Value.x0, dirty.Value.y0, dirty.Value.x1, dirty.Value.y1);
        }

        /// <summary>Brush outline at the mouse (canvas coordinates).</summary>
        void UpdateBrush(Point canvas)
        {
            if (!ViewModel.Painter.Active) { BrushCircle.IsVisible = false; return; }
            double d = (2 * ViewModel.Painter.Radius + 1) * Zoom;
            double scale = Math.Max(0.01, CurrentMatrix.M11);
            BrushCircle.Width = BrushCircle.Height = d;
            BrushCircle.StrokeThickness = Math.Max(1.5, 2 / scale);
            double cx = (Math.Floor(canvas.X / Zoom) + 0.5) * Zoom, cy = (Math.Floor(canvas.Y / Zoom) + 0.5) * Zoom;
            Canvas.SetLeft(BrushCircle, cx - d / 2);
            Canvas.SetTop(BrushCircle, cy - d / 2);
            BrushCircle.IsVisible = true;
        }

        /// <summary>Colors of the overlay (premultiplied BGRA): painted ground is more opaque than the computed one.</summary>
        static uint RegionColor(GeoRegion region, bool painted)
        {
            (byte r, byte g, byte b, byte a) c = region switch
            {
                GeoRegion.Seabed => (40, 100, 230, painted ? (byte)185 : (byte)120),
                GeoRegion.Uplift => (170, 110, 50, painted ? (byte)195 : (byte)135),
                GeoRegion.Volcanic => (235, 45, 30, (byte)190),
                _ => (50, 190, 70, painted ? (byte)120 : (byte)0),
            };
            return (uint)(c.a << 24 | (c.r * c.a / 255) << 16 | (c.g * c.a / 255) << 8 | (c.b * c.a / 255));
        }

        void DrawRegions(int x0, int y0, int x1, int y1)
        {
            if (_regionBmp == null) return;
            var map = ViewModel.Regions;
            x0 = Math.Clamp(x0, 0, RegionMap.Width - 1); x1 = Math.Clamp(x1, 0, RegionMap.Width - 1);
            y0 = Math.Clamp(y0, 0, RegionMap.Height - 1); y1 = Math.Clamp(y1, 0, RegionMap.Height - 1);
            using (var fb = _regionBmp.Lock())
            {
                int stride = fb.RowBytes / 4;
                unsafe
                {
                    uint* basePtr = (uint*)fb.Address;
                    for (int y = y0; y <= y1; ++y)
                        for (int x = x0; x <= x1; ++x)
                        {
                            int i = y * RegionMap.Width + x;
                            uint col = 0;
                            if (RegionMap.InFace(x, y))
                            {
                                bool painted = map.Paint[i] != 0;
                                col = RegionColor(map.RegionAtIndex(i), painted);
                            }
                            basePtr[y * stride + x] = col;
                        }
                }
            }
            RegionImage.InvalidateVisual();
        }

        const string StatusHint = "Left mouse: move the map   Wheel: zoom   Right click: 3D view of the ore at the violet square   Ctrl+0: fit   Ctrl+1: 100%";
        bool _fitPending;

        /// <summary>Scales and centers the whole map (4 x 3 tiles of 2048) in the view. False if the view has no size yet.</summary>
        internal bool FitToWindow()
        {
            double bw = BorderView.Bounds.Width, bh = BorderView.Bounds.Height;
            if (bw < 10 || bh < 10) return false;
            double sc = Math.Min(bw / ImageView.Width, bh / ImageView.Height);
            ImageView.RenderTransform = new MatrixTransform(new Matrix(sc, 0, 0, sc, (bw - ImageView.Width * sc) / 2, (bh - ImageView.Height * sc) / 2));
            UpdateOreInspectRect(new Point(-1, -1));
            return true;
        }

        void ZoomActual()
        {
            ImageView.RenderTransform = new MatrixTransform(Matrix.Identity);
            UpdateOreInspectRect(new Point(-1, -1));
        }

        private void Fit_Click(object sender, RoutedEventArgs e) => FitToWindow();
        private void Actual_Click(object sender, RoutedEventArgs e) => ZoomActual();

        /// <summary>Tile layout of the map: which face is under a canvas point, and the pixel in it (scaled to the tile width).</summary>
        internal bool TryFace(double cx, double cy, out CubeMapFace face, out int px, out int py)
        {
            face = CubeMapFace.Up; px = py = 0;
            int col = (int)Math.Floor(cx / 2048), row = (int)Math.Floor(cy / 2048);
            if (cx < 0 || cy < 0) return false;
            if (row == 0 && col == 0) face = CubeMapFace.Up;
            else if (row == 1 && col == 0) face = CubeMapFace.Front;
            else if (row == 1 && col == 1) face = CubeMapFace.Right;
            else if (row == 1 && col == 2) face = CubeMapFace.Back;
            else if (row == 1 && col == 3) face = CubeMapFace.Left;
            else if (row == 2 && col == 2) face = CubeMapFace.Down;
            else return false;
            double scale = ViewModel.TileWidth / 2048.0;
            px = (int)((cx - col * 2048) * scale);
            py = (int)((cy - row * 2048) * scale);
            return true;
        }

        void UpdateStatus(Point canvasPoint)
        {
            if (ViewModel.SelectedPlanetDefinition == null || !TryFace(canvasPoint.X, canvasPoint.Y, out var face, out int x, out int y))
            {
                StatusText.Text = StatusHint;
                return;
            }
            var text = ViewModel.DescribePixel(face, x, y);
            StatusText.Text = string.IsNullOrEmpty(text) ? StatusHint : text;
        }

        void RebuildRecentMenu()
        {
            var items = new List<Control>();
            foreach (var path in AppSettings.RecentFiles.Where(File.Exists))
            {
                var dir = Path.GetDirectoryName(path) ?? "";
                var parts = dir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                var where = parts.Length > 0 ? string.Join("/", parts.TakeLast(2)) : dir;
                var p = path;
                var mi = new MenuItem { Header = Path.GetFileName(path) + "   (" + where + ")" };
                mi.Click += (_, _) => ViewModel.OpenPlanetDefinition(p);
                items.Add(mi);
            }
            if (items.Count == 0) items.Add(new MenuItem { Header = "(none yet)", IsEnabled = false });
            RecentMenu.ItemsSource = items;
        }

        static IEnumerable<string> DroppedPaths(DragEventArgs e)
        {
            try
            {
                return e.DataTransfer.TryGetFiles()?.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToList() ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = DroppedPaths(e).Any(IsPlanetFile) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            HandleDroppedFiles(DroppedPaths(e));
            e.Handled = true;
        }

        static bool IsPlanetFile(string path) => path.EndsWith(".sbc", StringComparison.OrdinalIgnoreCase);

        /// <summary>Opens the first dropped .sbc file as planet definition.</summary>
        internal void HandleDroppedFiles(IEnumerable<string> paths)
        {
            var sbc = paths.FirstOrDefault(IsPlanetFile);
            if (sbc == null)
            {
                MessageBox.Show("Drop a planet definition file (.sbc) here, for example PlanetGeneratorDefinitions.sbc.", "Not a planet definition");
                return;
            }
            ViewModel.OpenPlanetDefinition(sbc);
            RebuildRecentMenu();
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
                && ViewModel.SelectedPlanetDefinition != null && !ViewModel.Painter.Active)
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

            var win = BuildOreView(face, x, y);
            win?.ShowDialogBlocking(this);
        }

        /// <summary>The 3D view of the ore under the inspector square whose top left corner is pixel (x, y) of the face. Null if there is no data.</summary>
        internal Ore3dView BuildOreView(CubeMapFace face, int x, int y)
        {
            // Copy pixels into matrix
            var width = (int)(OreInspectorRect.Width + .5);
            var height = (int)(OreInspectorRect.Height + .5);
            OreMapping[,] oreMap = new OreMapping[width, height];
            double[,] heightMeters = null;

            var info = ViewModel.GetInfo(face, CancellationToken.None);
            if (info == null) return null;
            // Terrain from the height map: meters above the lowest terrain (relief = radius x hill range of the planet definition;
            // without hill parameters an assumed 3 % of the radius). Pixel size on the planet gives the proportions of the scene.
            var water = ViewModel.Water;
            double radius = water?.PlanetRadius > 0 ? water.PlanetRadius : 60000;
            double relief = water != null && water.HasUsableHills ? radius * (water.HillMax - water.HillMin) : radius * 0.03;
            double pixelMeters = radius * Math.Sqrt(2 * Math.PI / 3) / Math.Max(1, ViewModel.TileWidth);
            if (info.HasHeightMap && info.H16 != null)
            {
                heightMeters = new double[width, height];
                for (int xx = 0; xx < width; ++xx)
                    for (int yy = 0; yy < height; ++yy)
                    {
                        int hx = xx + x, hy = yy + y;
                        heightMeters[xx, yy] = hx >= 0 && hx < info.H16.GetLength(0) && hy >= 0 && hy < info.H16.GetLength(1)
                            ? relief * info.H16[hx, hy] / 65535.0 : double.NaN;
                    }
            }
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
            win.ViewModel.AddCuboids(oreMap, heightMeters, pixelMeters);
            return win;
        }

        bool _moved = false;
        private void ImageView_MouseMove(object sender, PointerEventArgs e)
        {
            if (_painting) { PaintMove(e); return; }
            if (ViewModel.Painter.Active) UpdateBrush(e.GetPosition(ImageView));
            if (e.Pointer.Captured != BorderView)
            {
                UpdateOreInspectRect(e.GetPosition(BorderView));
                UpdateStatus(e.GetPosition(ImageView));
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
            if (_painting && e.InitialPressMouseButton == MouseButton.Left) { EndPaint(e); return; }
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                if (e.Pointer.Captured == BorderView) return;
                OpenOreInspector();
                return;
            }
            if (e.InitialPressMouseButton == MouseButton.Left || e.InitialPressMouseButton == MouseButton.Middle)
            {
                e.Pointer.Capture(null);
                _moved = false;
            }
        }

        private void ImageView_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(BorderView).Properties;
            if (ViewModel.Painter.Active && props.IsLeftButtonPressed) { BeginPaint(e); return; }
            if (!props.IsLeftButtonPressed && !props.IsMiddleButtonPressed) return;
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
