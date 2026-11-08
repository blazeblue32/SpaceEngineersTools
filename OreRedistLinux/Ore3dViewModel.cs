using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceEngineersToolsShared;

namespace SpaceEngineersOreRedistribution
{
    public class Ore3dViewModel : PropChangeNotifier
    {
        public ObservableCollection<Cuboid> Cuboids { get; } = new ObservableCollection<Cuboid>();

        int _rectSize = 20;
        public int RectSize
        {
            get => _rectSize;
            set => SetProp(ref _rectSize, value);
        }


        double _radius = 120;
        public double Radius
        {
            get => _radius;
            set
            {
                if (SetProp(ref _radius, value))
                {
                    UpdateCamera();
                }
            }
        }

        double _longitude = 270;
        public double Longitude
        {
            get => _longitude;
            set
            {
                if (SetProp(ref _longitude, value))
                {
                    UpdateCamera();
                }
            }
        }

        double _latitude = 45;
        public double Latitude
        {
            get => _latitude;
            set
            {
                if (SetProp(ref _latitude, value))
                {
                    UpdateCamera();
                }
            }
        }

        double _zOffset = 0;
        public double ZOffset
        {
            get => _zOffset;
            set
            {
                if (SetProp(ref _zOffset, value))
                {
                    UpdateCamera();
                }
            }
        }

        void UpdateCamera()
        {
            // Assume center of spere is ground center
            My3dHelper.CalculateCamera(Radius, Longitude, Latitude, ZOffset, RectSize, out var direction, out var position);
            CamDir = direction;
            CamPos = new Point3D(position.X, position.Y, position.Z);
        }

        double _camX = 40; double _camY; double _camZ;
        public double CamX
        {
            get => _camX;
            set => SetProp(ref _camX, value);
        }
        public double CamY
        {
            get => _camY;
            set => SetProp(ref _camY, value);
        }
        public double CamZ
        {
            get => _camZ;
            set => SetProp(ref _camZ, value);
        }

        double _camDistX = 50;
        public double CamDistX
        {
            get => _camDistX;
            set
            {
                if (SetProp(ref _camDistX, value))
                {
                    CamPos = new Point3D(value, CamDistY, CamDistZ);
                }
            }
        }

        double _camDistY = 40;
        public double CamDistY
        {
            get => _camDistY;
            set
            {
                if (SetProp(ref _camDistY, value))
                {
                    CamPos = new Point3D(CamDistX, value, CamDistZ);
                }
            }
        }

        double _camDistZ = 60;
        public double CamDistZ
        {
            get => _camDistZ;
            set
            {
                if (SetProp(ref _camDistZ, value))
                {
                    CamPos = new Point3D(CamDistX, CamDistY, value);
                }
            }
        }

        double _camDirX = 0;
        public double CamDirX
        {
            get => _camDirX;
            set
            {
                if (SetProp(ref _camDirX, value))
                {
                    CamDir = new Vector3D(value, CamDirY, CamDirZ);
                }
            }
        }

        double _camDirY = 0;
        public double CamDirY
        {
            get => _camDirY;
            set
            {
                if (SetProp(ref _camDirY, value))
                {
                    CamDir = new Vector3D(CamDirX, value, CamDirZ);
                }
            }
        }

        double _camDirZ = -1;
        public double CamDirZ
        {
            get => _camDirZ;
            set
            {
                if (SetProp(ref _camDirZ, value))
                {
                    CamDir = new Vector3D(CamDirX, CamDirY, value);
                }
            }
        }

        Vector3D _camDir = new Vector3D(0, 0, -1);
        public Vector3D CamDir
        {
            get => _camDir;
            set => SetProp(ref _camDir, value);
        }


        Point3D _camPos = new Point3D(50,40,120);
        public Point3D CamPos
        {
            get => _camPos;
            set => SetProp(ref _camPos, value);
        }

        public Ore3dViewModel()
        {
            UpdateCamera();
        }

        /// <summary>Terrain height in meters for every cell of the view (relative to the mean height), null = flat plane.</summary>
        public double[,] Heights { get; private set; }

        /// <summary>Scene units per meter (the horizontal size of a cell is BaseUnit units, so this keeps the proportions of the real ground).</summary>
        public double VerticalUnitsPerMeter { get; private set; } = 0.1;

        /// <summary>Text for the window: where the terrain comes from and how high it is.</summary>
        public string TerrainNote { get; private set; } = "";

        /// <param name="oreMap">ore mapping per cell (null = no ore)</param>
        /// <param name="heightMeters">terrain height in meters per cell from the height map (NaN = unknown); null = flat</param>
        /// <param name="pixelMeters">size of a map pixel on the planet in meters, sets the proportions of the scene (0 = assume 50 m)</param>
        public void AddCuboids(OreMapping[,] oreMap, double[,] heightMeters = null, double pixelMeters = 0)
        {
            Cuboids.Clear();
            int w = oreMap.GetLength(0), h = oreMap.GetLength(1);
            VerticalUnitsPerMeter = pixelMeters > 0 ? My3dHelper.BaseUnit / pixelMeters : 0.1;

            double mean = 0; int n = 0; double min = double.MaxValue, max = double.MinValue;
            if (heightMeters != null && heightMeters.GetLength(0) == w && heightMeters.GetLength(1) == h)
            {
                foreach (var v in heightMeters) if (!double.IsNaN(v)) { mean += v; ++n; min = Math.Min(min, v); max = Math.Max(max, v); }
            }
            if (n > 0)
            {
                mean /= n;
                Heights = new double[w, h];
                for (int x = 0; x < w; ++x) for (int y = 0; y < h; ++y) Heights[x, y] = double.IsNaN(heightMeters[x, y]) ? 0 : heightMeters[x, y] - mean;
                TerrainNote = $"Terrain from the height map: {max - min:0} m of relief in this view, ore depths are measured from the surface.";
                // a high relief needs the camera further away to keep the whole terrain in view
                double reliefUnits = (max - min) * VerticalUnitsPerMeter;
                if (reliefUnits > RectSize * My3dHelper.BaseUnit * 0.3) Radius = Math.Max(Radius, RectSize * My3dHelper.BaseUnit * 1.2 + reliefUnits * 0.5);
            }
            else { Heights = null; TerrainNote = "No height map: flat ground."; }

            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    if (oreMap[x, y] == null) continue;
                    var map = oreMap[x, y];
                    // Depth & Start are in meters
                    // Base unit is 50m
                    var start = map.Start / 50.0;
                    var depth = map.Depth / 50.0;
                    Cuboids.Add(My3dHelper.CreateCuboid(x, h - y, start, depth, map.MapRgbValue, Heights != null ? Heights[x, y] : 0));
                }
            }
        }
    }
}
