using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Geometry;

namespace SpaceVisual.Analyze2D
{
    public class SV_Isovist2D : SVComponent
    {
        public SV_Isovist2D() : base(
            name: "Isovist 2D",
            nickname: "Iso2D",
            description:
                "2D Isovist analysis. From each viewpoint, fires N rays uniformly in plane, " +
                "finds the nearest obstacle curve along each, and returns the resulting polygon " +
                "plus five geometric metrics. Multithreaded (Parallel.For over viewpoints).",
            subCategory: Constants.SubCategory.Analyze2D)
        { }

        public override Guid ComponentGuid => new Guid("e8b4a2c9-1f53-4d76-9a02-3c5e7b8f1d62");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Isovist 2D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddPointParameter("Points", "P", "Viewpoints.", GH_ParamAccess.list);
            int oIdx = pm.AddCurveParameter("_Obstacles", "_O", "2D obstacle curves.", GH_ParamAccess.list);
            pm[oIdx].Optional = true;
            int rIdx = pm.AddNumberParameter("_Radius", "_R",
                "Maximum sight distance. Rays that don't hit an obstacle within this distance terminate here.",
                GH_ParamAccess.item, 50.0);
            pm[rIdx].Optional = true;
            int nIdx = pm.AddIntegerParameter("_Count", "_N",
                "Number of rays per viewpoint. More = smoother polygon, slower.",
                GH_ParamAccess.item, 100);
            pm[nIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
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
            var pts = new List<Point3d>();
            var obstacles = new List<Curve>();
            double radius = 50.0;
            int count = 72;

            if (!da.GetDataList(0, pts) || pts.Count == 0) { Fail("No points."); return; }
            da.GetDataList(1, obstacles);
            da.GetData(2, ref radius);
            da.GetData(3, ref count);

            if (radius <= 0) { Fail("Radius must be positive."); return; }
            if (count < 8) { Fail("Count must be >= 8."); return; }

            var occluder = new CurveOccluder(obstacles);
            int n = pts.Count;
            var ptArr = pts.ToArray();

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
                    if (occluder.TryNearestHit(origin, dir, radius, out Point3d hit, out _))
                        verts[k] = hit;
                    else
                        verts[k] = origin + dir * radius;
                }

                // Closed polyline
                var poly = new Polyline(count + 1);
                for (int k = 0; k < count; k++) poly.Add(verts[k]);
                poly.Add(verts[0]);
                polygons[i] = poly.ToPolylineCurve();

                ComputeMetrics(origin, verts,
                    out areas[i], out perims[i], out compact[i], out maxRad[i], out drift[i]);
            });

            da.SetDataList(0, polygons);
            da.SetDataList(1, areas);
            da.SetDataList(2, perims);
            da.SetDataList(3, compact);
            da.SetDataList(4, maxRad);
            da.SetDataList(5, drift);
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
