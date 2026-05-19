using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;

namespace SpaceVisual.Build
{
    public class SV_ViewGridTriangle : SVComponent
    {
        public SV_ViewGridTriangle() : base(
            name: "View Grid Triangle",
            nickname: "GridT",
            description:
                "Triangulated grid of analysis points within a region.\n" +
                "  Curve   : 2D rectangular or hexagonal grid; Delaunay mesh clipped to the curve boundary.\n" +
                "  Surface : auto-meshed (default meshing parameters); points = face centers.\n" +
                "  Mesh    : points = face centers of the input mesh.",
            subCategory: Constants.SubCategory.Build)
        { }

        public override Guid ComponentGuid => new Guid("b1a8c3d5-2e74-4a09-b5c1-8f3a6d1e2c47");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("View Grid Triangle");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddGeometryParameter("Region", "R",
                "Region geometry: closed Curve, Surface, Brep, or Mesh.",
                GH_ParamAccess.item);
            int sIdx = pm.AddNumberParameter("_Spacing", "_S",
                "Cell size. For Curve regions: grid step. For Surface/Brep: drives auto-meshing edge length. " +
                "For pre-tessellated Mesh inputs: ignored (their existing tessellation is used).",
                GH_ParamAccess.item, 1.0);
            pm[sIdx].Optional = true;
            int mIdx = pm.AddIntegerParameter("_Mode", "_M",
                "Sampling mode (Curve only): 0 = Rectangle, 1 = Hexagonal.",
                GH_ParamAccess.item, 0);
            pm[mIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddMeshParameter("Mesh", "M",
                "Triangulated mesh aligned with the points (Delaunay for Curve, source/auto-mesh for Surface/Mesh).",
                GH_ParamAccess.item);
            pm.AddPointParameter("Points", "P",
                "Generated points (one per face center for Mesh/Surface; grid samples for Curve).",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            IGH_GeometricGoo? regionGoo = null;
            double spacing = 1.0;
            int mode = 0;

            if (!da.GetData(0, ref regionGoo) || regionGoo == null)
            {
                Fail("No region geometry provided."); return;
            }
            da.GetData(1, ref spacing);
            da.GetData(2, ref mode);

            if (spacing <= 0)
            {
                Fail("Spacing must be positive."); return;
            }

            Curve? curve = null;
            Mesh? mesh = null;
            Brep? brep = null;
            Surface? surf = null;

            // Try Mesh first to handle pre-meshed inputs without extra conversion.
            if (GH_Convert.ToMesh(regionGoo, ref mesh, GH_Conversion.Primary) && mesh != null)
            {
                SolveForMesh(mesh, da);
                return;
            }
            if (GH_Convert.ToCurve(regionGoo, ref curve, GH_Conversion.Primary) && curve != null)
            {
                SolveForCurve(curve, spacing, mode, da);
                return;
            }
            if (GH_Convert.ToBrep(regionGoo, ref brep, GH_Conversion.Both) && brep != null)
            {
                SolveForBrep(brep, spacing, da);
                return;
            }
            if (GH_Convert.ToSurface(regionGoo, ref surf, GH_Conversion.Both) && surf != null)
            {
                var bp = Brep.CreateFromSurface(surf);
                if (bp != null) { SolveForBrep(bp, spacing, da); return; }
            }

            Fail("Region must be a Curve, Surface, Brep, or Mesh.");
        }

        // ─────────────────────────────────────────────────────────────── Curve

        private void SolveForCurve(Curve curve, double spacing, int mode, IGH_DataAccess da)
        {
            if (!curve.IsClosed)
                Warn("Region curve is not closed; inside-test may behave unexpectedly.");

            bool hex = mode == 1;

            var bbox = curve.GetBoundingBox(Plane.WorldXY);
            if (!bbox.IsValid)
            {
                Fail("Region curve has an invalid bounding box."); return;
            }

            var pts = new List<Point3d>();
            double yStep = hex ? spacing * Math.Sqrt(3.0) / 2.0 : spacing;
            double rowShift = hex ? spacing * 0.5 : 0.0;
            const double containmentTol = 1e-6;

            int rowIdx = 0;
            for (double y = bbox.Min.Y; y <= bbox.Max.Y + 1e-9; y += yStep, rowIdx++)
            {
                double xShift = (hex && (rowIdx & 1) == 1) ? rowShift : 0.0;
                for (double x = bbox.Min.X + xShift; x <= bbox.Max.X + 1e-9; x += spacing)
                {
                    var p = new Point3d(x, y, 0);
                    var c = curve.Contains(p, Plane.WorldXY, containmentTol);
                    if (c == PointContainment.Inside || c == PointContainment.Coincident)
                        pts.Add(p);
                }
            }

            if (pts.Count == 0)
                Warn("No points were generated inside the region. Try a smaller spacing.");

            var rawMesh = MakeDelaunay(pts);
            var clipped = (rawMesh == null) ? null : ClipMeshToCurve(rawMesh, curve, containmentTol);

            da.SetData(0, clipped);
            da.SetDataList(1, pts);
        }

        private static Mesh? MakeDelaunay(List<Point3d> pts)
        {
            if (pts.Count < 3) return null;

            var nodes = new Grasshopper.Kernel.Geometry.Node2List();
            foreach (var p in pts)
                nodes.Append(new Grasshopper.Kernel.Geometry.Node2(p.X, p.Y));

            var faces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(nodes, 0);
            if (faces == null || faces.Count == 0) return null;

            var m = new Mesh();
            for (int i = 0; i < pts.Count; i++) m.Vertices.Add(pts[i]);
            foreach (var f in faces) m.Faces.AddFace(f.A, f.B, f.C);
            m.Normals.ComputeNormals();
            m.Compact();
            return m;
        }

        /// <summary>
        /// Cull mesh faces whose centroid lies outside the curve. This removes
        /// the Delaunay convex-hull overflow into the concave parts of the region.
        /// </summary>
        private static Mesh ClipMeshToCurve(Mesh mesh, Curve boundary, double tol)
        {
            var kept = new Mesh();
            kept.Vertices.AddVertices(mesh.Vertices);

            for (int f = 0; f < mesh.Faces.Count; f++)
            {
                var center = mesh.Faces.GetFaceCenter(f);
                var c = boundary.Contains(center, Plane.WorldXY, tol);
                if (c == PointContainment.Inside || c == PointContainment.Coincident)
                    kept.Faces.AddFace(mesh.Faces[f]);
            }
            kept.Normals.ComputeNormals();
            kept.Compact();
            return kept;
        }

        // ─────────────────────────────────────────────────────────────── Mesh / Brep / Surface

        private void SolveForMesh(Mesh mesh, IGH_DataAccess da)
        {
            var pts = ExtractFaceCenters(mesh);
            da.SetData(0, mesh);
            da.SetDataList(1, pts);
        }

        private void SolveForBrep(Brep brep, double spacing, IGH_DataAccess da)
        {
            // Drive Brep / Surface tessellation density from Spacing so this input
            // is actually wired up for curved surfaces (otherwise spacing was inert).
            var mp = new MeshingParameters
            {
                MaximumEdgeLength = spacing,
                MinimumEdgeLength = spacing * 0.25,
                GridAspectRatio   = 1.0,
                SimplePlanes      = false,
                JaggedSeams       = false,
                RefineGrid        = true,
            };

            var pieces = Mesh.CreateFromBrep(brep, mp);
            if (pieces == null || pieces.Length == 0)
            {
                Fail("Failed to mesh the input Brep / Surface."); return;
            }
            var joined = new Mesh();
            foreach (var p in pieces) if (p != null && p.IsValid) joined.Append(p);
            joined.Normals.ComputeNormals();
            joined.Compact();

            var pts = ExtractFaceCenters(joined);
            da.SetData(0, joined);
            da.SetDataList(1, pts);
        }

        private static List<Point3d> ExtractFaceCenters(Mesh mesh)
        {
            int fcount = mesh.Faces.Count;
            var pts = new List<Point3d>(fcount);
            for (int f = 0; f < fcount; f++)
                pts.Add(mesh.Faces.GetFaceCenter(f));
            return pts;
        }
    }
}
