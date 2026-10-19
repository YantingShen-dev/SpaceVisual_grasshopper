using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Geometry;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Analyze2D
{
    public class SV_Isovist2D : SVComponent
    {
        private bool _legacyLayout;
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("IsovistMeshInterface", _legacyLayout ? 0 : 1);
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            _legacyLayout = !reader.ItemExists("IsovistMeshInterface") || reader.GetInt32("IsovistMeshInterface") != 1;
            return base.Read(reader);
        }
        public SV_Isovist2D() : base(
            name: "Isovist 2D",
            nickname: "Iso2D",
            description:
                "2D Isovist analysis. From each viewpoint, fires N rays uniformly in plane, " +
                "finds the nearest obstacle curve along each, and returns the resulting polygon " +
                "plus five geometric metrics. Multithreaded (Parallel.For over viewpoints).",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => PluginIdentity.For(new Guid("e8b4a2c9-1f53-4d76-9a02-3c5e7b8f1d62"));

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Isovist 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddParameter(new Param_GridPoint(), "Points", "P", "Candidate viewpoints. Grid point metadata supplies the filtered visualization mesh.", GH_ParamAccess.list);
            int oIdx = pm.AddCurveParameter("_Obstacles", "_O", "Internal solid obstacles and open walls. Connect outer boundaries to _Boundary. Closed obstacle interiors are excluded; tol controls contact points.", GH_ParamAccess.list);
            pm[oIdx].Optional = true;
            int rIdx = pm.AddNumberParameter("_Radius", "_R",
                "Maximum sight distance. Rays that don't hit an obstacle within this distance terminate here.",
                GH_ParamAccess.item, 50.0);
            pm[rIdx].Optional = true;
            int nIdx = pm.AddIntegerParameter("_Count", "_N",
                "Number of rays per viewpoint. More = smoother polygon, slower.",
                GH_ParamAccess.item, 100);
            pm[nIdx].Optional = true;
            int bIdx = pm.AddCurveParameter("_Boundary", "_B", "Optional closed outer boundaries. Filter points to their union and stop rays at the outlines.", GH_ParamAccess.list);
            pm[bIdx].Optional = true;
            int tolIdx = pm.AddBooleanParameter("tol", "tol", "Accept points on boundary or obstacle curves. False (default) excludes contacts; true includes them. Closed-obstacle interiors remain excluded. Contact distance uses 1e-6 model units.", GH_ParamAccess.item, false);
            pm[tolIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddMeshParameter("Mesh", "M", "Filtered mesh in the same point order as all numeric outputs. Connect to Colorize.", GH_ParamAccess.item);
            pm.AddCurveParameter("Polygon", "Pgn", "Isovist polygon (closed PolylineCurve) per viewpoint.", GH_ParamAccess.list);
            pm.AddNumberParameter("Area", "A", "Polygon area.", GH_ParamAccess.list);
            pm.AddNumberParameter("Perimeter", "Pm", "Polygon perimeter.", GH_ParamAccess.list);
            pm.AddNumberParameter("Compactness", "C",
                "4π·Area / Perimeter². 1 = perfect circle, near 0 = elongated corridor.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Max Radial", "Mr",
                "Maximum distance from viewpoint to polygon boundary.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Drift", "Dr",
                "Distance from viewpoint to polygon centroid. Indicates which side the view 'pulls' to.",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            if (_legacyLayout) { Fail("Place a fresh Isovist 2D with Mesh first, _Boundary and tol inputs, then reconnect it."); return; }
            var samples = new List<GH_GridPoint>();
            var pts = new List<Point3d>();
            var obstacles = new List<Curve>();
            var boundaries = new List<Curve>();
            double radius = 50.0;
            int count = 72;
            bool acceptContact = false;
            if (!da.GetDataList(0, samples) || samples.Count == 0) { Fail("No points."); return; }
            foreach (var p in samples) pts.Add(p.Value);
            da.GetDataList(1, obstacles); da.GetData(2, ref radius); da.GetData(3, ref count);
            da.GetDataList(4, boundaries); da.GetData(5, ref acceptContact);
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0) { Fail("Radius must be finite and positive."); return; }
            if (count < 8) { Fail("Count must be >= 8."); return; }
            AnalysisDomain2D domain;
            var kept = new List<int>();
            var contacts = new List<bool>();
            try
            {
                domain = new AnalysisDomain2D(pts, boundaries, obstacles);
                for (int i = 0; i < pts.Count; i++)
                    if (domain.Allows(pts[i], acceptContact, out bool contact)) { kept.Add(i); contacts.Add(contact); }
            }
            catch (ArgumentException ex) { Fail(ex.Message); return; }
            using var occluder = new CurveOccluder(domain.Blocking);
            int n = kept.Count;
            var ptArr = new Point3d[n];
            for (int i = 0; i < n; i++) ptArr[i] = pts[kept[i]];
            var mesh = AnalysisMesh.Build(samples, kept, domain);
            if (mesh != null) da.SetData(0, mesh);
            else Warn("Mesh unavailable: connect Points directly from a fresh View Grid or View Grid Triangle. Missing, duplicated or mixed point/mesh metadata cannot define an aligned visualization mesh.");
            Remark($"Isovist: {n}/{pts.Count} points retained, {pts.Count - n} removed; tol={acceptContact}.");
            if (n == 0) Warn("All viewpoints were excluded. Put outer boundaries in _Boundary; closed _Obstacles are solid. tol controls contour contact points.");

            var polygons = new Curve?[n];
            var areas    = new double[n];
            var perims   = new double[n];
            var compact  = new double[n];
            var maxRad   = new double[n];
            var drift    = new double[n];

            Parallel.For(0, n, i =>
            {
                var origin = ptArr[i];
                var verts = new Point3d[count];

                for (int k = 0; k < count; k++)
                {
                    double angle = 2.0 * Math.PI * k / count;
                    var dir = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
                    if (contacts[i] && domain.RayStartsOutside(origin, dir, radius))
                        verts[k] = origin;
                    else if (occluder.TryNearestHit(origin, dir, radius, out Point3d hit, out _))
                        verts[k] = hit;
                    else
                        verts[k] = origin + dir * radius;
                }

                // Closed polyline
                var poly = new Polyline(count + 1);
                for (int k = 0; k < count; k++)
                    if (poly.Count == 0 || poly[poly.Count-1].DistanceTo(verts[k]) > 1e-9) poly.Add(verts[k]);
                if (poly.Count > 1 && poly[poly.Count-1].DistanceTo(poly[0]) <= 1e-9) poly.RemoveAt(poly.Count-1);
                if (poly.Count >= 3) { poly.Add(poly[0]); polygons[i] = poly.ToPolylineCurve(); }

                ComputeMetrics(origin, verts,
                    out areas[i], out perims[i], out compact[i], out maxRad[i], out drift[i]);
            });

            da.SetDataList(1, polygons);
            da.SetDataList(2, areas);
            da.SetDataList(3, perims);
            da.SetDataList(4, compact);
            da.SetDataList(5, maxRad);
            da.SetDataList(6, drift);
        }

        /// <summary>
        /// Shoelace formula for area + centroid + per-edge perimeter + max radial.
        /// </summary>
        private static void ComputeMetrics(
            Point3d origin, Point3d[] verts,
            out double area, out double perimeter,
            out double compactness, out double maxRadial, out double drift)
        {
            int n = verts.Length;
            double signedTwoArea = 0;
            double cxNum = 0, cyNum = 0;
            perimeter = 0;
            maxRadial = 0;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double xi = verts[i].X, yi = verts[i].Y;
                double xj = verts[j].X, yj = verts[j].Y;

                double cross = xi * yj - xj * yi;
                signedTwoArea += cross;
                cxNum += (xi + xj) * cross;
                cyNum += (yi + yj) * cross;

                perimeter += verts[i].DistanceTo(verts[j]);

                double r = origin.DistanceTo(verts[i]);
                if (r > maxRadial) maxRadial = r;
            }

            double signedArea = signedTwoArea * 0.5;
            area = Math.Abs(signedArea);

            if (Math.Abs(signedArea) > 1e-12)
            {
                double cx = cxNum / (6.0 * signedArea);
                double cy = cyNum / (6.0 * signedArea);
                drift = new Point3d(cx, cy, origin.Z).DistanceTo(origin);
            }
            else
            {
                drift = 0;
            }

            compactness = perimeter > 1e-12
                ? (4.0 * Math.PI * area) / (perimeter * perimeter)
                : 0;
        }
    }
}
