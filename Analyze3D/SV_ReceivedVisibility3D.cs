using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpaceVisual.Core;
using SpaceVisual.Core.Base;
using SpaceVisual.Core.Geometry;

namespace SpaceVisual.Analyze3D
{
    public class SV_ReceivedVisibility3D : SVComponent
    {
        public SV_ReceivedVisibility3D() : base(
            name: "Received Visibility 3D",
            nickname: "RecvVis",
            description:
                "Inverse-visibility analysis: for each face of the obstacle mesh(es), how often / how " +
                "close is it seen by the provided viewpoints?\n" +
                "  Hit Count        : number of viewpoints with unobstructed line-of-sight to the face.\n" +
                "  Avg Distance     : mean viewer distance over visible pairs.\n" +
                "  Min Distance     : closest visible viewer.\n" +
                "  Normal Alignment : mean of dot(faceNormal, viewerDir). 1 = head-on, 0 = grazing.\n" +
                "Outputs are aligned with the concatenated face indices of all input meshes.",
            subCategory: Constants.SubCategory.Analyze3D)
        { }

        public override Guid ComponentGuid => new Guid("f9a4b7e3-6d12-4c08-bf5a-2e8c3f1d7a04");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap? Icon => IconLoader.Load("Received Visibility 3D");

        protected override void RegisterInputParams(GH_InputParamManager pm)
        {
            pm.AddPointParameter("Points", "P",
                "Viewpoints (people positions, camera positions, etc.).",
                GH_ParamAccess.list);
            pm.AddGeometryParameter("Obstacles", "O",
                "Mesh(es) whose faces are analysed (also act as occluders). Mesh preferred; Brep auto-meshed.",
                GH_ParamAccess.list);
            int poIdx = pm.AddNumberParameter("_Probe Offset", "_Po",
                "Offset along face normal where the ray test originates (avoids self-occlusion).",
                GH_ParamAccess.item, 1e-3);
            pm[poIdx].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pm)
        {
            pm.AddNumberParameter("Hit Count", "Hc",
                "Number of viewpoints that see each face.", GH_ParamAccess.list);
            pm.AddNumberParameter("Avg Distance", "Ad",
                "Mean distance to visible viewpoints (NaN if face is unseen).", GH_ParamAccess.list);
            pm.AddNumberParameter("Min Distance", "Md",
                "Closest visible viewpoint distance (NaN if face is unseen).", GH_ParamAccess.list);
            pm.AddNumberParameter("Normal Alignment", "Na",
                "Mean dot(faceNormal, unit-vector-to-viewpoint). NaN if unseen.", GH_ParamAccess.list);
            pm.AddPointParameter("Face Centers", "Fc",
                "Face center used for each target (same indexing as the metric outputs).",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            var viewpoints = new List<Point3d>();
            var obsGoo = new List<IGH_GeometricGoo>();
            double offset = 1e-3;

            if (!da.GetDataList(0, viewpoints) || viewpoints.Count == 0)
            {
                Fail("No viewpoints."); return;
            }
            da.GetDataList(1, obsGoo);
            da.GetData(2, ref offset);

            if (obsGoo.Count == 0) { Fail("No obstacles."); return; }

            // Convert obstacle inputs to meshes; collect face centers + normals for analysis.
            var targetCenters = new List<Point3d>();
            var targetNormals = new List<Vector3d>();
            var allObstacles = new List<GeometryBase>();
            int converted = 0;

            foreach (var goo in obsGoo)
            {
                var raw = goo?.ScriptVariable();
                Mesh? m = ResolveToMesh(raw, ref converted);
                if (m == null) continue;

                allObstacles.Add(m);

                if (m.FaceNormals.Count != m.Faces.Count) m.FaceNormals.ComputeFaceNormals();
                for (int f = 0; f < m.Faces.Count; f++)
                {
                    targetCenters.Add(m.Faces.GetFaceCenter(f));
                    var fn = m.FaceNormals[f];
                    targetNormals.Add(new Vector3d(fn.X, fn.Y, fn.Z));
                }
            }

            if (converted > 0)
                Warn($"{converted} Brep/Surface input(s) auto-meshed; pre-mesh upstream for face-index stability.");

            if (targetCenters.Count == 0) { Fail("No face targets extracted."); return; }

            var caster = new MeshRaycaster(allObstacles);

            int nT = targetCenters.Count;
            int nV = viewpoints.Count;
            var vpArr = viewpoints.ToArray();

            var hitCount  = new double[nT];
            var avgDist   = new double[nT];
            var minDist   = new double[nT];
            var normAlign = new double[nT];

            Parallel.For(0, nT, ti =>
            {
                var center = targetCenters[ti];
                var normal = targetNormals[ti];
                if (!normal.Unitize()) { hitCount[ti] = 0; avgDist[ti] = double.NaN; minDist[ti] = double.NaN; normAlign[ti] = double.NaN; return; }

                var probe = center + normal * offset;

                int count = 0;
                double sumDist = 0;
                double minD = double.PositiveInfinity;
                double sumAlign = 0;

                for (int vi = 0; vi < nV; vi++)
                {
                    var v = vpArr[vi];
                    var toV = v - center;
                    double dist = toV.Length;
                    if (dist < 1e-9) continue;

                    // Back-face cull: skip if viewpoint is behind the face.
                    double dotN = (normal.X * toV.X + normal.Y * toV.Y + normal.Z * toV.Z) / dist;
                    if (dotN <= 0) continue;

                    if (caster.IsBlocked(probe, v)) continue;

                    count++;
                    sumDist += dist;
                    if (dist < minD) minD = dist;
                    sumAlign += dotN;
                }

                hitCount[ti] = count;
                if (count > 0)
                {
                    avgDist[ti]   = sumDist / count;
                    minDist[ti]   = minD;
                    normAlign[ti] = sumAlign / count;
                }
                else
                {
                    avgDist[ti]   = double.NaN;
                    minDist[ti]   = double.NaN;
                    normAlign[ti] = double.NaN;
                }
            });

            da.SetDataList(0, hitCount);
            da.SetDataList(1, avgDist);
            da.SetDataList(2, minDist);
            da.SetDataList(3, normAlign);
            da.SetDataList(4, targetCenters);
        }

        private static Mesh? ResolveToMesh(object? raw, ref int convertedCount)
        {
            if (raw is Mesh m) return m;

            Brep? brep = raw as Brep;
            if (brep == null && raw is Surface s) brep = Brep.CreateFromSurface(s);
            if (brep == null) return null;

            var pieces = Mesh.CreateFromBrep(brep, MeshingParameters.Default);
            if (pieces == null || pieces.Length == 0) return null;

            var joined = new Mesh();
            foreach (var p in pieces) if (p != null && p.IsValid) joined.Append(p);
            if (joined.Faces.Count == 0) return null;

            convertedCount++;
            return joined;
        }
    }
}
