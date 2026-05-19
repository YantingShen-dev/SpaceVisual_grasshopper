using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Geometry;

namespace SpaceVisual.Analyze3D
{
    public class SV_Isovist3D : SVComponent
    {
        public SV_Isovist3D() : base(
            name: "Isovist 3D",
            nickname: "Iso3D",
            description:
                "3D Isovist analysis. From each viewpoint, fires N rays over the upper hemisphere " +
                "(Fibonacci sphere sampling) and reports sight lines plus five visibility metrics. " +
                "Multithreaded (Parallel.For over viewpoints).\n" +
                "Use _Min Elev Angle to raise the cutoff above the horizon (e.g., 10° to exclude near-ground rays).\n" +
                "Mesh inputs are 5-50x faster than Brep — Brep / Surface inputs are auto-meshed internally.",
            subCategory: Constants.SubCategory.Analyze3D)
        { }

        public override Guid ComponentGuid => new Guid("e7c2b491-3f6d-4a85-9b07-1d4e8c3f5a92");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Isovist 3D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddPointParameter("Points", "P",
                "Viewpoints in 3D.",
                GH_ParamAccess.list);
            int oIdx = pm.AddGeometryParameter("_Obstacles", "_O",
                "3D obstacle geometry. Mesh strongly preferred; Brep/Surface accepted but auto-meshed.",
                GH_ParamAccess.list);
            pm[oIdx].Optional = true;
            int rIdx = pm.AddNumberParameter("_Radius", "_R",
                "Maximum sight distance.",
                GH_ParamAccess.item, 50.0);
            pm[rIdx].Optional = true;
            int sIdx = pm.AddIntegerParameter("_Samples", "_N",
                "Number of ray samples on the full sphere (Fibonacci spiral). " +
                "Roughly half are kept after the upper-hemisphere filter.",
                GH_ParamAccess.item, 512);
            pm[sIdx].Optional = true;
            int mIdx = pm.AddNumberParameter("_Min Elev Angle", "_Mn",
                "Lowest elevation (degrees from horizontal) — rays below this are skipped. " +
                "Default 0 = upper hemisphere only.",
                GH_ParamAccess.item, 0.0);
            pm[mIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddLineParameter("Sight Lines", "L",
                "Sight lines per viewpoint (Tree, one branch per viewpoint).",
                GH_ParamAccess.tree);
            pm.AddNumberParameter("Volume", "V",
                "Estimated visible volume above the local ground tangent.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Surface Area", "Sa",
                "Estimated visible surface area above the local ground tangent.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Max Radial", "Mr",
                "Maximum distance from viewpoint to any hit point.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Mean Radial", "Md",
                "Mean distance from viewpoint to hit points.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("Drift", "Dr",
                "Distance from viewpoint to centroid of hit points. Indicates view asymmetry.",
                GH_ParamAccess.list);
            pm.AddNumberParameter("SVF", "Sv",
                "Sky View Factor [0..1]. Cosine-weighted fraction of the upper hemisphere unobstructed " +
                "by obstacles within Radius. 1 = wide open sky, 0 = fully enclosed. " +
                "Accuracy improves with more samples (the per-ray accumulation runs inside the existing loop, so it's near-free).",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var pts = new List<Point3d>();
            var obstacleGoos = new List<IGH_GeometricGoo>();
            double radius = 50;
            int samples = 512;
            double minElev = 0;

            if (!da.GetDataList(0, pts) || pts.Count == 0) { Fail("No points."); return; }
            da.GetDataList(1, obstacleGoos);
            da.GetData(2, ref radius);
            da.GetData(3, ref samples);
            da.GetData(4, ref minElev);

            if (radius <= 0) { Fail("Radius must be positive."); return; }
            if (samples < 16) { Fail("Samples must be >= 16."); return; }
            if (minElev < -90 || minElev > 90) { Fail("Min Elev Angle must be in [-90, 90]."); return; }

            // Pool obstacles for the raycaster.
            var allObstacles = new List<GeometryBase>();
            foreach (var g in obstacleGoos)
                if (g?.ScriptVariable() is GeometryBase gb) allObstacles.Add(gb);

            var caster = new MeshRaycaster(allObstacles);
            if (caster.BrepConvertedCount > 0)
                Warn($"{caster.BrepConvertedCount} Brep/Surface input(s) auto-meshed. " +
                     "Pre-mesh upstream (Mesh from Brep) for best performance and stable face indexing.");

            // Full Fibonacci sphere — pre-filtered once (world +Z up).
            var allDirs = FibonacciSphere(samples);
            double sinMin = Math.Sin(minElev * Math.PI / 180.0);
            var keptDirs = new List<Vector3d>(allDirs.Count);
            for (int k = 0; k < allDirs.Count; k++)
                if (allDirs[k].Z >= sinMin) keptDirs.Add(allDirs[k]);

            int m = keptDirs.Count;
            if (m == 0) { Fail("All directions below elevation cutoff."); return; }

            double omegaTotal = 2.0 * Math.PI * (1.0 - sinMin);
            double vFactor = omegaTotal / (3.0 * m);
            double sFactor = omegaTotal / m;

            int n = pts.Count;
            var ptArr = pts.ToArray();

            var hitsPerVp = new Point3d[n][];
            var volumes  = new double[n];
            var areas    = new double[n];
            var maxRad   = new double[n];
            var meanRad  = new double[n];
            var drift    = new double[n];
            var svf      = new double[n];

            Parallel.For(0, n, i =>
            {
                var origin = ptArr[i];
                var hits = new Point3d[m];
                double sumDist = 0, sumD2 = 0, sumD3 = 0, mx = 0;
                double cx = 0, cy = 0, cz = 0;
                double svfSky = 0, svfTotal = 0;

                for (int k = 0; k < m; k++)
                {
                    var dir = keptDirs[k];
                    var ray = new Ray3d(origin, dir);
                    bool isHit = caster.TryIntersect(ray, radius, out double t, out _, out _);
                    double d = isHit ? t : radius;

                    var hit = origin + dir * d;
                    hits[k] = hit;

                    sumDist += d;
                    sumD2 += d * d;
                    sumD3 += d * d * d;
                    if (d > mx) mx = d;
                    cx += hit.X; cy += hit.Y; cz += hit.Z;

                    // Sky View Factor: cosine-weighted (cos(zenith) = dir.Z for upper-hemisphere rays).
                    // Only rays above horizon contribute; downward rays are already filtered out.
                    double w = dir.Z;
                    if (w > 0)
                    {
                        svfTotal += w;
                        if (!isHit) svfSky += w;
                    }
                }

                hitsPerVp[i] = hits;
                volumes[i] = vFactor * sumD3;
                areas[i]   = sFactor * sumD2;
                maxRad[i]  = mx;
                meanRad[i] = sumDist / m;

                var centroid = new Point3d(cx / m, cy / m, cz / m);
                drift[i] = centroid.DistanceTo(origin);
                svf[i] = svfTotal > 1e-12 ? svfSky / svfTotal : 0.0;
            });

            var lineTree = new DataTree<Line>();
            for (int i = 0; i < n; i++)
            {
                var origin = ptArr[i];
                var hits = hitsPerVp[i];
                var lines = new Line[hits.Length];
                for (int k = 0; k < hits.Length; k++) lines[k] = new Line(origin, hits[k]);
                lineTree.AddRange(lines, new GH_Path(i));
            }

            da.SetDataTree(0, lineTree);
            da.SetDataList(1, volumes);
            da.SetDataList(2, areas);
            da.SetDataList(3, maxRad);
            da.SetDataList(4, meanRad);
            da.SetDataList(5, drift);
            da.SetDataList(6, svf);
        }

        // ─────────────── helpers ───────────────

        /// <summary>
        /// Generate <paramref name="count"/> approximately-uniform unit vectors via
        /// Fibonacci spiral. Z component spans -1..1.
        /// </summary>
        private static List<Vector3d> FibonacciSphere(int count)
        {
            var result = new List<Vector3d>(count);
            double phi = Math.PI * (Math.Sqrt(5.0) - 1.0); // golden ratio derivative
            double inv = count > 1 ? 2.0 / (count - 1) : 0.0;

            for (int i = 0; i < count; i++)
            {
                double z = 1.0 - i * inv;
                double r = Math.Sqrt(Math.Max(0, 1.0 - z * z));
                double theta = phi * i;
                double x = Math.Cos(theta) * r;
                double y = Math.Sin(theta) * r;
                result.Add(new Vector3d(x, y, z));
            }
            return result;
        }
    }
}
