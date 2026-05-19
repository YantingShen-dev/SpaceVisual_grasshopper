using System;
using System.Drawing;
using Rhino.Geometry;

namespace SpaceVisual.Core.Visual
{
    /// <summary>
    /// Applies a per-vertex or per-face color array to a Mesh, returning a new
    /// colored Mesh. Auto-routes between vertex- and face- coloring based on
    /// the color array length, which directly supports the three Space Visual
    /// visualisation modes:
    ///   A  · per-viewpoint     (colors aligned with a viewpoint mesh's vertices)
    ///   B 2D · region grid     (colors aligned with region mesh vertices)
    ///   B 3D · obstacle faces  (colors aligned with obstacle mesh faces)
    /// </summary>
    public static class MeshColorizer
    {
        public enum Alignment { Auto, Vertex, Face }

        /// <summary>
        /// Color a mesh and return a new instance. Original mesh is not mutated.
        /// Throws if alignment cannot be resolved or counts mismatch.
        /// </summary>
        public static Mesh Apply(Mesh source, Color[] colors, Alignment alignment = Alignment.Auto)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (colors == null) throw new ArgumentNullException(nameof(colors));

            int vCount = source.Vertices.Count;
            int fCount = source.Faces.Count;

            var resolved = alignment;
            if (resolved == Alignment.Auto)
            {
                if (colors.Length == vCount) resolved = Alignment.Vertex;
                else if (colors.Length == fCount) resolved = Alignment.Face;
                else throw new ArgumentException(
                    $"Color count {colors.Length} matches neither vertex count {vCount} nor face count {fCount}.");
            }

            return resolved switch
            {
                Alignment.Vertex => ColorByVertices(source, colors),
                Alignment.Face   => ColorByFaces(source, colors),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Vertex coloring. Colors.Length must equal source.Vertices.Count.
        /// </summary>
        public static Mesh ColorByVertices(Mesh source, Color[] colors)
        {
            if (colors.Length != source.Vertices.Count)
                throw new ArgumentException(
                    $"Need {source.Vertices.Count} colors (vertex count), got {colors.Length}.");

            var m = source.DuplicateMesh();
            m.VertexColors.Clear();
            m.VertexColors.AppendColors(colors);
            return m;
        }

        /// <summary>
        /// Face coloring. Splits each face into its own triangle/quad with
        /// duplicated vertices so adjacent faces don't blend across shared edges.
        /// Output mesh has 3·triCount + 4·quadCount vertices.
        /// </summary>
        public static Mesh ColorByFaces(Mesh source, Color[] colors)
        {
            if (colors.Length != source.Faces.Count)
                throw new ArgumentException(
                    $"Need {source.Faces.Count} colors (face count), got {colors.Length}.");

            var result = new Mesh();
            var verts = source.Vertices;

            for (int f = 0; f < source.Faces.Count; f++)
            {
                var face = source.Faces[f];
                var color = colors[f];

                if (face.IsTriangle)
                {
                    int v0 = result.Vertices.Add(verts[face.A]);
                    int v1 = result.Vertices.Add(verts[face.B]);
                    int v2 = result.Vertices.Add(verts[face.C]);
                    result.Faces.AddFace(v0, v1, v2);
                    result.VertexColors.SetColor(v0, color);
                    result.VertexColors.SetColor(v1, color);
                    result.VertexColors.SetColor(v2, color);
                }
                else // quad
                {
                    int v0 = result.Vertices.Add(verts[face.A]);
                    int v1 = result.Vertices.Add(verts[face.B]);
                    int v2 = result.Vertices.Add(verts[face.C]);
                    int v3 = result.Vertices.Add(verts[face.D]);
                    result.Faces.AddFace(v0, v1, v2, v3);
                    result.VertexColors.SetColor(v0, color);
                    result.VertexColors.SetColor(v1, color);
                    result.VertexColors.SetColor(v2, color);
                    result.VertexColors.SetColor(v3, color);
                }
            }

            result.Normals.ComputeNormals();
            result.Compact();
            return result;
        }
    }
}
