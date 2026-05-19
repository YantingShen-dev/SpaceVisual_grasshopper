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
                "Build a 2D visibility graph from analysis points and obstacle curves. " +
                "Two points are connected if the segment between them is not blocked by any obstacle. " +
                "Multithreaded (Parallel.For over N points × N pair-tests).",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => new Guid("d7e3a9b4-5f12-4c80-a6e9-2b8d4f7c1903");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Build Graph 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddPointParameter("Points", "P",
                "Analysis points (viewpoints) projected to the WorldXY plane.",
                GH_ParamAccess.list);
            int oIdx = pm.AddCurveParameter("_Obstacles", "_O",
                "2D obstacle curves. Lines crossing any obstacle are treated as blocked. Leave empty for an all-visible graph.",
                GH_ParamAccess.list);
            pm[oIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph. Feed to SV_VGAMetrics2D / SV_FromViewpoint2D / SV_VisualPath2D.",
                GH_ParamAccess.item);
            pm.AddLineParameter("Visible Lines", "L",
                "Unique sight line segments (one per visible pair, i<j). Useful for visualisation.",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var pts = new List<Point3d>();
            var obstacles = new List<Curve>();

            if (!da.GetDataList(0, pts) || pts.Count == 0)
            {
                Fail("No points provided."); return;
            }
            da.GetDataList(1, obstacles);

            int n = pts.Count;
            var ptArr = pts.ToArray();

            var occluder = new CurveOccluder(obstacles);

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
            Remark($"Built graph: {n} points, {edges} edges, avg degree {(double)totalDegree / n:F1}.");

            da.SetData(0, new GH_VisibilityGraph(graph));
            da.SetDataList(1, lines);
        }
    }
}
