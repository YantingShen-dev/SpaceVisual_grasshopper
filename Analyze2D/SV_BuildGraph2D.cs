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
        private bool _legacyLayout;
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("CellsInterface", _legacyLayout ? 0 : 2);
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            _legacyLayout = !reader.ItemExists("CellsInterface") || reader.GetInt32("CellsInterface") != 2;
            return base.Read(reader);
        }
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
            pm.AddParameter(new Param_GridPoint(), "Points", "P",
                "Candidate viewpoints. Points outside _Boundary or inside closed _Obstacles are removed. tol controls points on any contour. All geometry must share an XY plane.",
                GH_ParamAccess.list);
            int oIdx = pm.AddCurveParameter("_Obstacles", "_O",
                "Internal obstacles: closed curves exclude interior points; all curves block sightlines. tol controls contact points. " +
                "Connect the outer boundary to _B, not here. Closed obstacles are solid even when nested.",
                GH_ParamAccess.list);
            pm[oIdx].Optional = true;
            int bIdx = pm.AddCurveParameter("_Boundary", "_B",
                "Optional closed outer boundary curves. Keep points inside any boundary (tol controls outline points), and block sightlines crossing their outlines. " +
                "Connect the same outer boundary used by View Grid here. With no boundary, the analysis extent is unrestricted.",
                GH_ParamAccess.list);
            pm[bIdx].Optional = true;
            int tolIdx = pm.AddBooleanParameter("tol", "tol", "Accept points on boundary or obstacle curves. False (default) excludes contact points; true includes them. Closed-obstacle interiors remain excluded. Contact distance uses 1e-6 model units.", GH_ParamAccess.item, false);
            pm[tolIdx].Optional = true;

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddMeshParameter("Mesh", "M", "Retained grid cells in graph/value order. Generated from cell information carried by View Grid Points. Connect to Colorize.", GH_ParamAccess.item);
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph. Feed to SV_VGAMetrics2D / SV_FromViewpoint2D / SV_VisualPath2D.",
                GH_ParamAccess.item);
            pm.AddLineParameter("Visible Lines", "L",
                "Unique sight line segments (one per visible pair, i<j). Useful for visualisation.",
                GH_ParamAccess.list);
            pm.AddParameter(new Param_GridPoint(), "Filtered Points", "P", "Retained points in graph/VGA order. Preview these instead of the original grid points.", GH_ParamAccess.list);
            pm.AddIntegerParameter("Kept Indices", "I", "Zero-based indices in the original Points list, in graph order. Use to select matching source data or grid faces before colorizing compact VGA values.", GH_ParamAccess.list);
            pm.AddIntegerParameter("Removed Indices", "R", "Zero-based indices of rejected input points. Graph/VGA outputs do not include these points.", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            if (_legacyLayout) { Fail("This saved Graph2D uses the old output order. Place a fresh Build Graph 2D (Mesh first, Graph second) and reconnect it."); return; }
            var samples = new List<GH_GridPoint>();
            var pts = new List<Point3d>();
            var obstacles = new List<Curve>();
            var boundaries = new List<Curve>();

            if (!da.GetDataList(0, samples) || samples.Count == 0)
            {
                Fail("No points provided."); return;
            }
            foreach (var sample in samples) pts.Add(sample.Value);
            da.GetDataList(1, obstacles);
            da.GetDataList(2, boundaries);
            bool acceptContact = false;
            da.GetData(3, ref acceptContact);
            AnalysisDomain2D domain;
            try { domain = new AnalysisDomain2D(pts, boundaries, obstacles); }
            catch (ArgumentException ex) { Fail(ex.Message); return; }
            var blocking = domain.Blocking;
            var retained = new List<Point3d>();
            var kept = new List<int>();
            var removed = new List<int>();
            var contacts = new List<bool>();
            try
            {
                for (int i = 0; i < pts.Count; i++)
                    if (domain.Allows(pts[i], acceptContact, out bool contact))
                    { retained.Add(pts[i]); kept.Add(i); contacts.Add(contact); }
                    else removed.Add(i);
            }
            catch (ArgumentException ex) { Fail(ex.Message); return; }
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
                    if (!occluder.IsBlocked(pi, ptArr[j]) &&
                        (!(contacts[i] || contacts[j]) || domain.Allows((pi + ptArr[j]) / 2.0)))
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

            var filteredSamples = new List<GH_GridPoint>();
            foreach (int index in kept) filteredSamples.Add((GH_GridPoint)samples[index].DuplicateGeometry());
            var mesh = AnalysisMesh.Build(samples, kept, domain);
            if (mesh != null) da.SetData(0, mesh);
            else Warn("Mesh unavailable: connect Points directly from a fresh View Grid or View Grid Triangle. Missing, duplicated or mixed point/mesh metadata cannot define an aligned visualization mesh.");
            da.SetData(1, new GH_VisibilityGraph(graph));
            da.SetDataList(2, lines);
            da.SetDataList(3, filteredSamples);
            da.SetDataList(4, kept);
            da.SetDataList(5, removed);
        }
    }
}
