using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Params;
using SpaceVisual.Core.Visual;

namespace SpaceVisual.Visualize
{
    public class SV_Colorize : SVComponent, IGH_VariableParameterComponent
    {
        private const int FixedInputCount   = 2;
        private const int FirstValuesIndex  = 2;

        // Cached render state — populated in SolveInstance, consumed in
        // DrawViewportMeshes / DrawViewportWires / BakeGeometry.
        private Mesh? _coloredMesh;
        private Mesh? _legendMesh;
        private List<LabelData>? _labels;
        private BoundingBox _clippingBox = BoundingBox.Empty;

        private struct LabelData
        {
            public string Text;
            public Plane  Plane;
            public double Height;
            public Color  Color;
        }

        public SV_Colorize() : base(
            name: "Colorize",
            nickname: "Color",
            description:
                "Renders a colored heatmap directly into the Rhino viewport, plus a legend bar and " +
                "value labels. No data outputs — bake to commit the visuals to the document.\n" +
                "Zoom in on the component to expose +/- buttons that add or remove extra Values inputs; " +
                "when multiple Values series are wired, the equal-weight average is used per index.\n" +
                "Colorization and legend layout are configured via the optional _Parameter input " +
                "(use SV_Parameter to build it). Label size and legend bar dimensions auto-scale from " +
                "the analysis mesh's bounding box; the Parameter component supplies scale multipliers " +
                "on top of that base unit.",
            subCategory: Constants.SubCategory.Visualize)
        { }

        public override Guid ComponentGuid => new Guid("a1d4f3c5-9b62-4e07-bd58-3a8c2f1d6e74");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override Bitmap? Icon => IconLoader.Load("Colorize");

        public override bool IsPreviewCapable => true;

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddMeshParameter("Mesh", "M",
                "Mesh to color. Values length must match vertex count or face count.",
                GH_ParamAccess.item);
            int pIdx = pm.AddParameter(new Param_ColorParameter(),
                "_Parameter", "_P",
                "Optional colorization + legend parameter bundle (from SV_Parameter).",
                GH_ParamAccess.item);
            pm[pIdx].Optional = true;
            pm.AddNumberParameter("Values 0", "V0",
                "Primary values series. Zoom-add more Values inputs for equal-weight blending.",
                GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            // No data outputs. Colorize renders directly to the viewport and supports baking.
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            // Clear cached state so a failed solve doesn't leave a stale preview.
            _coloredMesh = null;
            _legendMesh  = null;
            _labels      = null;
            _clippingBox = BoundingBox.Empty;

            Mesh? mesh = null;
            if (!da.GetData(0, ref mesh) || mesh == null) { Fail("No mesh."); return; }

            GH_ColorParameter? paramGoo = null;
            var param = (da.GetData(1, ref paramGoo) && paramGoo?.Value != null)
                ? paramGoo.Value
                : SV_ColorParameter.Default();

            // Read every Values input (index 2 .. end).
            var series = new List<List<double>>();
            for (int i = FirstValuesIndex; i < Params.Input.Count; i++)
            {
                var vals = new List<double>();
                if (da.GetDataList(i, vals) && vals.Count > 0)
                    series.Add(vals);
            }
            if (series.Count == 0) { Fail("No values."); return; }

            // Equal-weight average across series, NaN-safe.
            int len = series[0].Count;
            var combined = new double[len];
            for (int j = 0; j < len; j++)
            {
                double sum = 0; int cnt = 0;
                foreach (var v in series)
                {
                    if (j >= v.Count) continue;
                    double x = v[j];
                    if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                    sum += x; cnt++;
                }
                combined[j] = cnt > 0 ? sum / cnt : double.NaN;
            }

            var grad = param.ToGradient();
            var colors = grad.MapValues(combined, param.Min, param.Max);

            Mesh coloredMesh;
            try
            {
                int vCount = mesh.Vertices.Count;
                int fCount = mesh.Faces.Count;

                if (combined.Length == vCount)
                    coloredMesh = MeshColorizer.ColorByVertices(mesh, colors);
                else if (combined.Length == fCount)
                    coloredMesh = MeshColorizer.ColorByFaces(mesh, colors);
                else
                {
                    Fail($"Values length {combined.Length} matches neither vertex count {vCount} nor face count {fCount}.");
                    return;
                }
            }
            catch (ArgumentException ex) { Fail(ex.Message); return; }

            _coloredMesh = coloredMesh;

            // Range for legend labels.
            double dataMin = double.PositiveInfinity, dataMax = double.NegativeInfinity;
            for (int j = 0; j < combined.Length; j++)
            {
                double v = combined[j];
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                if (v < dataMin) dataMin = v;
                if (v > dataMax) dataMax = v;
            }
            double legendMin = param.Min ?? (double.IsInfinity(dataMin) ? 0 : dataMin);
            double legendMax = param.Max ?? (double.IsInfinity(dataMax) ? 1 : dataMax);

            BuildLegend(mesh, param, grad, legendMin, legendMax);

            // Clipping box for viewport culling.
            var bb = coloredMesh.GetBoundingBox(false);
            if (_legendMesh != null) bb.Union(_legendMesh.GetBoundingBox(false));
            if (_labels != null)
                foreach (var lbl in _labels) bb.Union(lbl.Plane.Origin);
            _clippingBox = bb;
        }

        // ----------------------------------------------------------- legend build

        private void BuildLegend(Mesh sourceMesh, SV_ColorParameter p, HeatGradient grad, double min, double max)
        {
            const int strips = 16;
            var bbox = sourceMesh.GetBoundingBox(false);
            double diag = bbox.Diagonal.Length;
            double baseUnit = diag * 0.025;          // 2.5% of bbox diagonal
            if (baseUnit <= 1e-9) baseUnit = 1.0;

            double segH = baseUnit * Math.Max(0.01, p.SegHeight);
            double segW = baseUnit * Math.Max(0.01, p.SegWidth);
            double texH = baseUnit * Math.Max(0.01, p.TextHeight);
            double pad  = texH * 0.5;
            double total = segH * strips;

            Point3d origin = p.PlanePoint ?? (
                p.Vertical
                    ? new Point3d(bbox.Max.X + segW, bbox.Min.Y, bbox.Min.Z)
                    : new Point3d(bbox.Min.X, bbox.Max.Y + segW, bbox.Min.Z));

            var legend = new Mesh();
            for (int i = 0; i < strips; i++)
            {
                double a0 = i * segH;
                double a1 = (i + 1) * segH;
                Point3d c0, c1, c2, c3;
                if (p.Vertical)
                {
                    c0 = origin + new Vector3d(0,    a0, 0);
                    c1 = origin + new Vector3d(segW, a0, 0);
                    c2 = origin + new Vector3d(segW, a1, 0);
                    c3 = origin + new Vector3d(0,    a1, 0);
                }
                else
                {
                    c0 = origin + new Vector3d(a0, 0,    0);
                    c1 = origin + new Vector3d(a1, 0,    0);
                    c2 = origin + new Vector3d(a1, segW, 0);
                    c3 = origin + new Vector3d(a0, segW, 0);
                }
                int v0 = legend.Vertices.Add(c0);
                int v1 = legend.Vertices.Add(c1);
                int v2 = legend.Vertices.Add(c2);
                int v3 = legend.Vertices.Add(c3);
                legend.Faces.AddFace(v0, v1, v2, v3);
                var col = grad.Sample((i + 0.5) / strips);
                legend.VertexColors.SetColor(v0, col);
                legend.VertexColors.SetColor(v1, col);
                legend.VertexColors.SetColor(v2, col);
                legend.VertexColors.SetColor(v3, col);
            }
            legend.Normals.ComputeNormals();
            legend.Compact();
            _legendMesh = legend;

            _labels = new List<LabelData>();
            for (int k = 0; k < 3; k++)
            {
                double t = k * 0.5;
                double val = min + (max - min) * t;
                Point3d labelOrigin = p.Vertical
                    ? origin + new Vector3d(segW + pad, t * total, 0)
                    : origin + new Vector3d(t * total, segW + pad, 0);
                var plane = new Plane(labelOrigin, Vector3d.XAxis, Vector3d.YAxis);
                _labels.Add(new LabelData
                {
                    Text   = val.ToString("G4"),
                    Plane  = plane,
                    Height = texH,
                    Color  = Color.Black,
                });
            }
        }

        // ----------------------------------------------------------- viewport

        public override BoundingBox ClippingBox =>
            _clippingBox.IsValid ? _clippingBox : new BoundingBox();

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (Hidden) return;
            if (_coloredMesh != null) args.Display.DrawMeshFalseColors(_coloredMesh);
            if (_legendMesh  != null) args.Display.DrawMeshFalseColors(_legendMesh);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden) return;
            if (_labels == null) return;
            foreach (var lbl in _labels)
            {
                using var t3d = new Text3d(lbl.Text, lbl.Plane, lbl.Height)
                {
                    HorizontalAlignment = TextHorizontalAlignment.Left,
                    VerticalAlignment   = TextVerticalAlignment.Bottom,
                };
                args.Display.Draw3dText(t3d, lbl.Color);
            }
        }

        // ----------------------------------------------------------- bake

        public override bool IsBakeCapable => _coloredMesh != null;

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (_coloredMesh != null)
                obj_ids.Add(doc.Objects.AddMesh(_coloredMesh, att));
            if (_legendMesh != null)
                obj_ids.Add(doc.Objects.AddMesh(_legendMesh, att));
            if (_labels != null)
            {
                var dsId = doc.DimStyles.Current.Id;
                foreach (var lbl in _labels)
                {
                    var te = new TextEntity
                    {
                        PlainText        = lbl.Text,
                        Plane            = lbl.Plane,
                        TextHeight       = lbl.Height,
                        DimensionStyleId = dsId,
                    };
                    obj_ids.Add(doc.Objects.AddText(te, att));
                }
            }
        }

        // ─────────────── IGH_VariableParameterComponent ───────────────

        public bool CanInsertParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Input && index >= FirstValuesIndex;

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Input && index > FirstValuesIndex;

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            int seriesNum = index - FixedInputCount;
            return new Param_Number
            {
                Name        = $"Values {seriesNum}",
                NickName    = $"V{seriesNum}",
                Description = $"Values series #{seriesNum}. Averaged equally with other Values inputs.",
                Access      = GH_ParamAccess.list,
            };
        }

        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public void VariableParameterMaintenance()
        {
            int seriesIdx = 0;
            for (int i = FirstValuesIndex; i < Params.Input.Count; i++)
            {
                var p = Params.Input[i];
                p.Name        = $"Values {seriesIdx}";
                p.NickName    = $"V{seriesIdx}";
                p.Description = seriesIdx == 0
                    ? "Primary values series."
                    : $"Values series #{seriesIdx}. Averaged equally with other Values inputs.";
                p.Access      = GH_ParamAccess.list;
                seriesIdx++;
            }
        }
    }
}
