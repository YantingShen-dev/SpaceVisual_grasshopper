using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace SpaceVisual.Core.Params
{
    public sealed class Param_ColorParameter : GH_PersistentParam<GH_ColorParameter>
    {
        public Param_ColorParameter() : base(
            "Color Parameter", "Param",
            "Bundle of colorization + legend layout parameters for SV_Colorize.",
            Constants.Category, "Params")
        { }

        public override Guid ComponentGuid => new Guid("d5e9b3a2-7f48-4c10-bd62-3a8e1f7c0269");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override GH_GetterResult Prompt_Plural(ref List<GH_ColorParameter> values)
            => GH_GetterResult.cancel;

        protected override GH_GetterResult Prompt_Singular(ref GH_ColorParameter value)
            => GH_GetterResult.cancel;
    }
}
