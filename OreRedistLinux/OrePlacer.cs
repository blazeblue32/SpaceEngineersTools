using SpaceEngineersToolsShared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceEngineersOreRedistribution
{
    internal class PlacementContext
    {
        public RedistributionSetupViewModel Setup;
        public Dictionary<string, List<OreMapping>> Mappings;
        public int TileWidth;
        public bool IgnoreTiers;
        public WaterSettings Water;
        /// <summary>Water rules are used at all (switched on and usable).</summary>
        public bool UseWater;
    }

    /// <summary>Water related look-ups of one cube face: land pixels sorted by height, sea pixels, density bookkeeping.</summary>
    internal class FaceWater
    {
        public const int BlockShift = 7; // 128 x 128 pixel blocks for the density limit

        public readonly WaterSettings Water;
        public readonly ushort[,] H;
        public readonly int Width;
        public readonly int[] LandPixels;      // x * Width + y, highest first
        readonly int[] _landKeys;              // -height, ascending (so highest first)
        public readonly int[] SeaPixels;
        public readonly int BlocksPerRow;
        public readonly int[] LandInBlock;     // land pixels per block
        public readonly int[] LandDrawn;       // ore pixels drawn on land per block

        public FaceWater(WaterSettings water, ImageData source, int width)
        {
            Water = water; H = source.H16; Width = width;
            BlocksPerRow = ((width - 1) >> BlockShift) + 1;
            LandInBlock = new int[BlocksPerRow * BlocksPerRow];
            LandDrawn = new int[BlocksPerRow * BlocksPerRow];
            int land = 0;
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < width; ++y)
                    if (!water.IsUnderwater(H[x, y])) ++land;
            _landKeys = new int[land];
            LandPixels = new int[land];
            SeaPixels = new int[width * width - land];
            int l = 0, s = 0;
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < width; ++y)
                {
                    var h = H[x, y];
                    if (water.IsUnderwater(h)) SeaPixels[s++] = x * width + y;
                    else
                    {
                        _landKeys[l] = -h; LandPixels[l] = x * width + y; ++l;
                        ++LandInBlock[Block(x, y)];
                    }
                }
            Array.Sort(_landKeys, LandPixels);
        }

        public int Block(int x, int y) => (x >> BlockShift) * BlocksPerRow + (y >> BlockShift);
        public bool IsSea(int x, int y) => Water.IsUnderwater(H[x, y]);

        /// <summary>Number of land pixels with a height map value of at least minH16.</summary>
        public int LandAtLeast(int minH16)
        {
            // _landKeys ascending; pixels with -h <= -minH16
            int lo = 0, hi = _landKeys.Length; // first index with key > -minH16
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_landKeys[mid] <= -minH16) lo = mid + 1; else hi = mid;
            }
            return lo;
        }
    }

    /// <summary>
    /// The ore placement of the original tool (veins that random walk over the map and change layer on the way),
    /// moved out of the view model so it can run several times: once without water rules to get the reference
    /// (ratios and normal density), once for real, plus a top-up pass.
    ///
    /// Without water rules (or for ores set to "Anywhere") the random number draws are exactly those of the
    /// original code, so the same seed gives the same maps.
    ///
    /// With water rules:
    ///  - Tier first: a vein keeps the layer the dice chose and looks for a site where that layer is dry,
    ///    taken from the land pixels that are high enough (sorted by height), instead of being pushed to a shallow layer.
    ///  - Density: land blocks never get more than LandDensityLimit x the normal ore density. In Land + seabed mode
    ///    the vein goes to the seabed then; Land only ores lose the vein (reported).
    ///  - Veins prefer pixels that are still empty, so they do not paint over other ore.
    ///  - Top-up: what is still missing per ore and layer (compared to the reference) is placed afterwards on ground
    ///    where it is allowed (see TopUp).
    /// </summary>
    internal class OrePlacer
    {
        const int PoolTries = 80;

        readonly PlacementContext _c;
        readonly Random _rnd;
        readonly Normal _normal;
        readonly bool _rulesOn;
        readonly Dictionary<string, OreReportRow> _report;
        readonly Dictionary<CubeMapFace, FaceWater> _faces = new();
        readonly Dictionary<CubeMapFace, ImageData> _sources = new();

        /// <summary>Ore pixels per surface pixel without water rules (set before the real pass).</summary>
        public double ReferenceDensity;

        /// <summary>Cancels a long run (checked between columns / veins, never changes the random draws).</summary>
        public System.Threading.CancellationToken Token;

        /// <summary>Progress of the current step, 0..1.</summary>
        public Action<double> Progress;

        public OrePlacer(PlacementContext ctx, int seed, bool rulesOn, Dictionary<string, OreReportRow> report)
        {
            _c = ctx;
            _rnd = new Random(seed);
            _normal = new Normal(_rnd);
            _rulesOn = rulesOn && ctx.UseWater;
            _report = report;
        }

        WaterSettings W => _c.Water;

        static int GetTier(int d)
        {
            if (d <= 2) return 1;
            if (d <= 5) return 2;
            return 3;
        }

        OreWaterMode ModeOf(RedistributionSetupViewModel.OreInfo info) => _rulesOn ? info.WaterMode : OreWaterMode.Anywhere;

        static void Limits(RedistributionSetupViewModel.OreInfo info, IList<OreMapping> mappings, out int minIdx, out int maxIdx)
        {
            maxIdx = info.VeryDeepOre ? 9 : 8;
            if (info.TypicalDepthMax > -1 && info.TypicalDepthMax < maxIdx) maxIdx = info.TypicalDepthMax;
            minIdx = 0;
            if (info.TypicalDepthMin > -1 && info.TypicalDepthMin > minIdx) minIdx = info.TypicalDepthMin;
            // Layers switched off by hand at either end shorten the range (without such layers nothing changes)
            while (minIdx < maxIdx && mappings[minIdx].Value <= 0) ++minIdx;
            while (maxIdx > minIdx && mappings[maxIdx].Value <= 0) --maxIdx;
        }

        /// <summary>Is a layer inside the range switched off by hand? Then the random picks and the vein walk have to skip it.</summary>
        static bool HasGap(IList<OreMapping> mappings, int minIdx, int maxIdx)
        {
            for (int i = minIdx; i <= maxIdx && i < mappings.Count; ++i)
                if (mappings[i].Value <= 0) return true;
            return false;
        }

        /// <summary>Nearest layer that is switched on (ties go towards the vein's own layer).</summary>
        static int Snap(IList<OreMapping> mappings, int idx, int towards, int minIdx, int maxIdx)
        {
            if (idx >= 0 && idx < mappings.Count && mappings[idx].Value > 0) return idx;
            int dir = Math.Sign(towards - idx); if (dir == 0) dir = 1;
            for (int d = 1; d < 10; ++d)
                foreach (int cand in new[] { idx + dir * d, idx - dir * d })
                    if (cand >= minIdx && cand <= maxIdx && cand >= 0 && cand < mappings.Count && mappings[cand].Value > 0) return cand;
            return idx;
        }

        // ------------------------------------------------------------------------------------------------
        // One cube face

        public ImageData GenerateFace(ImageData source, CubeMapFace key)
        {
            int tw = _c.TileWidth;
            var image = new ImageData(key, tw);
            _sources[key] = source;
            if (_rulesOn && !_faces.ContainsKey(key)) _faces[key] = new FaceWater(W, source, tw);
            _faces.TryGetValue(key, out var fw);

            // Clear ores: Blue = 255
            System.Threading.Tasks.Parallel.For(0, tw, x =>
            {
                for (int y = 0; y < tw; ++y)
                {
                    image.R[x, y] = source.R[x, y];
                    image.G[x, y] = source.G[x, y];
                    image.B[x, y] = 255;
                }
            });

            var setup = _c.Setup;
            int spawnRate = setup.OreSpawnRate; // Default 3000
            if (setup.SmallPlanetMode) spawnRate *= 2;
            if (spawnRate < RedistributionSetupViewModel.MinSpawnRate) spawnRate = RedistributionSetupViewModel.MinSpawnRate;
            else if (spawnRate > RedistributionSetupViewModel.MaxSpawnRate) spawnRate = RedistributionSetupViewModel.MaxSpawnRate;

            double prop = 1.0 / spawnRate;
            double nBaseMin = 0.72; // This is just a random number I picked
            double nBaseMax = nBaseMin + prop;
            for (int x = 0; x < tw; ++x)
            {
                if ((x & 15) == 0)
                {
                    Token.ThrowIfCancellationRequested();
                    Progress?.Invoke(x / (double)tw);
                }
                for (int y = 0; y < tw; ++y)
                {
                    // Check if ore spawns at this location
                    var r = _rnd.NextDouble();
                    if (r < nBaseMin || r > nBaseMax) continue;

                    // Decide which ore is spawned
                    var info = setup.PickRandomOreWeighted(_rnd);
                    var mappings = _c.Mappings[info.Name];
                    Limits(info, mappings, out int minIdx, out int maxIdx);
                    int depth = RollDepth(info, mappings, minIdx, maxIdx);
                    if (depth == -1) continue;
                    int spawnSize = RollSize(info);

                    var row = _report[info.Name];
                    var mode = ModeOf(info);
                    ++row.Events;

                    int siteX = x, siteY = y, idx = depth;
                    if (mode != OreWaterMode.Anywhere)
                    {
                        if (!FindSite(fw, source, image, mode, mappings, x, y, depth, minIdx, spawnSize, ShareFor(info, mappings, depth), row, out siteX, out siteY, out idx, out bool sea))
                        {
                            ++row.Dropped;
                            continue;
                        }
                        if (sea) ++row.VeinsSea; else ++row.VeinsLand;
                    }
                    ++row.Placed;
                    DrawVein(image, fw, source, row, mode, mappings, siteX, siteY, idx, minIdx, maxIdx, spawnSize, info.LandShare, null);
                }
            }
            return image;
        }

        int RollDepth(RedistributionSetupViewModel.OreInfo info, IList<OreMapping> mappings, int minIdx, int maxIdx)
        {
            // Anything switched off by hand: only layers that are on may be rolled (also outside the depth limits' range).
            // Without such layers the original rolls are used unchanged.
            bool gaps = mappings.Any(m => !m.LayerEnabled);
            var setup = _c.Setup;
            var depthMin = info.TypicalDepthMin;
            var depthMax = info.TypicalDepthMax;
            int depth = -1;
            if (info.PreferredDepth > -1)
            {
                double[] depthWeights = new double[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                if (!info.VeryDeepOre) depthWeights[9] = 0;
                if (depthMin > 0)
                {
                    for (int i = 0; i < depthMin; ++i) depthWeights[i] = 0;
                }
                if (depthMax > -1)
                {
                    // Layer 9 is switched off by FinalizeList for every maximum below 9. The original loop started at 8,
                    // so with Very Deep Ore ticked layer 9 could still be picked and the vein was lost.
                    for (int i = AppSettings.LegacyVeryDeepRange ? 8 : 9; i > depthMax; --i) depthWeights[i] = 0;
                }
                if (gaps)
                    for (int i = 0; i < 10; ++i)
                        if (mappings[i].Value <= 0) depthWeights[i] = 0; // switched off by hand
                double weightSum = 0;
                for (int i = 0; i < 10; ++i)
                {
                    if (depthWeights[i] == 0) continue;
                    double stdDev = setup.StdDevDepth;
                    var divider = stdDev * Math.Sqrt(2 * Math.PI);
                    var eTerm = -0.5 * Math.Pow((i - info.PreferredDepth) / stdDev, 2);
                    depthWeights[i] = Math.Exp(eTerm) / divider;
                    weightSum += depthWeights[i];
                }
                var depthRnd = _rnd.NextDouble() * weightSum;
                weightSum = 0;
                for (int i = 0; i < 10; ++i)
                {
                    if (depthWeights[i] == 0) continue;
                    if (AppSettings.LegacyDepthWeighting)
                        weightSum += weightSum += depthWeights[i]; // original (buggy) double counting
                    else
                        weightSum += depthWeights[i];
                    if (depthRnd <= weightSum)
                    {
                        depth = i;
                        break;
                    }
                }
            }
            else if (gaps)
            {
                // some layers are switched off: pick among the ones that are on
                var on = new List<int>();
                for (int i = minIdx; i <= maxIdx; ++i) if (mappings[i].Value > 0) on.Add(i);
                depth = on.Count > 0 ? on[_rnd.Next(on.Count)] : -1;
            }
            else
            {
                if (depthMin == -1 && depthMax == -1)
                    depth = _rnd.Next(0, UpperBound(info.VeryDeepOre)); // upper bound is exclusive
                else if (depthMin == -1 && depthMax > -1)
                    depth = _rnd.Next(0, depthMax + 1);
                else if (depthMin >= -1 && depthMax == -1)
                    depth = _rnd.Next(depthMin, UpperBound(info.VeryDeepOre));
                else
                    depth = _rnd.Next(depthMin, depthMax + 1);
            }
            return depth;
        }

        static int UpperBound(bool veryDeepOre)
        {
            bool tenSlots = AppSettings.LegacyVeryDeepRange ? !veryDeepOre : veryDeepOre;
            return tenSlots ? 10 : 9;
        }

        int RollSize(RedistributionSetupViewModel.OreInfo info)
        {
            var setup = _c.Setup;
            var spawnSize = info.TypicalSize;
            if (setup.SmallPlanetMode) spawnSize *= 2;
            // Randomize if not set by user
            if (spawnSize == 0) spawnSize = _rnd.Next(5, 31);
            // Gauss randomizer
            if (setup.StdDev > 0)
            {
                var stdDevPercentage = setup.StdDev / 100.0;
                spawnSize = (int)(_normal.Next(spawnSize, spawnSize * stdDevPercentage) + 0.5);
                if (spawnSize < 1) spawnSize = 1; else if (spawnSize > 500) spawnSize = 500;
            }
            return spawnSize;
        }

        // ------------------------------------------------------------------------------------------------
        // Water rules: where may a vein start, which layer fits where

        /// <summary>Land share override for a layer: the layer's own value, else the ore's, else none (baseline of the Water window).</summary>
        static double? ShareFor(RedistributionSetupViewModel.OreInfo info, IList<OreMapping> mappings, int idx) =>
            mappings[idx].LandShare ?? info.LandShare;

        /// <summary>Dry depth the layer needs (its lower end). With AllowShallower the shallowest usable layer counts.</summary>
        double Need(IList<OreMapping> mappings, int idx, int minIdx)
        {
            if (!W.AllowShallower) return mappings[idx].Start + mappings[idx].Depth;
            double best = double.MaxValue;
            for (int i = Math.Max(minIdx, 0); i <= idx; ++i)
                if (mappings[i].Value > 0) best = Math.Min(best, mappings[i].Start + mappings[i].Depth);
            return best == double.MaxValue ? mappings[idx].Start + mappings[idx].Depth : best;
        }

        bool Free(ImageData image, int x, int y) => image.B[x, y] == 255;

        /// <summary>Room for a vein of this size in the block? (about 80% of a vein's pixels end up as new ore)</summary>
        bool UnderCap(FaceWater fw, int x, int y, int veinSize)
        {
            double limit = W.LandDensityLimit;
            if (limit <= 0 || ReferenceDensity <= 0) return true;
            int b = fw.Block(x, y);
            return fw.LandDrawn[b] + 0.8 * veinSize <= limit * ReferenceDensity * fw.LandInBlock[b];
        }

        /// <summary>Layer to use at a land pixel for a vein of layer idx, or -1 if it does not fit.</summary>
        int LandIndex(ushort h, IList<OreMapping> mappings, int idx, int minIdx)
        {
            double allowed = W.AllowedDepth(h);
            if (!W.AllowShallower) return (mappings[idx].Value > 0 && mappings[idx].Start + mappings[idx].Depth <= allowed) ? idx : -1;
            for (int i = idx; i >= Math.Max(minIdx, 0); --i)
                if (mappings[i].Value > 0 && mappings[i].Start + mappings[i].Depth <= allowed) return i;
            return -1;
        }

        /// <summary>
        /// Site for a new vein. Land + seabed: a vein that wants land but finds no fitting land with room
        /// goes to the seabed ("diverted").
        /// </summary>
        bool FindSite(FaceWater fw, ImageData source, ImageData image, OreWaterMode mode, IList<OreMapping> mappings,
            int diceX, int diceY, int idx, int minIdx, int veinSize, double? landShare, OreReportRow row,
            out int siteX, out int siteY, out int siteIdx, out bool siteIsSea)
        {
            siteX = diceX; siteY = diceY; siteIdx = idx; siteIsSea = false;
            if (mappings[idx].Value <= 0) return false;

            // Which kind of ground, in which order
            bool landOk = mode == OreWaterMode.AboveWater || mode == OreWaterMode.LandAndSeabed;
            bool seaOk = mode == OreWaterMode.UnderwaterOnly || mode == OreWaterMode.LandAndSeabed;
            bool landFirst = true;
            if (mode == OreWaterMode.LandAndSeabed)
            {
                if (landShare.HasValue)
                {
                    // Override of the ore or its layer: 100 = land only, 0 = seabed only, in between a preference
                    if (landShare.Value >= 100) seaOk = false;
                    else if (landShare.Value <= 0) landOk = false;
                    else landFirst = _rnd.NextDouble() * 100 < landShare.Value;
                    if (landShare.Value <= 0) landFirst = false;
                }
                else if (W.SeabedShare.HasValue) landFirst = !(_rnd.NextDouble() * 100 < W.SeabedShare.Value);
                else
                {
                    // by area, land counts LandPreference times: p(land) = f * land / (f * land + sea)
                    double fl = W.LandPreference * fw.LandPixels.Length, fs = fw.SeaPixels.Length;
                    landFirst = _rnd.NextDouble() < (fl + fs > 0 ? fl / (fl + fs) : 0.5);
                }
            }
            else if (mode == OreWaterMode.UnderwaterOnly) landFirst = false;

            double need = Need(mappings, idx, minIdx);
            for (int pass = 0; pass < 2; ++pass)
            {
                bool tryLand = pass == 0 ? landFirst : !landFirst;
                if (tryLand ? !landOk : !seaOk) continue;
                if (TrySiteInClass(fw, image, tryLand, mappings, diceX, diceY, idx, minIdx, need, veinSize, pass == 0, out siteX, out siteY, out siteIdx))
                {
                    siteIsSea = !tryLand;
                    if (mode == OreWaterMode.LandAndSeabed && pass == 1 && landFirst && !tryLand) ++row.Diverted;
                    return true;
                }
            }
            return false;
        }

        bool TrySiteInClass(FaceWater fw, ImageData image, bool land, IList<OreMapping> mappings, int diceX, int diceY,
            int idx, int minIdx, double need, int veinSize, bool tryDiceSite, out int siteX, out int siteY, out int siteIdx)
        {
            siteX = diceX; siteY = diceY; siteIdx = idx;
            int w = fw.Width;
            // the pixel the dice chose
            if (tryDiceSite && fw.IsSea(diceX, diceY) == !land)
            {
                if (land)
                {
                    int li = LandIndex(fw.H[diceX, diceY], mappings, idx, minIdx);
                    if (li >= 0 && UnderCap(fw, diceX, diceY, veinSize)) { siteIdx = li; return true; }
                }
                else return true;
            }
            if (land)
            {
                int k = fw.LandAtLeast(W.MinH16ForAllowed(need));
                if (k <= 0) return false;
                for (int t = 0; t < PoolTries; ++t)
                {
                    int p = fw.LandPixels[_rnd.Next(k)];
                    int x = p / w, y = p % w;
                    if (t < PoolTries / 2 && !Free(image, x, y)) continue; // prefer empty ground first
                    if (!UnderCap(fw, x, y, veinSize)) continue;
                    int li = LandIndex(fw.H[x, y], mappings, idx, minIdx);
                    if (li < 0) continue;
                    siteX = x; siteY = y; siteIdx = li; return true;
                }
                return false;
            }
            else
            {
                if (fw.SeaPixels.Length == 0) return false;
                for (int t = 0; t < PoolTries; ++t)
                {
                    int p = fw.SeaPixels[_rnd.Next(fw.SeaPixels.Length)];
                    int x = p / w, y = p % w;
                    if (t < PoolTries / 2 && !Free(image, x, y)) continue;
                    siteX = x; siteY = y; siteIdx = idx; return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Layer to draw at a pixel of a vein that is on its way (the vein keeps its own layer on land,
        /// only the small ups and downs of the walk are clamped), or -1 if the pixel cannot be used.
        /// </summary>
        int PixelIndex(FaceWater fw, OreWaterMode mode, IList<OreMapping> mappings, int x, int y, int lastIdx, int defaultIdx, int minIdx, double? oreShare)
        {
            var h = fw.H[x, y];
            bool sea = W.IsUnderwater(h);
            if (mode == OreWaterMode.LandAndSeabed)
            {
                // A layer set to 100 (land only) or 0 (seabed only) is also kept on that kind of ground pixel by pixel
                var share = mappings[lastIdx].LandShare ?? oreShare;
                if (share.HasValue && ((sea && share.Value >= 100) || (!sea && share.Value <= 0))) return -1;
            }
            if (sea) return (mode == OreWaterMode.AboveWater) ? -1 : lastIdx;
            if (mode == OreWaterMode.UnderwaterOnly) return -1;
            if (!W.AllowShallower)
            {
                // tier stable: the vein's own layer must fit here, otherwise the vein turns away from this pixel
                if (!(mappings[defaultIdx].Value > 0 && mappings[defaultIdx].Start + mappings[defaultIdx].Depth <= W.AllowedDepth(h))) return -1;
            }
            double allowed = W.AllowedDepth(h);
            for (int i = lastIdx; i >= Math.Max(minIdx, 0); --i)
            {
                var m = mappings[i];
                if (m.Value > 0 && m.Start + m.Depth <= allowed) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------------------------------------
        // The vein (walk of the original tool)

        /// <summary>Draws one vein into the image. placedPerValue (optional) counts the pixels written per value.</summary>
        void DrawVein(ImageData image, FaceWater fw, ImageData source, OreReportRow row, OreWaterMode mode, IList<OreMapping> mappings,
            int siteX, int siteY, int defaultDepthIndex, int minDepthIndex, int maxDepthIndex, int spawnSize, double? oreShare, long[] placedPerValue)
        {
            int tw = _c.TileWidth;
            bool gaps = HasGap(mappings, minDepthIndex, maxDepthIndex);
            HashSet<OreToDraw> drawnOre = new();
            drawnOre.Add(new OreToDraw(tw) { X = siteX, Y = siteY, Value = mappings[defaultDepthIndex].Value });
            int lastDepthIndex = defaultDepthIndex;
            int lastX = siteX;
            int lastY = siteY;

            // Vanilla ore scanner max range is 150m
            // Only index 6 is detectable from surface
            // Start there and fall down to default depth index
            bool dive = false;
            if (lastDepthIndex > 6 && minDepthIndex <= 6)
            {
                lastDepthIndex = 5; // We call ++ below to set it to 6
                dive = true;
            }

            // Draw ore
            // Strategy:
            // - Start at X,Y
            // - Select random neighbor and draw if not already drawn
            // - If all neighbors are already drawn, escape in straight random direction until a non drawn field is available
            // - While drawing
            // - If last drawn height is not starting height give a 30% to jump back to starting height. 20% Chance of getting further away.
            //   50% of staying at same height.
            int extraSteps = 0;
            for (int i = 1; i < spawnSize; ++i) // We already added one pixel at this point, so start with 1
            {
                var direction = _rnd.Next(4);
                var nextOreToDraw = new OreToDraw(tw);
                // Check if that pixel is already painted:
                int directionTries = 0;
                bool foundFreePixel = false;
                while (directionTries < 4)
                {
                    ++direction;
                    if (direction > 3) direction = 0;
                    // direction: 0 -> left, 1 -> up, 2 -> right, 3 -> down
                    int dx, dy;

                    // 10% chance of skipping over a pixel
                    var skipX = _rnd.NextDouble() < 0.1 ? 2 : 1;
                    var skipY = _rnd.NextDouble() < 0.1 ? 2 : 1;

                    GetDeltas(direction, out dx, out dy);
                    nextOreToDraw.X = lastX + dx * skipX;
                    nextOreToDraw.Y = lastY + dy * skipY;

                    if (!drawnOre.Contains(nextOreToDraw))
                    {
                        foundFreePixel = true;
                        break;
                    }
                    ++directionTries;
                }
                if (!foundFreePixel)
                {
                    // We're stuck. Get unstuck by going into random direction, brute force
                    int pleaseDontHang = 0;
                    int dx, dy;
                    GetDeltas(direction, out dx, out dy);
                    if (lastX < 500 && dx < 0) dx = 1;
                    if (lastY < 500 && dy < 0) dy = 1;
                    if (lastX > 1800 && dx > 0) dx = -1;
                    if (lastY > 1800 && dy > 0) dy = -1;
                    nextOreToDraw.X = lastX;
                    nextOreToDraw.Y = lastY;
                    while (pleaseDontHang++ < 1000)
                    {
                        nextOreToDraw.X += dx;
                        nextOreToDraw.Y += dy;
                        if (!drawnOre.Contains(nextOreToDraw))
                        {
                            foundFreePixel = true;
                            break;
                        }
                    }
                }
                if (!foundFreePixel) break; // Bye bye. Don't hammer your head against a wall

                int prevX = lastX, prevY = lastY; // for water rules: step back if this pixel cannot be used
                lastX = nextOreToDraw.X;
                lastY = nextOreToDraw.Y;
                drawnOre.Add(nextOreToDraw);
                var depthMod = _rnd.NextDouble();

                if (dive)
                {
                    lastDepthIndex++;
                    if (lastDepthIndex >= defaultDepthIndex)
                        dive = false;
                }
                else if (lastDepthIndex == defaultDepthIndex)
                {
                    if (depthMod >= 0.70) // Give a 70% chance of staying at the same height if height is starting height
                    {
                        // Flip to neighbor index
                        if (lastDepthIndex <= 0) lastDepthIndex++;
                        else if (lastDepthIndex >= maxDepthIndex) lastDepthIndex--;
                        else
                        {
                            // both directions possible
                            if (_rnd.NextDouble() > 0.6 || _c.IgnoreTiers)
                            {
                                // 40% chance of going up or down
                                if (_rnd.Next(2) == 0) lastDepthIndex--; else lastDepthIndex++;
                            }
                            else // 60% chance of staying in the same tier
                            {
                                var oldTier = GetTier(lastDepthIndex);
                                var tierDown = GetTier(lastDepthIndex + 1);
                                var tierUp = GetTier(lastDepthIndex - 1);
                                if (tierUp == oldTier && tierDown == oldTier)
                                {
                                    // We are in the middle of a tier. Randomly go up or down
                                    if (_rnd.Next(2) == 0) lastDepthIndex--; else lastDepthIndex++;
                                }
                                else if (tierDown != oldTier && tierUp == oldTier)
                                    lastDepthIndex--;
                                else if (tierDown == oldTier && tierUp != oldTier)
                                    lastDepthIndex++;
                                else
                                {   /* Same as random above. Reaching this shouldn't be possible though */
                                    if (_rnd.Next(2) == 0) lastDepthIndex--; else lastDepthIndex++;
                                }
                            }
                        }
                    }
                }
                // Give a 30% chance to jump back to starting height and 20% chance of getting further away
                else
                {
                    if (depthMod <= 0.3)
                    {
                        lastDepthIndex += Math.Sign(defaultDepthIndex - lastDepthIndex);
                    }
                    else if (depthMod <= 0.5) // 0.5-0.3 -> 20%
                    {
                        if (lastDepthIndex < defaultDepthIndex)
                            lastDepthIndex--;
                        else
                            lastDepthIndex++;
                    }
                    // else: 50% chance of staying
                }
                // Ignore limits while diving
                if (!dive)
                {
                    if (lastDepthIndex > maxDepthIndex) lastDepthIndex = maxDepthIndex;
                    else if (lastDepthIndex < minDepthIndex) lastDepthIndex = minDepthIndex;
                }

                // Layer to draw: the walk may land on a layer that is switched off, then the nearest one that is on is used
                int drawIdx = gaps ? Snap(mappings, lastDepthIndex, defaultDepthIndex, minDepthIndex, maxDepthIndex) : lastDepthIndex;

                // Get value for depth
                if (mode == OreWaterMode.Anywhere || fw == null)
                    nextOreToDraw.Value = mappings[drawIdx].Value;
                else
                {
                    int idxHere = PixelIndex(fw, mode, mappings, nextOreToDraw.X, nextOreToDraw.Y, drawIdx, defaultDepthIndex, minDepthIndex, oreShare);
                    if (idxHere < 0)
                    {
                        nextOreToDraw.Value = -1; ++row.SkippedPixels;
                        // The pixel stays marked as used, the vein steps back and goes on along the edge,
                        // so it still reaches its size (limited, so it cannot loop forever)
                        if (extraSteps < spawnSize * 4)
                        {
                            ++extraSteps;
                            lastX = prevX; lastY = prevY;
                            --i;
                        }
                    }
                    else
                    {
                        if (idxHere < drawIdx) ++row.ClampedPixels;
                        nextOreToDraw.Value = mappings[idxHere].Value;
                    }
                }
            }

            foreach (var drawn in drawnOre)
            {
                if (drawn.Value > 0 && drawn.Value < 256) // don't draw disabled entries which have value -1. See FinalizeList()
                {
                    if (fw != null && !fw.IsSea(drawn.X, drawn.Y) && image.B[drawn.X, drawn.Y] == 255)
                        ++fw.LandDrawn[fw.Block(drawn.X, drawn.Y)];
                    image.B[drawn.X, drawn.Y] = (byte)drawn.Value;
                    ++row.Pixels;
                    if (placedPerValue != null) ++placedPerValue[drawn.Value];
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Keeping the ratios: top-up

        public static long[] CountValues(ImageData image)
        {
            var counts = new long[256];
            int w = image.TileWidth;
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < w; ++y)
                    ++counts[image.B[x, y]];
            return counts;
        }

        /// <summary>
        /// Compares what the maps got with the reference (what the tool places without water rules, per ore and layer)
        /// and places extra veins of the missing ore and layer, on ground where the ore's water mode allows it and
        /// within the density limit. Land + seabed ores can use the seabed for this. What cannot be placed stays
        /// missing and shows up in the report.
        /// </summary>
        public void TopUp(Dictionary<CubeMapFace, ImageData> images, Dictionary<CubeMapFace, long[]> reference)
        {
            if (!_rulesOn) return;
            var faces = images.Keys.OrderBy(f => f).ToList();
            var achieved = new long[256];
            foreach (var f in faces) { var c = CountValues(images[f]); for (int v = 0; v < 256; ++v) achieved[v] += c[v]; }
            var target = new long[256];
            foreach (var f in faces) for (int v = 0; v < 256; ++v) target[v] += reference[f][v];

            int infoIndex = 0, infoCount = Math.Max(1, _c.Setup.OreInfos.Count);
            foreach (var info in _c.Setup.OreInfos)
            {
                Token.ThrowIfCancellationRequested();
                Progress?.Invoke(infoIndex++ / (double)infoCount);
                var mode = ModeOf(info);
                if (mode == OreWaterMode.Anywhere) continue;
                var mappings = _c.Mappings[info.Name];
                Limits(info, mappings, out int minIdx, out int maxIdx);
                var row = _report[info.Name];
                for (int i = minIdx; i <= maxIdx && i < mappings.Count; ++i)
                {
                    int v = mappings[i].Value;
                    if (v <= 0 || v > 254 || target[v] <= 0) continue;
                    long deficit = target[v] - achieved[v];
                    if (deficit < Math.Max(20, 0.03 * target[v])) continue;

                    var exhausted = new HashSet<CubeMapFace>();
                    for (int round = 0; round < 3 && deficit > 0; ++round)
                    {
                        var open = faces.Where(f => !exhausted.Contains(f) && reference[f][v] > 0).ToList();
                        if (open.Count == 0) break;
                        double share = open.Sum(f => (double)reference[f][v]);
                        long placedInRound = 0;
                        foreach (var f in open)
                        {
                            long want = (long)Math.Ceiling(deficit * reference[f][v] / share);
                            long got = TopUpFace(images[f], f, info, mappings, mode, i, minIdx, maxIdx, want, row);
                            achieved[v] += got; placedInRound += got;
                            if (got < want * 0.6) exhausted.Add(f);
                        }
                        deficit = target[v] - achieved[v];
                        if (placedInRound == 0) break;
                    }
                }
            }
        }

        long TopUpFace(ImageData image, CubeMapFace face, RedistributionSetupViewModel.OreInfo info, List<OreMapping> mappings,
            OreWaterMode mode, int layer, int minIdx, int maxIdx, long wantPixels, OreReportRow row)
        {
            var fw = _faces[face];
            var source = _sources[face];
            int tw = _c.TileWidth;
            int baseSize = Math.Max(5, info.TypicalSize * (_c.Setup.SmallPlanetMode ? 2 : 1));
            if (info.TypicalSize == 0) baseSize = 17;
            long placed = 0;
            int fails = 0;
            var counts = new long[256];
            for (int e = 0; e < 20000 && placed < wantPixels; ++e)
            {
                if ((e & 15) == 0) Token.ThrowIfCancellationRequested();
                int dx = _rnd.Next(tw), dy = _rnd.Next(tw);
                int size = RollSize(info);
                if (!FindSite(fw, source, image, mode, mappings, dx, dy, layer, minIdx, size, ShareFor(info, mappings, layer), row, out int sx, out int sy, out int idx, out bool sea))
                {
                    if (++fails >= 12) break; // no room for this ore and layer on this face
                    continue;
                }
                ++row.TopUpVeins;
                Array.Clear(counts, 0, counts.Length);
                DrawVein(image, fw, source, row, mode, mappings, sx, sy, idx, minIdx, maxIdx, size, info.LandShare, counts);
                long got = counts.Sum();
                placed += counts[mappings[layer].Value];
                row.TopUpPixels += got;
            }
            return placed;
        }

        // ------------------------------------------------------------------------------------------------
        // Report numbers

        /// <summary>Fills the per layer target and achieved numbers of the report rows.</summary>
        public void FillLayerStats(Dictionary<CubeMapFace, ImageData> images, Dictionary<CubeMapFace, long[]> reference)
        {
            var achieved = new long[256];
            var target = new long[256];
            foreach (var f in images.Keys)
            {
                var c = CountValues(images[f]);
                for (int v = 0; v < 256; ++v) { achieved[v] += c[v]; if (reference != null) target[v] += reference[f][v]; }
            }
            foreach (var info in _c.Setup.OreInfos)
            {
                var mappings = _c.Mappings[info.Name];
                var row = _report[info.Name];
                for (int i = 0; i < 10 && i < mappings.Count; ++i)
                {
                    int v = mappings[i].Value;
                    if (v <= 0 || v > 254) continue;
                    row.LayerAchieved[i] = achieved[v];
                    row.LayerTarget[i] = target[v];
                }
            }
        }

        /// <summary>Ore density on land and under water as multiple of the normal density, plus the fullest land block.</summary>
        public string DensityLine(Dictionary<CubeMapFace, ImageData> images)
        {
            if (!_rulesOn || ReferenceDensity <= 0) return "";
            long landOre = 0, seaOre = 0, landPx = 0, seaPx = 0; double worst = 0;
            foreach (var kv in images)
            {
                if (!_faces.TryGetValue(kv.Key, out var fw)) continue;
                int w = fw.Width;
                var perBlock = new int[fw.LandInBlock.Length];
                for (int x = 0; x < w; ++x)
                    for (int y = 0; y < w; ++y)
                    {
                        bool ore = kv.Value.B[x, y] != 255;
                        if (fw.IsSea(x, y)) { ++seaPx; if (ore) ++seaOre; }
                        else { ++landPx; if (ore) { ++landOre; ++perBlock[fw.Block(x, y)]; } }
                    }
                for (int b = 0; b < perBlock.Length; ++b)
                    if (fw.LandInBlock[b] >= 2000) worst = Math.Max(worst, perBlock[b] / (ReferenceDensity * fw.LandInBlock[b]));
            }
            string f(long a, long b) => b == 0 ? "n/a" : (a / (double)b / ReferenceDensity).ToString("0.00") + "x";
            return $"Ore density compared to normal ({ReferenceDensity * 100:0.00}% of the surface): land {f(landOre, landPx)}, seabed {f(seaOre, seaPx)}, " +
                   $"fullest land block {worst:0.00}x (limit {(W.LandDensityLimit > 0 ? W.LandDensityLimit.ToString("0.##") + "x" : "none")}).";
        }

        // ------------------------------------------------------------------------------------------------

        static void GetDeltas(int direction, out int dx, out int dy)
        {
            switch (direction)
            {
                case 0: dx = -1; dy = 0; return; // Left
                case 1: dx = 0; dy = -1; return; // Up
                case 2: dx = 1; dy = 0; return;  // Right
                case 3: dx = 0; dy = 1; return;  // Down
            }
            dx = 0; dy = 0;
        }
    }

    class OreToDraw
    {
        int _x, _y;
        int _tileWidth = 2048;
        public OreToDraw(int tileWidth) { _tileWidth = tileWidth; }

        public int X
        {
            get => _x;
            set
            {
                if (value < 0) _x = 0;
                else if (value > (_tileWidth - 1)) _x = (_tileWidth - 1);
                else _x = value;
            }
        }
        public int Y
        {
            get => _y;
            set
            {
                if (value < 0) _y = 0;
                else if (value > (_tileWidth - 1)) _y = (_tileWidth - 1);
                else _y = value;
            }
        }
        public int Value;

        public override bool Equals(object obj)
        {
            if (obj is not OreToDraw ore) return false;
            return X == ore.X && Y == ore.Y;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
    }
}
