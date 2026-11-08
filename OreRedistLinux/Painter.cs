using System;
using System.Collections.Generic;

namespace SpaceEngineersOreRedistribution
{
    public enum PaintTool { None = 0, Land = 1, Seabed = 2, Uplift = 3, Volcanic = 4, Erase = 5 }

    /// <summary>
    /// Brush, eraser and undo for the hand painted regions (RegionMap.Paint). A stroke is everything between pressing and
    /// releasing the mouse button; undo / redo work on whole strokes.
    /// </summary>
    internal class GeologyPainter
    {
        class Stroke { public readonly List<(int index, byte before, byte after)> Cells = new(); public readonly HashSet<int> Seen = new(); }

        public PaintTool Tool { get; set; } = PaintTool.None;
        /// <summary>Brush radius in cells of the region map (a cell is 4 x 4 map pixels at tile width 2048).</summary>
        public int Radius { get; set; } = 12;
        public bool Active => Tool != PaintTool.None;
        public const int MaxUndo = 60;

        readonly List<Stroke> _undo = new();
        readonly List<Stroke> _redo = new();
        Stroke _current;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public event Action HistoryChanged;

        byte Value => Tool switch
        {
            PaintTool.Land => 1, PaintTool.Seabed => 2, PaintTool.Uplift => 3, PaintTool.Volcanic => 4, _ => 0,
        };

        public void BeginStroke() { _current = new Stroke(); }

        /// <summary>Paints a disc around the cell. Returns the changed rectangle (x0,y0,x1,y1 inclusive) or null if nothing changed.</summary>
        public (int x0, int y0, int x1, int y1)? PaintAt(RegionMap map, int cx, int cy)
        {
            if (!Active) return null;
            _current ??= new Stroke();
            byte v = Value;
            int r = Math.Max(1, Radius), r2 = r * r;
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            for (int y = cy - r; y <= cy + r; ++y)
                for (int x = cx - r; x <= cx + r; ++x)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy > r2 || !RegionMap.InFace(x, y)) continue;
                    int i = y * RegionMap.Width + x;
                    byte old = map.Paint[i];
                    if (old == v) continue;
                    if (_current.Seen.Add(i)) _current.Cells.Add((i, old, v));
                    else
                    {
                        // painted again in the same stroke: keep the value from before the stroke
                        int k = _current.Cells.FindIndex(c => c.index == i);
                        _current.Cells[k] = (i, _current.Cells[k].before, v);
                    }
                    map.Paint[i] = v;
                    x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
                }
            return x0 == int.MaxValue ? null : (x0, y0, x1, y1);
        }

        /// <summary>Paints along a line (the mouse moves faster than events arrive).</summary>
        public (int x0, int y0, int x1, int y1)? PaintLine(RegionMap map, int ax, int ay, int bx, int by)
        {
            int steps = Math.Max(1, (int)(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay)) / Math.Max(1.0, Radius / 2.0)));
            (int x0, int y0, int x1, int y1)? total = null;
            for (int s = 0; s <= steps; ++s)
            {
                int x = ax + (bx - ax) * s / steps, y = ay + (by - ay) * s / steps;
                var d = PaintAt(map, x, y);
                if (d.HasValue) total = Union(total, d.Value);
            }
            return total;
        }

        public void EndStroke()
        {
            if (_current == null) return;
            var stroke = _current; _current = null;
            if (stroke.Cells.Count == 0) return;
            _undo.Add(stroke);
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
            _redo.Clear();
            HistoryChanged?.Invoke();
        }

        public (int x0, int y0, int x1, int y1)? Undo(RegionMap map)
        {
            if (_undo.Count == 0) return null;
            var s = _undo[^1]; _undo.RemoveAt(_undo.Count - 1); _redo.Add(s);
            HistoryChanged?.Invoke();
            return Apply(map, s, undo: true);
        }

        public (int x0, int y0, int x1, int y1)? Redo(RegionMap map)
        {
            if (_redo.Count == 0) return null;
            var s = _redo[^1]; _redo.RemoveAt(_redo.Count - 1); _undo.Add(s);
            HistoryChanged?.Invoke();
            return Apply(map, s, undo: false);
        }

        /// <summary>Removes all paint (as one stroke, so it can be undone).</summary>
        public (int x0, int y0, int x1, int y1)? ClearAll(RegionMap map)
        {
            var s = new Stroke();
            for (int i = 0; i < map.Paint.Length; ++i)
                if (map.Paint[i] != 0) { s.Cells.Add((i, map.Paint[i], (byte)0)); map.Paint[i] = 0; }
            if (s.Cells.Count == 0) return null;
            _undo.Add(s); if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
            _redo.Clear();
            HistoryChanged?.Invoke();
            return (0, 0, RegionMap.Width - 1, RegionMap.Height - 1);
        }

        public void ResetHistory() { _undo.Clear(); _redo.Clear(); _current = null; HistoryChanged?.Invoke(); }

        static (int x0, int y0, int x1, int y1)? Apply(RegionMap map, Stroke s, bool undo)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var (index, before, after) in s.Cells)
            {
                map.Paint[index] = undo ? before : after;
                int x = index % RegionMap.Width, y = index / RegionMap.Width;
                x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
            }
            return (x0, y0, x1, y1);
        }

        static (int x0, int y0, int x1, int y1) Union((int x0, int y0, int x1, int y1)? a, (int x0, int y0, int x1, int y1) b) =>
            a == null ? b : (Math.Min(a.Value.x0, b.x0), Math.Min(a.Value.y0, b.y0), Math.Max(a.Value.x1, b.x1), Math.Max(a.Value.y1, b.y1));
    }
}
