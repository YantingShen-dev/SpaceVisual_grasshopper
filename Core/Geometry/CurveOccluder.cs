using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace SpaceVisual.Core.Geometry
{
    /// <summary>
    /// 2D line-of-sight occluder. Wraps a set of obstacle Curves with an
    /// RTree over per-curve bounding boxes for fast Line-Curve queries.
    /// Used by SV_BuildGraph2D and SV_Isovist2D.
    /// </summary>
    public sealed class CurveOccluder
    {
        private readonly Curve[] _curves;
        private readonly RTree _tree;
        private readonly double _tolerance;

        public int CurveCount => _curves.Length;

        public CurveOccluder(IEnumerable<Curve> obstacles, double tolerance = 1e-6)
        {
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            _tolerance = tolerance;

            var collected = new List<Curve>();
            foreach (var c in obstacles)
            {
                if (c != null && c.IsValid) collected.Add(c);
            }
            _curves = collected.ToArray();

            _tree = new RTree();
            for (int i = 0; i < _curves.Length; i++)
            {
                var bb = _curves[i].GetBoundingBox(false);
                if (bb.IsValid) _tree.Insert(bb, i);
            }
        }

        /// <summary>
        /// True if the segment from→to is blocked (crosses any obstacle curve).
        /// Intersections within <paramref name="endpointEpsilon"/> of either endpoint
        /// are ignored so that endpoint coincidence doesn't register as blockage.
        /// </summary>
        public bool IsBlocked(Point3d from, Point3d to, double endpointEpsilon = 1e-4)
        {
            if (_curves.Length == 0) return false;

            double len = from.DistanceTo(to);
            if (len < endpointEpsilon * 2) return false;

            var searchBox = new BoundingBox(new[] { from, to });
            searchBox.Inflate(_tolerance + endpointEpsilon);

            var candidates = new List<int>();
            _tree.Search(searchBox, (sender, e) => candidates.Add(e.Id));
            if (candidates.Count == 0) return false;

            var line = new Line(from, to);
            foreach (var i in candidates)
            {
                var ix = Intersection.CurveLine(_curves[i], line, _tolerance, _tolerance);
                if (ix == null || ix.Count == 0) continue;

                for (int j = 0; j < ix.Count; j++)
                {
                    var evt = ix[j];
                    if (!evt.IsPoint) return true; // overlap counts as blocked

                    var p = evt.PointB;
                    if (p.DistanceTo(from) > endpointEpsilon &&
                        p.DistanceTo(to)   > endpointEpsilon)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Cast a ray from <paramref name="origin"/> in <paramref name="direction"/> up to
        /// <paramref name="maxDistance"/> and return the nearest curve intersection.
        /// Used by SV_Isovist2D to find the boundary of each isovist polygon.
        /// </summary>
        public bool TryNearestHit(
            Point3d origin, Vector3d direction, double maxDistance,
            out Point3d hit, out double distance)
        {
            hit = origin;
            distance = double.PositiveInfinity;
            if (_curves.Length == 0 || maxDistance <= 0) return false;

            var d = direction;
            if (!d.Unitize()) return false;

            var end = origin + d * maxDistance;
            var rayLine = new Line(origin, end);

            var searchBox = new BoundingBox(new[] { origin, end });
            searchBox.Inflate(_tolerance);

            var candidates = new List<int>();
            _tree.Search(searchBox, (sender, e) => candidates.Add(e.Id));
            if (candidates.Count == 0) return false;

            bool found = false;
            foreach (var i in candidates)
            {
                var ix = Intersection.CurveLine(_curves[i], rayLine, _tolerance, _tolerance);
                if (ix == null || ix.Count == 0) continue;

                for (int k = 0; k < ix.Count; k++)
                {
                    var evt = ix[k];
                    if (!evt.IsPoint) continue;
                    var p = evt.PointB;
                    double r = p.DistanceTo(origin);
                    if (r > 1e-9 && r < distance)
                    {
                        distance = r;
                        hit = p;
                        found = true;
                    }
                }
            }
            return found;
        }
    }
}
