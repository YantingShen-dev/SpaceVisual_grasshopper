using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Graph;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Visualize
{
    public class SV_VisualPath2D : SVComponent
    {
        public SV_VisualPath2D() : base(
            name: "Visual Path 2D",
            nickname: "VPath",
            description:
                "A* shortest visibility-path between two viewpoints on a visibility graph. " +
                "Edge cost = straight-line distance; heuristic = Euclidean distance to goal " +
                "(admissible and consistent → A* yields the optimal path).",
            subCategory: Constants.SubCategory.Visualize)
        { }

        public override Guid ComponentGuid => new Guid("c8e2f5a1-9b34-4d07-bc6a-5d2e8f1a7036");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Visual Path 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph from SV_BuildGraph2D.", GH_ParamAccess.item);
            pm.AddPointParameter("Start", "S",
                "Start viewpoint (snapped to nearest graph node).", GH_ParamAccess.item);
            pm.AddPointParameter("End", "E",
                "End viewpoint (snapped to nearest graph node).", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddCurveParameter("Path", "P",
                "Shortest visibility-path as a PolylineCurve. Null if unreachable.",
                GH_ParamAccess.item);
            pm.AddNumberParameter("Length", "L",
                "Path length (sum of edge lengths). NaN if unreachable.",
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            GH_VisibilityGraph? graphGoo = null;
            Point3d startPt = default;
            Point3d endPt = default;

            if (!da.GetData(0, ref graphGoo) || graphGoo?.Value == null)
            {
                Fail("No graph."); return;
            }
            if (!da.GetData(1, ref startPt)) return;
            if (!da.GetData(2, ref endPt)) return;

            var graph = graphGoo.Value;
            int n = graph.Count;
            if (n == 0) { Fail("Empty graph."); return; }

            int sIdx = NearestNode(graph, startPt);
            int eIdx = NearestNode(graph, endPt);

            var result = AStar(graph, sIdx, eIdx);

            if (result.path == null)
            {
                Warn($"No visibility path from node {sIdx} to node {eIdx}.");
                da.SetData(1, double.NaN);
                return;
            }

            var verts = new Point3d[result.path.Count];
            for (int i = 0; i < result.path.Count; i++) verts[i] = graph.Points[result.path[i]];
            var poly = new Polyline(verts);

            da.SetData(0, poly.ToPolylineCurve());
            da.SetData(1, result.length);
        }

        private static int NearestNode(VisibilityGraph g, Point3d pt)
        {
            int best = 0;
            double bestSq = double.PositiveInfinity;
            for (int i = 0; i < g.Count; i++)
            {
                double dsq = g.Points[i].DistanceToSquared(pt);
                if (dsq < bestSq) { bestSq = dsq; best = i; }
            }
            return best;
        }

        /// <summary>
        /// A* on a visibility graph. Uses lazy-deletion via a closed-set bool array;
        /// each node is finalised on its first dequeue.
        /// </summary>
        private static (List<int>? path, double length) AStar(
            VisibilityGraph g, int start, int goal)
        {
            int n = g.Count;
            if (start == goal) return (new List<int> { start }, 0);

            var gScore   = new double[n];
            var cameFrom = new int[n];
            var closed   = new bool[n];
            for (int i = 0; i < n; i++)
            {
                gScore[i] = double.PositiveInfinity;
                cameFrom[i] = -1;
            }
            gScore[start] = 0;

            var goalPt = g.Points[goal];
            var open = new MinHeap<int>();
            open.Enqueue(start, g.Points[start].DistanceTo(goalPt));

            while (open.TryDequeue(out int current, out _))
            {
                if (closed[current]) continue;
                closed[current] = true;

                if (current == goal)
                {
                    var path = new List<int>();
                    int c = current;
                    while (c >= 0) { path.Add(c); c = cameFrom[c]; }
                    path.Reverse();
                    return (path, gScore[goal]);
                }

                var nbrs = g.Adjacency[current];
                var curPt = g.Points[current];
                double gCur = gScore[current];

                for (int k = 0; k < nbrs.Length; k++)
                {
                    int nb = nbrs[k];
                    if (closed[nb]) continue;

                    double cost = curPt.DistanceTo(g.Points[nb]);
                    double tentative = gCur + cost;
                    if (tentative < gScore[nb])
                    {
                        cameFrom[nb] = current;
                        gScore[nb] = tentative;
                        double f = tentative + g.Points[nb].DistanceTo(goalPt);
                        open.Enqueue(nb, f);
                    }
                }
            }

            return (null, double.NaN);
        }
    }
}
