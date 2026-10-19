using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace SpaceVisual.Core.Params
{
    public sealed class Param_GridPoint : GH_PersistentGeometryParam<GH_GridPoint>
    {
        public Param_GridPoint() : base(new GH_InstanceDescription("Points", "P", "Analysis points with optional cell geometry.", Constants.Category, "Params")) { }
        public override Guid ComponentGuid => PluginIdentity.For(new Guid("bfb5555d-4167-40dd-9dd5-7f98698f041b"));
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        protected override GH_GetterResult Prompt_Singular(ref GH_GridPoint value) => GH_GetterResult.cancel;
        protected override GH_GetterResult Prompt_Plural(ref List<GH_GridPoint> values) => GH_GetterResult.cancel;
    }
}
