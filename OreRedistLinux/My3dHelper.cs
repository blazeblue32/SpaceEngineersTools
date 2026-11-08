using SpaceEngineersToolsShared;
using System;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>One ore cell as a box. Units: x,y in cells; start/depth in 50m steps.</summary>
    public record Cuboid(int X, int Y, double Start, double Depth, RgbValue Rgb);

    public static class My3dHelper
    {
        public const double BaseUnit = 5;
        public const double DepthUnit = 5;

        public static Cuboid CreateCuboid(int x, int y, double start, double depth, RgbValue rgb)
            => new Cuboid(x, y, start, depth, rgb);

        public static Vector3D CalcNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }

        public static void CalculateCamera(double radius, double longitude, double latitude, double zOffset, double rectSize, out Vector3D direction, out Vector3D position)
        {
            Vector3D centerOfSphere = new Vector3D(BaseUnit * rectSize / 2, BaseUnit * rectSize / 2, zOffset);
            position = new Vector3D();

            position.Z = radius * Math.Sin(latitude * Math.PI / 180);
            // Z-Position other than 0 changes radius!
            radius = position.Z / Math.Tan(latitude * Math.PI / 180);

            position.X = radius * Math.Cos(longitude * Math.PI / 180);
            position.Y = radius * Math.Sin(longitude * Math.PI / 180);

            direction = position * -1;
            direction.Normalize();
            position += centerOfSphere;
        }
    }
}
