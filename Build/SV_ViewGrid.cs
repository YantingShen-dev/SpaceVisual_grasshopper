using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;

namespace SpaceVisual.Build
{
    public class SV_ViewGrid : SVComponent
    {
        public SV_ViewGrid() : base(
            name: "View Grid",
            nickname: "Grid",
            description:
                "Quad UV-grid of analysis points within a region.\n" +
                "  Curve          : planar UV grid clipped to the curve (Overhang controls boundary cells).\n" +
                "  Surface        : exact (U+1)×(V+1) UV grid via Surface.PointAt.\n" +
                "  Brep (trimmed) : UV grid with trim-aware cell culling. Each cell's 4 corners are tested against " +
                "the face trim (IsPointOnFace).\n" +
                "Overhang = false (default): strict — keep only cells whose 4 corners are all inside the trim " +
                "(boundary-crossing cells dropped → stepped inset).\n" +
                "Overhang = true: lenient — keep any cell with ≥1 corner inside (boundary cells kept → stepped outset).",
            subCategory: Constants.SubCategory.Build)
        { }

        public override Guid ComponentGuid => new Guid("f3a8e5c2-7b41-4d09-bc52-1d6e8a3f4c70");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("View Grid");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddGeometryParameter("Region", "R",
                "Region geometry: closed planar Curve, Surface, or single-face Brep.",
                GH_ParamAccess.item);
            int uIdx = pm.AddIntegerParameter("_U Count", "_U",
                "Number of cells along the U direction (or along the curve bbox X-axis for Curve input).",
                GH_ParamAccess.item, 10);
            pm[uIdx].Optional = true;
            int vIdx = pm.AddIntegerParameter("_V Count", "_V",
                "Number of cells along the V direction (or Y-axis for Curve input).",
                GH_ParamAccess.item, 10);
            pm[vIdx].Optional = true;
            int oIdx = pm.AddBooleanParameter("_Overhang", "_Oh",
                "True = keep faces that overhang the boundary. False = cull faces whose center lies outside.",
                GH_ParamAccess.item, false);
            pm[oIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddMeshParameter("Mesh", "M",
                "Quad mesh covering the region.",
                GH_ParamAccess.item);
            pm.AddPointParameter("Points", "P",
                "Face-center points (one per quad face, aligned with mesh.Faces).",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            IGH_GeometricGoo? regionGoo = null;
            int uCount = 20, vCount = 20;
            bool overhang = false;

            if (!da.GetData(0, ref regionGoo) || regionGoo == null) { Fail("No region."); return; }
            da.GetData(1, ref uCount);
            da.GetData(2, ref vCount);
            da.GetData(3, ref overhang);

            if (uCount < 1 || vCount < 1) { Fail("U Count and V Count must be ≥ 1."); return; }

            Brep? brep = null;
            Surface? surf = null;
            Curve? curve = null;

            // Brep input first — it carries trim info we need for trim-respecting cell culling.
            if (GH_Convert.ToBrep(regionGoo, ref brep, GH_Conversion.Primary) && brep != null && brep.Faces.Count > 0)
            {
                if (brep.Faces.Count > 1) Warn("Brep has multiple faces; using the first one.");
                var face = brep.Faces[0];
                // Pass BrepFace as the trim source when the face is actually trimmed.
                BrepFace? trim = face.IsSurface ? null : face;
                SolveForSurface(face.UnderlyingSurface(), uCount, vCount, overhang, trim, da);
                return;
            }
            if (GH_Convert.ToSurface(regionGoo, ref surf, GH_Conversion.Both) && surf != null)
            {
                // Raw Surface has no trim info — straight UV grid (matches native Mesh Surface for untrimmed input).
                SolveForSurface(surf, uCount, vCount, overhang, null, da);
                return;
            }
            if (GH_Convert.ToCurve(regionGoo, ref curve, GH_Conversion.Both) && curve != null)
            {
                SolveForCurve(curve, uCount, vCount, overhang, da);
                return;
            }

            Fail("Region must be a Curve, Surface, or single-face Brep.");
        }

        // ─────────────────────────────────────────────────────────────── Curve

        private void SolveForCurve(Curve curve, int uCount, int vCount, bool overhang, IGH_DataAccess da)
        {
            if (!curve.IsClosed)
                Warn("Region curve is not closed; inside-test may behave unexpectedly.");

            var bbox = curve.GetBoundingBox(Plane.WorldXY);
            if (!bbox.IsValid) { Fail("Region curve has an invalid bounding box."); return; }

            double dx = (bbox.Max.X - bbox.Min.X) / uCount;
            double dy = (bbox.Max.Y - bbox.Min.Y) / vCount;
            if (dx <= 0 || dy <= 0) { Fail("Region has zero width or height."); return; }

            // Grid of corner vertices.
            var corners = new Point3d[uCount + 1, vCount + 1];
            for (int j = 0; j <= vCount; j++)
                for (int i = 0; i <= uCount; i++)
                    corners[i, j] = new Point3d(bbox.Min.X + i * dx, bbox.Min.Y + j * dy, 0);

            var mesh = new Mesh();
            var vmap = new int[uCount + 1, vCount + 1];
            for (int j = 0; j <= vCount; j++)
                for (int i = 0; i <= uCount; i++)
                    vmap[i, j] = mesh.Vertices.Add(corners[i, j]);

            var faceCenters = new List<Point3d>();
            const double tol = 1e-6;

            for (int j = 0; j < vCount; j++)
            {
                for (int i = 0; i < uCount; i++)
                {
                    var c00 = corners[i,     j    ];
                    var c10 = corners[i + 1, j    ];
                    var c11 = corners[i + 1, j + 1];
                    var c01 = corners[i,     j + 1];
                    var center = new Point3d(
                        (c00.X + c10.X + c11.X + c01.X) * 0.25,
                        (c00.Y + c10.Y + c11.Y + c01.Y) * 0.25,
                        0);

                    if (!overhang)
                    {
                        var con = curve.Contains(center, Plane.WorldXY, tol);
                        if (con != PointContainment.Inside && con != PointContainment.Coincident) continue;
                    }

                    mesh.Faces.AddFace(vmap[i, j], vmap[i + 1, j], vmap[i + 1, j + 1], vmap[i, j + 1]);
                    faceCenters.Add(center);
                }
            }

            mesh.Normals.ComputeNormals();
            mesh.Compact();

            da.SetData(0, mesh);
            da.SetDataList(1, faceCenters);
        }

        // ─────────────────────────────────────────────────────────────── Surface

        /// <summary>
        /// (U+1)×(V+1) UV quad grid via Surface.PointAt — matches GH-native Mesh Surface
        /// behaviour, including its simple cell-by-cell trim culling for Brep faces.
        /// When <paramref name="trimFace"/> is non-null and <paramref name="overhang"/> is false,
        /// cells whose midpoint UV lies outside the face's trim are dropped (stepped boundary).
        /// </summary>
        private void SolveForSurface(Surface surf, int uCount, int vCount, bool overhang, BrepFace? trimFace, IGH_DataAccess da)
        {
            var uDom = surf.Domain(0);
            var vDom = surf.Domain(1);

            var uVals = new double[uCount + 1];
            var vVals = new double[vCount + 1];
            for (int i = 0; i <= uCount; i++) uVals[i] = uDom.ParameterAt(i / (double)uCount);
            for (int j = 0; j <= vCount; j++) vVals[j] = vDom.ParameterAt(j / (double)vCount);

            var mesh = new Mesh();
            var vmap = new int[uCount + 1, vCount + 1];
            for (int j = 0; j <= vCount; j++)
                for (int i = 0; i <= uCount; i++)
                    vmap[i, j] = mesh.Vertices.Add(surf.PointAt(uVals[i], vVals[j]));

            var faceCenters = new List<Point3d>(uCount * vCount);

            for (int j = 0; j < vCount; j++)
            {
                for (int i = 0; i < uCount; i++)
                {
                    double uMid = (uVals[i] + uVals[i + 1]) * 0.5;
                    double vMid = (vVals[j] + vVals[j + 1]) * 0.5;

                    if (trimFace != null)
                    {
                        // Sample all four cell corners against the trim. A cell is a
                        // BOUNDARY cell iff its corners straddle the trim curve.
                        bool c00 = trimFace.IsPointOnFace(uVals[i],     vVals[j])     != PointFaceRelation.Exterior;
                        bool c10 = trimFace.IsPointOnFace(uVals[i + 1], vVals[j])     != PointFaceRelation.Exterior;
                        bool c11 = trimFace.IsPointOnFace(uVals[i + 1], vVals[j + 1]) != PointFaceRelation.Exterior;
                        bool c01 = trimFace.IsPointOnFace(uVals[i],     vVals[j + 1]) != PointFaceRelation.Exterior;
                        int inCount = (c00 ? 1 : 0) + (c10 ? 1 : 0) + (c11 ? 1 : 0) + (c01 ? 1 : 0);

                        if (overhang)
                        {
                            // Lenient: keep any cell that has ANY corner inside or crosses the trim.
                            if (inCount == 0) continue;
                        }
                        else
                        {
                            // Strict: drop the cell if any corner is outside the trim — the trim
                            // either crosses this cell or excludes it entirely. Eliminates overflow.
                            if (inCount < 4) continue;
                        }
                    }

                    mesh.Faces.AddFace(vmap[i, j], vmap[i + 1, j], vmap[i + 1, j + 1], vmap[i, j + 1]);
                    faceCenters.Add(surf.PointAt(uMid, vMid));
                }
            }

            mesh.Normals.ComputeNormals();
            mesh.Compact();

            da.SetData(0, mesh);
            da.SetDataList(1, faceCenters);
        }
    }
}
