using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Graph;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Analyze2D
{
    public class SV_VGAMetrics2D : SVComponent
    {
        public SV_VGAMetrics2D() : base(
            name: "VGA Metrics 2D",
            nickname: "VGA",
            description:
                "Compute five space-syntax metrics from a visibility graph: " +
                "integration, entropy, control, clustering, connectivity. " +
                "Integration and entropy share the same all-pairs BFS pass " +
                "(parallel, thread-local scratch). All five metrics are computed " +
                "in a single pass.",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => new Guid("f4a7b8e2-6c91-4d05-bcad-3e8a2f7d9106");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("VGA Metrics 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddParameter(new Param_VisibilityGraph(), "Graph", "G",
                "Visibility graph from SV_BuildGraph2D.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddNumberParameter("Integration",  "I",  "1 / mean depth per node.", GH_ParamAccess.list);
            pm.AddNumberParameter("Entropy",      "E",  "Depth-distribution Shannon entropy (bits) per node.", GH_ParamAccess.list);
            pm.AddNumberParameter("Control",      "Ct", "Σ 1/C_j over neighbors per node.", GH_ParamAccess.list);
            pm.AddNumberParameter("Clustering",   "Cl", "Local clustering coefficient per node.", GH_ParamAccess.list);
            pm.AddNumberParameter("Connectivity", "Cn", "Degree per node.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            GH_VisibilityGraph? graphGoo = null;
            if (!da.GetData(0, ref graphGoo) || graphGoo?.Value == null)
            {
                Fail("No graph."); return;
            }
            var graph = graphGoo.Value;
            int n = graph.Count;
            if (n == 0) { Fail("Empty graph."); return; }

            // Connectivity (degree) — needed for Control anyway.
            var connectivity = new int[n];
            for (int i = 0; i < n; i++) connectivity[i] = graph.Adjacency[i].Length;

            var integration = new double[n];
            var entropy     = new double[n];
            var control     = new double[n];
            var clustering  = new double[n];

            // Control: Σ_{j∈N(i)} 1/max(1, C_j).
            Parallel.For(0, n, i =>
            {
                var ni = graph.Adjacency[i];
                double sum = 0;
                for (int k = 0; k < ni.Length; k++)
                    sum += 1.0 / Math.Max(1, connectivity[ni[k]]);
                control[i] = sum;
            });

            // Clustering: count edges between neighbors of i.
            var adjSets = new HashSet<int>[n];
            Parallel.For(0, n, i => adjSets[i] = new HashSet<int>(graph.Adjacency[i]));

            Parallel.For(0, n, i =>
            {
                var ni = graph.Adjacency[i];
                int k = ni.Length;
                if (k < 2) { clustering[i] = 0; return; }

                int actual = 0;
                for (int a = 0; a < k; a++)
                {
                    var sa = adjSets[ni[a]];
                    for (int b = a + 1; b < k; b++)
                        if (sa.Contains(ni[b])) actual++;
                }
                double possible = k * (k - 1) * 0.5;
                clustering[i] = actual / possible;
            });

            // Integration + Entropy: one parallel all-pairs BFS pass.
            BFS.AllPairsEnumerateParallel(graph, (s, depth) =>
            {
                int reachable = 0;
                long totalDepth = 0;
                int maxD = 0;

                for (int j = 0; j < n; j++)
                {
                    if (j == s) continue;
                    int d = depth[j];
                    if (d > 0)
                    {
                        reachable++;
                        totalDepth += d;
                        if (d > maxD) maxD = d;
                    }
                }

                integration[s] = reachable > 0
                    ? reachable / (double)totalDepth   // = 1 / mean_depth
                    : 0;

                if (reachable == 0 || maxD == 0)
                {
                    entropy[s] = 0;
                }
                else
                {
                    var bins = new int[maxD + 1];
                    for (int j = 0; j < n; j++)
                    {
                        if (j == s) continue;
                        int d = depth[j];
                        if (d > 0) bins[d]++;
                    }
                    double h = 0;
                    double invReach = 1.0 / reachable;
                    for (int d = 1; d <= maxD; d++)
                    {
                        int c = bins[d];
                        if (c == 0) continue;
                        double p = c * invReach;
                        h -= p * Math.Log(p, 2);
                    }
                    entropy[s] = h;
                }
            });

            var connD = new double[n];
            for (int i = 0; i < n; i++) connD[i] = connectivity[i];

            da.SetDataList(0, integration);
            da.SetDataList(1, entropy);
            da.SetDataList(2, control);
            da.SetDataList(3, clustering);
            da.SetDataList(4, connD);
        }
    }
}
