using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Graph;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Analyze2D
{
    public class SV_FromViewpoint2D : SVComponent
    {
        public SV_FromViewpoint2D() : base(
            name: "From Viewpoint 2D",
            nickname: "FromVP",
            description:
                "Compute step / distance / angle from a starting viewpoint to every node in the graph.\n" +
                "All three metrics respect obstacle occlusion: when a target is unreachable in the " +
                "visibility graph, all three are reported as NaN.\n" +
                "  Step     = BFS hop count along visibility graph.\n" +
                "  Distance = straight-line distance from start to target.\n" +
                "  Angle    = bearing in degrees, 0 = +X axis, CCW.\n" +
                "Multiple start points produce a DataTree (one branch per start).",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => new Guid("a5d2c9f3-7e84-4b10-8f6c-2a4e9b1d5072");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("From Viewpoint");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph from SV_BuildGraph2D.", GH_ParamAccess.item);
            pm.AddPointParameter("Start", "S",
                "Start viewpoint(s). Each is snapped to the nearest graph node.",
                GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddNumberParameter("Step", "St",
                "BFS hop count from start to each node. NaN means unreachable (blocked by obstacles).",
                GH_ParamAccess.tree);
            pm.AddNumberParameter("Distance", "D",
                "Straight-line distance from start to each reachable node. NaN for blocked targets.",
                GH_ParamAccess.tree);
            pm.AddNumberParameter("Angle", "A",
                "Bearing from start to each reachable node (degrees, 0 = +X, CCW). NaN for blocked targets.",
                GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            GH_VisibilityGraph? graphGoo = null;
            var starts = new List<Point3d>();

            if (!da.GetData(0, ref graphGoo) || graphGoo?.Value == null)
            {
                Fail("No graph."); return;
            }
            if (!da.GetDataList(1, starts) || starts.Count == 0)
            {
                Fail("No start point."); return;
            }

            var graph = graphGoo.Value;
            int n = graph.Count;
            if (n == 0) { Fail("Empty graph."); return; }

            var stepTree  = new DataTree<double>();
            var distTree  = new DataTree<double>();
            var angleTree = new DataTree<double>();

            const double RadToDeg = 180.0 / Math.PI;

            for (int s = 0; s < starts.Count; s++)
            {
                var startPt = starts[s];
                int startIdx = FindNearestNode(graph, startPt, out double snapDist);

                if (snapDist > 1e-3)
                    Remark($"Start #{s} snapped to nearest graph node at distance {snapDist:F3}.");

                var depth = BFS.FromSource(graph, startIdx);

                var stepRow  = new double[n];
                var distRow  = new double[n];
                var angleRow = new double[n];

                for (int j = 0; j < n; j++)
                {
                    int d_j = depth[j];
                    bool reachable = d_j >= 0;
                    stepRow[j] = reachable ? d_j : double.NaN;

                    if (!reachable)
                    {
                        // Target blocked by obstacles → distance & angle undefined.
                        distRow[j] = double.NaN;
                        angleRow[j] = double.NaN;
                        continue;
                    }

                    var v = graph.Points[j] - startPt;
                    double d = v.Length;
                    distRow[j] = d;
                    angleRow[j] = d > 1e-9
                        ? Math.Atan2(v.Y, v.X) * RadToDeg
                        : double.NaN;
                }

                var path = new GH_Path(s);
                stepTree.AddRange(stepRow, path);
                distTree.AddRange(distRow, path);
                angleTree.AddRange(angleRow, path);
            }

            da.SetDataTree(0, stepTree);
            da.SetDataTree(1, distTree);
            da.SetDataTree(2, angleTree);
        }

        private static int FindNearestNode(VisibilityGraph g, Point3d pt, out double distance)
        {
            int best = 0;
            double bestSq = double.PositiveInfinity;
            for (int i = 0; i < g.Count; i++)
            {
                double dsq = g.Points[i].DistanceToSquared(pt);
                if (dsq < bestSq) { bestSq = dsq; best = i; }
            }
            distance = Math.Sqrt(bestSq);
            return best;
        }
    }
}
