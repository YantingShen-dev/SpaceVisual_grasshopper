using Grasshopper.Kernel.Types;
using SpaceVisual.Core.Graph;

namespace SpaceVisual.Core.Params
{
    /// <summary>
    /// Grasshopper data-wrapper for VisibilityGraph. Carries a single graph
    /// instance between SV_BuildGraph2D and the downstream VGA / FromViewpoint /
    /// VisualPath components.
    /// </summary>
    public sealed class GH_VisibilityGraph : GH_Goo<VisibilityGraph>
    {
        public GH_VisibilityGraph() { }

        public GH_VisibilityGraph(VisibilityGraph value) : base(value) { }

        public GH_VisibilityGraph(GH_VisibilityGraph other) : base(other?.Value!) { }

        public override bool IsValid => Value != null && Value.Count > 0;

        public override string TypeName => "Visibility Graph";

        public override string TypeDescription =>
            "Adjacency between mutually-visible analysis points. " +
            "Output of SV_BuildGraph2D; consumed by VGA / FromViewpoint / VisualPath.";

        public override IGH_Goo Duplicate() => new GH_VisibilityGraph(this);

        public override string ToString()
        {
            if (Value == null) return "Null Graph";
            int edges = 0;
            for (int i = 0; i < Value.Adjacency.Length; i++)
            {
                var row = Value.Adjacency[i];
                for (int k = 0; k < row.Length; k++)
                    if (row[k] > i) edges++;
            }
            return $"Visibility Graph ({Value.Count} pts, {edges} edges)";
        }
    }
}
