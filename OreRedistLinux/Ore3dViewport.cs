using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// Small software 3D renderer for the ore inspector. WPF had Viewport3D, Avalonia has nothing
    /// comparable, and the scene (translucent boxes + one plane) is simple enough to paint directly:
    /// perspective projection, near plane clipping, flat Lambert shading, painter's algorithm.
    /// </summary>
    public class Ore3dViewport : Control
    {
        public IReadOnlyList<Cuboid> Cuboids { get; set; } = Array.Empty<Cuboid>();
        public int RectSize { get; set; } = 20;
        public Point3D CamPos { get; set; } = new Point3D(50, 40, 120);
        public Vector3D CamDir { get; set; } = new Vector3D(0, 0, -1);
        public double FieldOfViewDeg { get; set; } = 60;

        // Same light setup as the WPF version
        static readonly Vector3D LightDir = Norm(new Vector3D(-0.2, -0.2, -0.5));
        const double Ambient = 80 / 255.0;

        static Vector3D Norm(Vector3D v) { v.Normalize(); return v; }

        class Poly
        {
            public Point3D[] Pts;
            public Vector3D Normal;
            public Color Color;
            public double Alpha;
            public double Depth;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var bounds = new Rect(Bounds.Size);
            context.FillRectangle(Brushes.White, bounds);
            if (bounds.Width < 2 || bounds.Height < 2) return;

            using var clip = context.PushClip(bounds);

            // Camera basis (Z is up)
            var f = CamDir; f.Normalize();
            var right = Vector3D.CrossProduct(f, new Vector3D(0, 0, 1)); right.Normalize();
            var up = Vector3D.CrossProduct(right, f);
            var cam = CamPos;

            // The WPF view was a 4:3 viewport with 60 degree horizontal FOV inside a Viewbox
            double viewW = Math.Min(bounds.Width, bounds.Height * 4.0 / 3.0);
            double focal = (viewW / 2) / Math.Tan(FieldOfViewDeg * Math.PI / 360);
            double cx = bounds.Width / 2, cy = bounds.Height / 2;
            const double near = 0.5;

            var polys = new List<Poly>(Cuboids.Count * 6);
            foreach (var c in Cuboids)
                AddCuboid(polys, c);

            // Camera space transform + depth for sorting
            foreach (var p in polys)
            {
                double d = 0;
                for (int i = 0; i < p.Pts.Length; ++i) d += ToCam(p.Pts[i]).Z;
                p.Depth = d / p.Pts.Length;
            }
            polys.Sort((a, b) => b.Depth.CompareTo(a.Depth)); // far to near

            Poly plane = BuildPlane();
            bool planeLast = cam.Z > 0.1; // plane is at z=0.1, all cuboids are below it
            if (!planeLast) DrawPoly(context, plane);
            foreach (var p in polys) DrawPoly(context, p);
            if (planeLast) DrawPoly(context, plane);

            (double X, double Y, double Z) ToCam(Point3D p)
            {
                var v = p - cam;
                return (Vector3D.DotProduct(v, right), Vector3D.DotProduct(v, up), Vector3D.DotProduct(v, f));
            }

            void DrawPoly(DrawingContext dc, Poly poly)
            {
                // Two sided: use the normal pointing to the viewer for shading
                var n = poly.Normal;
                var toCam = cam - poly.Pts[0];
                if (Vector3D.DotProduct(n, toCam) < 0) n = -n;
                double diffuse = Math.Max(0, Vector3D.DotProduct(n, -LightDir));
                double k = Math.Min(1, Ambient + diffuse);
                var col = Color.FromArgb((byte)(poly.Alpha + .5),
                    (byte)(poly.Color.R * k + .5), (byte)(poly.Color.G * k + .5), (byte)(poly.Color.B * k + .5));

                // Clip against near plane
                var camPts = poly.Pts.Select(ToCam).ToList();
                var clipped = new List<(double X, double Y, double Z)>();
                for (int i = 0; i < camPts.Count; ++i)
                {
                    var a = camPts[i];
                    var b = camPts[(i + 1) % camPts.Count];
                    bool ain = a.Z >= near, bin = b.Z >= near;
                    if (ain) clipped.Add(a);
                    if (ain != bin)
                    {
                        double t = (near - a.Z) / (b.Z - a.Z);
                        clipped.Add((a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, near));
                    }
                }
                if (clipped.Count < 3) return;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    for (int i = 0; i < clipped.Count; ++i)
                    {
                        var q = clipped[i];
                        var pt = new Point(cx + q.X / q.Z * focal, cy - q.Y / q.Z * focal);
                        if (i == 0) ctx.BeginFigure(pt, true);
                        else ctx.LineTo(pt);
                    }
                    ctx.EndFigure(true);
                }
                dc.DrawGeometry(new Avalonia.Media.Immutable.ImmutableSolidColorBrush(col), null, geo);
            }
        }

        Poly BuildPlane()
        {
            double s = RectSize * My3dHelper.BaseUnit;
            // Same winding as WPF code (p2, p0, p1 / p2, p1, p3), as one quad
            return new Poly
            {
                Pts = new[] { new Point3D(0, s, .1), new Point3D(0, 0, .1), new Point3D(s, 0, .1), new Point3D(s, s, .1) },
                Normal = new Vector3D(0, 0, 1),
                Color = Color.FromRgb(128, 128, 128),
                Alpha = 50,
            };
        }

        static void AddCuboid(List<Poly> list, Cuboid c)
        {
            var z1 = -1 * c.Start * My3dHelper.DepthUnit;
            var z2 = -1 * c.Start * My3dHelper.DepthUnit - c.Depth * My3dHelper.DepthUnit;
            var x1 = c.X * My3dHelper.BaseUnit;
            var x2 = x1 + My3dHelper.BaseUnit;
            var y1 = c.Y * My3dHelper.BaseUnit;
            var y2 = y1 + My3dHelper.BaseUnit;

            var p0 = new Point3D(x1, y1, z1); var p1 = new Point3D(x2, y1, z1);
            var p2 = new Point3D(x2, y2, z1); var p3 = new Point3D(x1, y2, z1);
            var p4 = new Point3D(x1, y1, z2); var p5 = new Point3D(x2, y1, z2);
            var p6 = new Point3D(x2, y2, z2); var p7 = new Point3D(x1, y2, z2);

            var color = c.Rgb != null ? Color.FromRgb(c.Rgb.R, c.Rgb.G, c.Rgb.B) : Color.FromRgb(0, 0, 128);
            void Quad(Point3D a, Point3D b, Point3D cc, Point3D d, Vector3D n) =>
                list.Add(new Poly { Pts = new[] { a, b, cc, d }, Normal = n, Color = color, Alpha = 128 });

            Quad(p0, p1, p2, p3, new Vector3D(0, 0, 1));   // top (z1)
            Quad(p4, p5, p6, p7, new Vector3D(0, 0, -1));  // bottom (z2)
            Quad(p0, p1, p5, p4, new Vector3D(0, -1, 0));
            Quad(p3, p2, p6, p7, new Vector3D(0, 1, 0));
            Quad(p0, p3, p7, p4, new Vector3D(-1, 0, 0));
            Quad(p1, p2, p6, p5, new Vector3D(1, 0, 0));
        }
    }
}
