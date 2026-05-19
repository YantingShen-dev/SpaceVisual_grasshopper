using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace SpaceVisual.Core.Geometry
{
    /// <summary>
    /// Mesh-first 3D ray / line-of-sight engine. Accepts Mesh, Brep, or Surface
    /// obstacles; Breps and Surfaces are converted to Mesh once at construction
    /// (ray-mesh intersection is 5-50x faster than ray-brep).
    ///
    /// Wraps an RTree over per-mesh bounding boxes so each query only tests
    /// the meshes that could plausibly intersect the ray / segment.
    /// </summary>
    public sealed class MeshRaycaster
    {
        private readonly Mesh[] _meshes;
        private readonly bool[] _fromBrep;
        private readonly RTree _tree;
        private readonly int _brepConvertedCount;

        public int MeshCount => _meshes.Length;

        /// <summary>How many input Brep / Surface obstacles were auto-meshed.</summary>
        public int BrepConvertedCount => _brepConvertedCount;

        public Mesh GetMesh(int index) => _meshes[index];

        public MeshRaycaster(IEnumerable<GeometryBase> obstacles, MeshingParameters? meshingParams = null)
        {
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            var mp = meshingParams ?? MeshingParameters.Default;

            var collected = new List<Mesh>();
            var fromBrep = new List<bool>();
            int convertedCount = 0;

            foreach (var g in obstacles)
            {
                if (g == null) continue;
                switch (g)
                {
                    case Mesh m when m.IsValid:
                        collected.Add(m);
                        fromBrep.Add(false);
                        break;

                    case Brep b when b.IsValid:
                    {
                        var ms = Mesh.CreateFromBrep(b, mp);
                        if (ms == null) break;
                        foreach (var mm in ms)
                        {
                            if (mm == null || !mm.IsValid) continue;
                            collected.Add(mm);
                            fromBrep.Add(true);
                        }
                        convertedCount++;
                        break;
                    }

                    case Surface s when s.IsValid:
                    {
                        var brep = Brep.CreateFromSurface(s);
                        if (brep == null) break;
                        var ms = Mesh.CreateFromBrep(brep, mp);
                        if (ms == null) break;
                        foreach (var mm in ms)
                        {
                            if (mm == null || !mm.IsValid) continue;
                            collected.Add(mm);
                            fromBrep.Add(true);
                        }
                        convertedCount++;
                        break;
                    }
                }
            }

            _meshes = collected.ToArray();
            _fromBrep = fromBrep.ToArray();
            _brepConvertedCount = convertedCount;

            _tree = new RTree();
            for (int i = 0; i < _meshes.Length; i++)
            {
                var bb = _meshes[i].GetBoundingBox(false);
                if (bb.IsValid) _tree.Insert(bb, i);
            }
        }

        /// <summary>
        /// Try to find the nearest intersection along a ray within maxDistance.
        /// Returns true if a hit was found.
        /// </summary>
        public bool TryIntersect(
            Ray3d ray,
            double maxDistance,
            out double distance,
            out int meshIndex,
            out int faceIndex)
        {
            distance = double.PositiveInfinity;
            meshIndex = -1;
            faceIndex = -1;

            if (_meshes.Length == 0 || maxDistance <= 0) return false;

            var endPt = ray.Position + ray.Direction * maxDistance;
            var searchBox = new BoundingBox(new[] { ray.Position, endPt });
            searchBox.Inflate(1e-9);

            var candidates = new List<int>();
            _tree.Search(searchBox, (sender, e) => candidates.Add(e.Id));

            bool hit = false;
            foreach (var i in candidates)
            {
                int[] hitFaces;
                double t = Intersection.MeshRay(_meshes[i], ray, out hitFaces);
                if (t > 0 && t < distance && t <= maxDistance)
                {
                    distance = t;
                    meshIndex = i;
                    faceIndex = (hitFaces != null && hitFaces.Length > 0) ? hitFaces[0] : -1;
                    hit = true;
                }
            }
            return hit;
        }

        /// <summary>
        /// True if the segment from→to is blocked by any obstacle.
        /// Endpoints are excluded by <paramref name="endpointEpsilon"/> so that
        /// rays touching the start/end exactly don't register as blocked.
        /// </summary>
        public bool IsBlocked(Point3d from, Point3d to, double endpointEpsilon = 1e-4)
        {
            if (_meshes.Length == 0) return false;

            var v = to - from;
            double len = v.Length;
            if (len < endpointEpsilon * 2) return false;

            v *= 1.0 / len;
            var ray = new Ray3d(from, v);
            double clip = len - endpointEpsilon;
            if (clip <= endpointEpsilon) return false;

            return TryIntersect(ray, clip, out _, out _, out _);
        }
    }
}
