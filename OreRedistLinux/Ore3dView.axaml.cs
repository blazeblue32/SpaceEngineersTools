using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    public partial class Ore3dView : DialogWindow
    {
        public Ore3dViewModel ViewModel { get; } = new Ore3dViewModel();

        public Ore3dView()
        {
            DataContext = ViewModel;
            InitializeComponent();

            ViewModel.Cuboids.CollectionChanged += (_, _) => UpdateViewport();
            ViewModel.PropertyChanged += (_, _) => UpdateViewport();

            // Idea for mouse controls:
            // Camera is on virtual sphere:
            // - Camera always looks towards center
            // - Mouse Wheel controls sphere radius
            // - Mouse left right moves it longitude
            // - Mouse up down moves it latitude
            MyCanvas.PointerPressed += MyViewPort_MouseDown;
            MyCanvas.PointerReleased += MyViewPort_MouseUp;
            MyCanvas.PointerMoved += MyViewPort_MouseMove;
            MyCanvas.PointerWheelChanged += MyViewPort_MouseWheel;
            UpdateViewport();
        }

        void UpdateViewport()
        {
            var vp = MyViewPort;
            if (vp == null) return;
            vp.Cuboids = ViewModel.Cuboids.ToList();
            vp.RectSize = ViewModel.RectSize;
            vp.CamPos = ViewModel.CamPos;
            vp.CamDir = ViewModel.CamDir;
            vp.InvalidateVisual();
        }

        private void MyViewPort_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            e.Pointer.Capture(null);
            _dragCount = 0;
        }

        private void MyViewPort_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            // WPF delta for one wheel notch is 120, Avalonia reports 1.
            var delta = -e.Delta.Y * 120 / 20.0;
            var radius = ViewModel.Radius + delta;
            if (radius < 50) radius = 50;
            ViewModel.Radius = radius;
        }

        private void MyViewPort_MouseMove(object sender, PointerEventArgs e)
        {
            var canvas = (Control)sender;
            var props = e.GetCurrentPoint(canvas).Properties;
            if (props.IsLeftButtonPressed)
            {
                // There is a bug that causes first deltas to be too large
                if (_dragCount < 3)
                {
                    _previousPositionL = e.GetPosition(canvas);
                    ++_dragCount;
                    return;
                }

                // Panning
                var pos = e.GetPosition(canvas);
                var delta = pos - _previousPositionL;

                var lo = ViewModel.Longitude - delta.X / 2;
                var la = ViewModel.Latitude + delta.Y / 2;

                while (lo < 0 || lo > 360)
                {
                    if (lo < 0) lo += 360;
                    if (lo > 360) lo -= 360;
                }

                if (la < -89) la = -89;
                if (la > 89) la = 89;

                ViewModel.Longitude = lo;
                ViewModel.Latitude = la;

                _previousPositionL = pos;
            }
            else if (props.IsRightButtonPressed)
            {
                var pos = e.GetPosition(canvas);
                var delta = pos - _previousPositionR;
                if (_dragCount < 3)
                {
                    _previousPositionR = pos;
                    ++_dragCount;
                    return;
                }
                var zOff = ViewModel.ZOffset + delta.Y / 20;
                if (zOff > 0) zOff = 0;
                if (zOff < -50) zOff = -50;
                ViewModel.ZOffset = zOff;
                _previousPositionR = pos;
            }
        }

        Point _previousPositionL;
        Point _previousPositionR;
        int _dragCount = 0;
        private void MyViewPort_MouseDown(object sender, PointerPressedEventArgs e)
        {
            e.Pointer.Capture((Control)sender);
            _dragCount = 0;
            _previousPositionL = _previousPositionR = e.GetPosition((Control)sender);
        }

        public void SetRectSize(int size)
        {
            ViewModel.RectSize = size;
            ViewModel.Radius = size * 1.2 * My3dHelper.BaseUnit;
        }
    }
}
