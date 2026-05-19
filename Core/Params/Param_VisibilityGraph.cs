using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace SpaceVisual.Core.Params
{
    /// <summary>
    /// Grasshopper parameter type that carries <see cref="GH_VisibilityGraph"/>.
    /// Hidden from the toolbar — instantiated automatically when components
    /// declare their I/O with this parameter type.
    /// </summary>
    public sealed class Param_VisibilityGraph : GH_PersistentParam<GH_VisibilityGraph>
    {
        public Param_VisibilityGraph() : base(
            "Visibility Graph", "Graph",
            "Adjacency between mutually-visible analysis points.",
            Constants.Category, "Params")
        { }

        public override Guid ComponentGuid => new Guid("c2e6d7b1-9a4f-4e1b-bd35-7e0f9c2a8d51");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override GH_GetterResult Prompt_Plural(ref List<GH_VisibilityGraph> values)
            => GH_GetterResult.cancel;

        protected override GH_GetterResult Prompt_Singular(ref GH_VisibilityGraph value)
            => GH_GetterResult.cancel;
    }
}
