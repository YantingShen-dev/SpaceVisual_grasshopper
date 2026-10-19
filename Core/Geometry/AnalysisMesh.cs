using System;
using System.Collections.Generic;
using Rhino.Geometry;
using SpaceVisual.Core.Params;

namespace SpaceVisual.Core.Geometry
{
    public static class AnalysisMesh
    {
        public static List<GH_GridPoint> FaceSamples(Mesh mesh)
        {
            var points = new List<GH_GridPoint>();
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var f = mesh.Faces[i];
                var corners = f.IsTriangle
                    ? new[] { (Point3d)mesh.Vertices[f.A], (Point3d)mesh.Vertices[f.B], (Point3d)mesh.Vertices[f.C] }
                    : new[] { (Point3d)mesh.Vertices[f.A], (Point3d)mesh.Vertices[f.B], (Point3d)mesh.Vertices[f.C], (Point3d)mesh.Vertices[f.D] };
                points.Add(new GH_GridPoint(mesh.Faces.GetFaceCenter(i), corners));
            }
            return points;
        }

        public static List<GH_GridPoint> VertexSamples(IList<Point3d> points, Mesh? mesh)
        {
            var incident = new List<int>[points.Count];
            for (int i = 0; i < incident.Length; i++) incident[i] = new List<int>();
            if (mesh != null)
                foreach (var face in mesh.Faces)
                {
                    int[] triangle = { face.A, face.B, face.C };
                    foreach (int vertex in triangle) incident[vertex].AddRange(triangle);
                }
            var id = Guid.NewGuid();
            var result = new List<GH_GridPoint>();
            for (int i = 0; i < points.Count; i++) result.Add(GH_GridPoint.Vertex(points[i], id, i, incident[i].ToArray()));
            return result;
        }

        public static Mesh? Build(IList<GH_GridPoint> samples, IList<int> kept, AnalysisDomain2D? domain = null)
        {
            var mesh = new Mesh();
            if (kept.Count == 0) return mesh;
            bool faces = true, vertices = true;
            foreach (int i in kept) { faces &= samples[i].HasCell; vertices &= samples[i].HasTopology; }
            if (faces)
            {
                foreach (int i in kept)
                {
                    var corners = samples[i].Corners!;
                    int first = mesh.Vertices.Count; mesh.Vertices.AddVertices(corners);
                    if (corners.Length == 3) mesh.Faces.AddFace(first, first+1, first+2);
                    else mesh.Faces.AddFace(first, first+1, first+2, first+3);
                }
            }
            else if (vertices)
            {
                var map = new Dictionary<(Guid, int), int>();
                foreach (int i in kept)
                {
                    var p = samples[i];
                    var key = (p.TopologyId, p.SourceVertex);
                    if (map.ContainsKey(key)) { mesh.Dispose(); return null; }
                    map.Add(key, mesh.Vertices.Add(p.Value));
                }
                var seen = new HashSet<(int, int, int)>();
                using var occluder = domain == null ? null : new CurveOccluder(domain.Blocking);
                foreach (int i in kept)
                {
                    var p = samples[i];
                    for (int j = 0; j < p.Triangles.Length; j += 3)
                    {
                        if (!map.TryGetValue((p.TopologyId,p.Triangles[j]), out int a) ||
                            !map.TryGetValue((p.TopologyId,p.Triangles[j+1]), out int b) ||
                            !map.TryGetValue((p.TopologyId,p.Triangles[j+2]), out int c)) continue;
                        if (!seen.Add((a,b,c))) continue;
                        Point3d x=mesh.Vertices[a], y=mesh.Vertices[b], z=mesh.Vertices[c];
                        if (domain != null && (!domain.Allows((x+y+z)/3.0) ||
                            !domain.Allows((x+y)/2.0) || !domain.Allows((y+z)/2.0) || !domain.Allows((z+x)/2.0) ||
                            occluder!.IsBlocked(x,y, blockOverlap:false) || occluder.IsBlocked(y,z, blockOverlap:false) || occluder.IsBlocked(z,x, blockOverlap:false))) continue;
                        mesh.Faces.AddFace(a,b,c);
                    }
                }
                // Keep even isolated retained vertices: values remain in exactly this order.
            }
            else { mesh.Dispose(); return null; }
            mesh.Normals.ComputeNormals();
            return mesh;
        }
    }
}
