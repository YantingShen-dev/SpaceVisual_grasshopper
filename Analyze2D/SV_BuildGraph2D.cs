using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Geometry;
using SpaceVisual.Core.Graph;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Analyze2D
{
    public class SV_BuildGraph2D : SVComponent
    {
        public SV_BuildGraph2D() : base(
            name: "Build Graph 2D",
            nickname: "Graph2D",
            description:
                "Filter candidate points to the boundary and outside solid obstacles, then build a 2D visibility graph. " +
                "Two points are connected if the segment between them is not blocked by any obstacle. " +
                "Multithreaded (Parallel.For over N points × N pair-tests).",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => PluginIdentity.For(new Guid("d7e3a9b4-5f12-4c80-a6e9-2b8d4f7c1903"));

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Build Graph 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddPointParameter("Points", "P",
                "Candidate viewpoints. Points outside _Boundary or inside/on closed _Obstacles are removed before building the graph. All geometry must share an XY plane.",
                GH_ParamAccess.list);
            int oIdx = pm.AddCurveParameter("_Obstacles", "_O",
                "Internal obstacles: closed curves exclude points inside/on them; open curves only block sightlines. " +
                "Connect the outer boundary to _B, not here. Closed obstacles are solid even when nested.",
                GH_ParamAccess.list);
            pm[oIdx].Optional = true;
            int bIdx = pm.AddCurveParameter("_Boundary", "_B",
                "Optional closed outer boundary curves. Keep points inside/on any boundary, and block sightlines crossing their outlines. " +
                "Connect the same outer boundary used by View Grid here. With no boundary, the analysis extent is unrestricted.",
                GH_ParamAccess.list);
            pm[bIdx].Optional = true;
            int mIdx = pm.AddMeshParameter("_Grid Mesh", "_M",
                "Optional original View Grid mesh, with one face per input point in the same order. " +
                "Faces at rejected points are removed. Connect output M to Colorize instead of the original mesh. " +
                "Vertex-sampled meshes are not supported by this input.", GH_ParamAccess.item);
            pm[mIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph. Feed to SV_VGAMetrics2D / SV_FromViewpoint2D / SV_VisualPath2D.",
                GH_ParamAccess.item);
            pm.AddLineParameter("Visible Lines", "L",
                "Unique sight line segments (one per visible pair, i<j). Useful for visualisation.",
                GH_ParamAccess.list);
            pm.AddPointParameter("Filtered Points", "P", "Retained points in graph/VGA order. Preview these instead of the original grid points.", GH_ParamAccess.list);
            pm.AddIntegerParameter("Kept Indices", "I", "Zero-based indices in the original Points list, in graph order. Use to select matching source data or grid faces before colorizing compact VGA values.", GH_ParamAccess.list);
            pm.AddIntegerParameter("Removed Indices", "R", "Zero-based indices of rejected input points. Graph/VGA outputs do not include these points.", GH_ParamAccess.list);
            pm.AddMeshParameter("Filtered Mesh", "M",
                "Grid faces retained in graph/VGA order. Connect to Colorize with graph metrics; hide the original grid preview. " +
                "Requires the original face-sampled mesh at _M.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var pts = new List<Point3d>();
            var obstacles = new List<Curve>();
            var boundaries = new List<Curve>();

            if (!da.GetDataList(0, pts) || pts.Count == 0)
            {
                Fail("No points provided."); return;
            }
            da.GetDataList(1, obstacles);
            da.GetDataList(2, boundaries);
            Mesh? gridMesh = null;
            da.GetData(3, ref gridMesh);
            if (gridMesh != null && (!gridMesh.IsValid || gridMesh.Faces.Count != pts.Count))
            {
                Fail("_Grid Mesh must be valid and have one face per original input point, in the same order. " +
                     $"Received {gridMesh.Faces.Count} faces and {pts.Count} points. Use the face-sampled View Grid mesh.");
                return;
            }
            const double tolerance = 1e-6;
            double z = pts[0].Z;
            var plane = new Plane(new Point3d(0, 0, z), Vector3d.ZAxis);
            foreach (var p in pts)
                if (!p.IsValid || Math.Abs(p.Z - z) > tolerance)
                { Fail("All candidate points must be valid and lie in the same XY plane."); return; }

            var blocking = new List<Curve>(obstacles);
            blocking.AddRange(boundaries);
            foreach (var c in blocking)
            {
                if (c == null || !c.IsValid)
                { Fail("Boundary and obstacle curves must be valid."); return; }
                var box = c.GetBoundingBox(true);
                if (!box.IsValid || Math.Abs(box.Min.Z - z) > tolerance || Math.Abs(box.Max.Z - z) > tolerance)
                { Fail("All boundaries, obstacles and points must lie in the same XY plane."); return; }
            }
            foreach (var boundary in boundaries)
                if (!boundary.IsClosed)
                { Fail("_Boundary curves must be closed. Put open wall curves in _Obstacles."); return; }

            var retained = new List<Point3d>();
            var kept = new List<int>();
            var removed = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                bool allowed = boundaries.Count == 0;
                foreach (var boundary in boundaries)
                {
                    var relation = boundary.Contains(pts[i], plane, tolerance);
                    if (relation == PointContainment.Unset)
                    { Fail("Could not classify points against _Boundary. Check closed planar curves."); return; }
                    if (relation == PointContainment.Inside || relation == PointContainment.Coincident) allowed = true;
                }
                if (allowed)
                    foreach (var obstacle in obstacles)
                    {
                        if (!obstacle.IsClosed) continue;
                        var relation = obstacle.Contains(pts[i], plane, tolerance);
                        if (relation == PointContainment.Unset)
                        { Fail("Could not classify points against a closed obstacle. Check its geometry."); return; }
                        if (relation == PointContainment.Inside || relation == PointContainment.Coincident)
                        { allowed = false; break; }
                    }
                if (allowed) { retained.Add(pts[i]); kept.Add(i); }
                else removed.Add(i);
            }
            if (blocking.Count == 0)
                Warn("No boundary or obstacles: every pair of points is visible.");
            if (retained.Count == 0)
                Warn("All points were excluded. Outer boundaries belong in _B; closed curves in _O are solid obstacles.");

            int n = retained.Count;
            var ptArr = retained.ToArray();

            using var occluder = new CurveOccluder(blocking);

            // Each pair (i,j) with j>i is independent. Parallelise the outer i loop
            // and collect visible pairs in a thread-safe bag; we'll fold them into
            // a sorted adjacency once.
            var pairs = new ConcurrentBag<(int i, int j)>();

            Parallel.For(0, n, i =>
            {
                var pi = ptArr[i];
                for (int j = i + 1; j < n; j++)
                {
                    if (!occluder.IsBlocked(pi, ptArr[j]))
                        pairs.Add((i, j));
                }
            });

            // Fold into adjacency
            var rows = new List<int>[n];
            for (int i = 0; i < n; i++) rows[i] = new List<int>();
            foreach (var (i, j) in pairs)
            {
                rows[i].Add(j);
                rows[j].Add(i);
            }

            var adjacency = new int[n][];
            int totalDegree = 0;
            for (int i = 0; i < n; i++)
            {
                var arr = rows[i].ToArray();
                Array.Sort(arr);
                adjacency[i] = arr;
                totalDegree += arr.Length;
            }

            var graph = new VisibilityGraph(ptArr, adjacency);
            var lines = graph.GetVisibleLines();

            int edges = totalDegree / 2;
            Remark($"Built graph: {n}/{pts.Count} points retained, {removed.Count} removed, {edges} edges, avg degree {(n == 0 ? 0 : (double)totalDegree / n):F1}.");

            da.SetData(0, new GH_VisibilityGraph(graph));
            da.SetDataList(1, lines);
            da.SetDataList(2, ptArr);
            da.SetDataList(3, kept);
            da.SetDataList(4, removed);
            if (gridMesh != null)
            {
                // Preserve source face order and attributes without changing the input mesh.
                var filteredMesh = gridMesh.DuplicateMesh();
                filteredMesh.Faces.Clear();
                foreach (int index in kept) filteredMesh.Faces.AddFace(gridMesh.Faces[index]);
                filteredMesh.Vertices.CullUnused();
                filteredMesh.Normals.ComputeNormals();
                filteredMesh.Compact();
                da.SetData(5, filteredMesh);
            }
        }
    }
}
