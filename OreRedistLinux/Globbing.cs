using System;
using System.Linq;
using SpaceEngineersToolsShared;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// "Globbing": ores tend to occur around other ore of the same kind, so there are regions where an ore is common
    /// and regions where it is rare. Every ore gets its own smooth random field over the planet (3-D value noise on
    /// the sphere, so it continues across the edges of the cube map). A new vein is accepted at a place with a chance
    /// that depends on the field and the ore's strength:
    ///   strength 0   : always (evenly spread, the original behavior)
    ///   strength 1   : only inside the globs (the top <c>coverage</c> share of the field)
    ///   in between   : a background chance of (1 - strength)^3 everywhere, plus the globs on top.
    /// A vein that is not accepted looks for another place, so the number of veins (the ore ratios) does not change,
    /// only where they are.
    /// </summary>
    internal class GlobField
    {
        readonly uint _seed;
        readonly double _scale;      // lattice cells per meter * planet radius: 1 / glob size on the unit sphere
        readonly double _threshold;  // field value above which a place counts as "in a glob"
        readonly double _width;      // softness of the glob edge
        readonly double[] _sorted;   // sampled field values, for Rank()

        /// <param name="seed">per ore</param>
        /// <param name="planetRadius">m</param>
        /// <param name="globSize">m, size of the globs (distance between the lattice points of the noise)</param>
        /// <param name="coverage">share of the planet that is inside globs, 0..1</param>
        public GlobField(uint seed, double planetRadius, double globSize, double coverage)
        {
            _seed = seed;
            _scale = planetRadius / Math.Max(1, globSize);

            // Find the field value that is exceeded on 'coverage' of the sphere
            var rnd = new Random(12345);
            var samples = new double[20000];
            for (int i = 0; i < samples.Length; ++i)
            {
                double z = rnd.NextDouble() * 2 - 1, phi = rnd.NextDouble() * 2 * Math.PI, r = Math.Sqrt(1 - z * z);
                samples[i] = Value(r * Math.Cos(phi), r * Math.Sin(phi), z);
            }
            Array.Sort(samples);
            _sorted = samples;
            coverage = Math.Clamp(coverage, 0.005, 0.9);
            _threshold = samples[(int)Math.Clamp((1 - coverage) * samples.Length, 0, samples.Length - 1)];
            double mean = samples.Average();
            double sd = Math.Sqrt(samples.Sum(v => (v - mean) * (v - mean)) / samples.Length);
            _width = Math.Max(0.01, 0.25 * sd);
        }

        /// <summary>Field value 0..1 at a point of the unit sphere.</summary>
        public double Value(double x, double y, double z)
        {
            double sx = x * _scale, sy = y * _scale, sz = z * _scale;
            double n1 = Noise(sx, sy, sz, _seed);
            double n2 = Noise(sx * 2 + 17.3, sy * 2 + 41.7, sz * 2 + 5.1, _seed ^ 0x9E3779B9u);
            return (n1 + 0.5 * n2) / 1.5;
        }

        /// <summary>
        /// Where a field value stands among all places on the planet, 0..1 (evenly distributed): turns the bell shaped
        /// noise into something that can be cut into shares, e.g. the rock types of a region.
        /// </summary>
        public double Rank(double fieldValue)
        {
            int lo = 0, hi = _sorted.Length;
            while (lo < hi) { int mid = (lo + hi) >> 1; if (_sorted[mid] <= fieldValue) lo = mid + 1; else hi = mid; }
            return lo / (double)_sorted.Length;
        }

        /// <summary>Chance (0..1) that a vein is accepted where the field has this value.</summary>
        public double Acceptance(double fieldValue, double strength)
        {
            if (strength <= 0) return 1;
            strength = Math.Min(strength, 1);
            double t = Math.Clamp((fieldValue - (_threshold - _width)) / (2 * _width), 0, 1);
            t = t * t * (3 - 2 * t); // smoothstep: 0 outside the globs, 1 inside
            // Chance outside the globs: falls off quickly with the strength so the middle of the scale is already visible
            // (0.25: outside is 2.4x less likely than inside, 0.5: 8x, 0.75: 64x, 1: never).
            double background = Math.Pow(1 - strength, 3);
            return background + (1 - background) * t;
        }

        static double Lattice(int x, int y, int z, uint seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2147483647u + seed * 1274126177u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h / 4294967295.0;
            }
        }

        static double Fade(double t) => t * t * (3 - 2 * t);

        static double Noise(double x, double y, double z, uint seed)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y), zi = (int)Math.Floor(z);
            double fx = Fade(x - xi), fy = Fade(y - yi), fz = Fade(z - zi);
            double c000 = Lattice(xi, yi, zi, seed), c100 = Lattice(xi + 1, yi, zi, seed);
            double c010 = Lattice(xi, yi + 1, zi, seed), c110 = Lattice(xi + 1, yi + 1, zi, seed);
            double c001 = Lattice(xi, yi, zi + 1, seed), c101 = Lattice(xi + 1, yi, zi + 1, seed);
            double c011 = Lattice(xi, yi + 1, zi + 1, seed), c111 = Lattice(xi + 1, yi + 1, zi + 1, seed);
            double x00 = c000 + (c100 - c000) * fx, x10 = c010 + (c110 - c010) * fx;
            double x01 = c001 + (c101 - c001) * fx, x11 = c011 + (c111 - c011) * fx;
            double y0 = x00 + (x10 - x00) * fy, y1 = x01 + (x11 - x01) * fy;
            return y0 + (y1 - y0) * fz;
        }

        /// <summary>Same on every run and machine (string.GetHashCode is not).</summary>
        public static uint StableHash(string text, int seed)
        {
            unchecked
            {
                uint h = 2166136261u ^ (uint)seed;
                foreach (char c in text ?? "") { h ^= c; h *= 16777619u; }
                return h;
            }
        }
    }

    /// <summary>
    /// Ore regions for the geology mode ("provinces"): like a glob, but the place of the region also follows the rock. The score of a place is the
    /// glob noise (rank 0..1) times a rock factor, (1 - affinity) + affinity * suitability, where suitability is how well the rocks there suit the ore
    /// (0..1). The provinces are the places with the highest scores (the top <c>coverage</c> share), so with a high affinity they lie where the ore's
    /// rock is, and with 0 the rock plays no part (a plain glob). The rock still counts for the vein itself, a province only makes a place more likely.
    /// </summary>
    internal class ProvinceField
    {
        readonly GlobField _noise;
        readonly double _affinity, _threshold, _width;

        public ProvinceField(GlobField noise, double affinity, double coverage, int tileWidth, int seed, Func<CubeMapFace, int, int, double> suitability)
        {
            _noise = noise;
            _affinity = Math.Clamp(affinity, 0, 1);
            var rnd = new Random(seed ^ 0x5bd1e995);
            var sc = new double[30000];
            for (int i = 0; i < sc.Length; ++i)
            {
                var face = (CubeMapFace)rnd.Next(6); int x = rnd.Next(tileWidth), y = rnd.Next(tileWidth);
                sc[i] = Score(face, x, y, tileWidth, suitability(face, x, y));
            }
            Array.Sort(sc);
            coverage = Math.Clamp(coverage, 0.005, 0.9);
            _threshold = Math.Max(1e-6, sc[(int)Math.Clamp((1 - coverage) * sc.Length, 0, sc.Length - 1)]);
            double mean = sc.Average();
            double sd = Math.Sqrt(sc.Sum(v => (v - mean) * (v - mean)) / sc.Length);
            _width = Math.Max(0.005, 0.25 * sd);
        }

        public double Score(CubeMapFace face, int x, int y, int tileWidth, double suitability)
        {
            var p = CoordinateHelper.GetNormalizedSphereCoordinates(face, x, y, tileWidth);
            double rank = _noise.Rank(_noise.Value(p.X, p.Y, p.Z));
            return rank * ((1 - _affinity) + _affinity * Math.Clamp(suitability, 0, 1));
        }

        /// <summary>Chance (0..1) that a vein is accepted at a place with this score (same shape as GlobField.Acceptance).</summary>
        public double Acceptance(double score, double strength)
        {
            if (strength <= 0) return 1;
            strength = Math.Min(strength, 1);
            double t = Math.Clamp((score - (_threshold - _width)) / (2 * _width), 0, 1);
            t = t * t * (3 - 2 * t);
            double background = Math.Pow(1 - strength, 3);
            return background + (1 - background) * t;
        }
    }
}
