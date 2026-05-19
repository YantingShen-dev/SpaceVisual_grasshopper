using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Params;
using SpaceVisual.Core.Visual;

namespace SpaceVisual.Visualize
{
    public class SV_Parameter : SVComponent
    {
        public SV_Parameter() : base(
            name: "Parameter",
            nickname: "Param",
            description:
                "Configure color mapping and legend layout for SV_Colorize. " +
                "All numeric parameters (Seg Height/Width/Text Height) are scale multipliers relative " +
                "to an auto-computed base unit; defaults give a readable legend at any model scale. " +
                "Plane Point pins the legend's lower-left corner — leave unwired to auto-place.",
            subCategory: Constants.SubCategory.Visualize)
        { }

        public override Guid ComponentGuid => new Guid("c2b6e8a1-3f47-4a09-bd05-2e8c1d7f5b30");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Parameter");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            int idx;
            idx = pm.AddNumberParameter("_Min", "_Mn",
                "Lower bound for value normalisation. Auto-detected if unwired.",
                GH_ParamAccess.item);
            pm[idx].Optional = true;

            idx = pm.AddNumberParameter("_Max", "_Mx",
                "Upper bound for value normalisation. Auto-detected if unwired.",
                GH_ParamAccess.item);
            pm[idx].Optional = true;

            idx = pm.AddColourParameter("_Colors", "_C",
                "Custom gradient stops (≥ 2 colors). Default = Viridis.",
                GH_ParamAccess.list);
            pm[idx].Optional = true;

            idx = pm.AddBooleanParameter("_Vertical", "_V",
                "Legend orientation: True = vertical bar, False = horizontal bar.",
                GH_ParamAccess.item, true);
            pm[idx].Optional = true;

            idx = pm.AddNumberParameter("_Seg Height", "_Sh",
                "Scale multiplier for each segment's length along the long axis. Default 1.0.",
                GH_ParamAccess.item, 1.0);
            pm[idx].Optional = true;

            idx = pm.AddNumberParameter("_Seg Width", "_Sw",
                "Scale multiplier for the legend strip thickness (perpendicular). Default 0.3.",
                GH_ParamAccess.item, 0.3);
            pm[idx].Optional = true;

            idx = pm.AddNumberParameter("_Text Height", "_Th",
                "Scale multiplier for label text height. Default 1.0.",
                GH_ParamAccess.item, 1.0);
            pm[idx].Optional = true;

            idx = pm.AddPointParameter("_Plane Point", "_Pt",
                "Lower-left corner of the legend bar. Unwired = auto-place next to the analysis mesh.",
                GH_ParamAccess.item);
            pm[idx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddParameter(new Param_ColorParameter(), "Parameter", "P",
                "Bundled parameter — wire into SV_Colorize's _Parameter input.",
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var p = SV_ColorParameter.Default();

            double minV = 0, maxV = 0;
            if (da.GetData(0, ref minV)) p.Min = minV;
            if (da.GetData(1, ref maxV)) p.Max = maxV;

            var colorList = new List<Color>();
            if (da.GetDataList(2, colorList) && colorList.Count >= 2)
                p.Colors = colorList.ToArray();

            bool vert = true;  if (da.GetData(3, ref vert)) p.Vertical = vert;

            double sh = 1.0;   if (da.GetData(4, ref sh)) p.SegHeight = sh;
            double sw = 0.3;   if (da.GetData(5, ref sw)) p.SegWidth = sw;
            double th = 1.0;   if (da.GetData(6, ref th)) p.TextHeight = th;

            Point3d pt = default;
            if (da.GetData(7, ref pt)) p.PlanePoint = pt;

            if (p.Min.HasValue && p.Max.HasValue && p.Min >= p.Max)
            {
                Fail("Min must be less than Max."); return;
            }

            da.SetData(0, new GH_ColorParameter(p));
        }
    }
}
