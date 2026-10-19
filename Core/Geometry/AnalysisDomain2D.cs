using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace SpaceVisual.Core.Geometry
{
    /// <summary>Shared domain and contact policy for graph and isovist sampling.</summary>
    public sealed class AnalysisDomain2D
    {
        public const double Tolerance = 1e-6;
        private readonly IList<Curve> _boundaries;
        private readonly IList<Curve> _obstacles;
        private readonly Plane _plane;
        public List<Curve> Blocking { get; }

        public AnalysisDomain2D(IList<Point3d> points, IList<Curve> boundaries, IList<Curve> obstacles)
        {
            if (points.Count == 0) throw new ArgumentException("No points provided.");
            double z = points[0].Z;
            foreach (var p in points)
                if (!p.IsValid || Math.Abs(p.Z - z) > Tolerance)
                    throw new ArgumentException("All analysis points must be valid and lie in the same XY plane.");
            _plane = new Plane(new Point3d(0, 0, z), Vector3d.ZAxis);
            _boundaries = boundaries; _obstacles = obstacles;
            Blocking = new List<Curve>(obstacles); Blocking.AddRange(boundaries);
            foreach (var c in Blocking)
            {
                if (c == null || !c.IsValid) throw new ArgumentException("Boundary and obstacle curves must be valid.");
                var box = c.GetBoundingBox(true);
                if (!box.IsValid || Math.Abs(box.Min.Z-z) > Tolerance || Math.Abs(box.Max.Z-z) > Tolerance)
                    throw new ArgumentException("Points, boundaries and obstacles must lie in the same XY plane.");
            }
            foreach (var c in boundaries)
                if (!c.IsClosed) throw new ArgumentException("_Boundary curves must be closed. Use _Obstacles for open walls.");
        }

        public bool Allows(Point3d point, bool acceptContact, out bool contact)
        {
            contact = false;
            bool inBoundary = _boundaries.Count == 0;
            foreach (var c in _boundaries)
            {
                var relation = c.Contains(point, _plane, Tolerance);
                if (relation == PointContainment.Unset) throw new ArgumentException("Cannot classify points against _Boundary.");
                if (relation == PointContainment.Inside) inBoundary = true;
                if (relation == PointContainment.Coincident) { contact = true; inBoundary = true; }
            }
            if (!inBoundary) return false;
            foreach (var c in _obstacles)
            {
                if (c.IsClosed)
                {
                    var relation = c.Contains(point, _plane, Tolerance);
                    if (relation == PointContainment.Unset) throw new ArgumentException("Cannot classify points against a closed obstacle.");
                    if (relation == PointContainment.Inside) return false;
                    if (relation == PointContainment.Coincident) contact = true;
                }
                else if (c.ClosestPoint(point, out double t) && c.PointAt(t).DistanceTo(point) <= Tolerance)
                    contact = true;
            }
            return acceptContact || !contact;
        }

        public bool Allows(Point3d point) => Allows(point, true, out _);
        public bool RayStartsOutside(Point3d origin, Vector3d direction, double radius)
            => !Allows(origin + direction * Math.Min(radius * 0.5, Tolerance * 4));
    }
}
