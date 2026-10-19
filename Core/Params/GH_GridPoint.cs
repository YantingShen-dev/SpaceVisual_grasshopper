using System;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace SpaceVisual.Core.Params
{
    /// <summary>A point with optional grid-cell corners, carried with the data, not a global cache.</summary>
    public sealed class GH_GridPoint : GH_GeometricGoo<Point3d>, IGH_PreviewData, IGH_Goo
    {
        public Point3d[]? Corners { get; private set; }
        public bool HasCell => Corners != null && (Corners.Length == 3 || Corners.Length == 4);
        public Guid TopologyId { get; private set; }
        public int SourceVertex { get; private set; } = -1;
        public int[] Triangles { get; private set; } = Array.Empty<int>();
        public bool HasTopology => TopologyId != Guid.Empty && SourceVertex >= 0;
        public static GH_GridPoint Vertex(Point3d point, Guid topology, int index, int[] triangles)
            => new GH_GridPoint(point) { TopologyId = topology, SourceVertex = index, Triangles = (int[])triangles.Clone() };
        private GH_GridPoint Copy() => new GH_GridPoint(Value, Corners)
            { TopologyId = TopologyId, SourceVertex = SourceVertex, Triangles = (int[])Triangles.Clone() };
        public GH_GridPoint() : base(Point3d.Unset) { }
        public GH_GridPoint(Point3d point, Point3d[]? corners = null) : base(point)
            => Corners = corners == null ? null : (Point3d[])corners.Clone();
        public override bool IsValid => Value.IsValid;
        public override string TypeName => "Point";
        public override string TypeDescription => "Analysis point with optional source grid-cell corners.";
        public override string ToString() => new GH_Point(Value).ToString();
        public override BoundingBox Boundingbox => new BoundingBox(Value, Value);
        public BoundingBox ClippingBox => Boundingbox;
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var p = Value; p.Transform(xform); return new BoundingBox(p, p);
        }
        public override IGH_GeometricGoo DuplicateGeometry() => Copy();
        public override IGH_GeometricGoo Transform(Transform xform)
        {
            var result = Copy();
            var p = result.Value; p.Transform(xform); result.Value = p;
            if (result.Corners != null)
                for (int i = 0; i < result.Corners.Length; i++) result.Corners[i].Transform(xform);
            return result;
        }
        public override IGH_GeometricGoo Morph(SpaceMorph morph)
        {
            var result = Copy(); result.Value = morph.MorphPoint(Value);
            if (result.Corners != null)
                for (int i = 0; i < result.Corners.Length; i++) result.Corners[i] = morph.MorphPoint(result.Corners[i]);
            return result;
        }
        public override bool CastFrom(object source)
        {
            if (source is GH_ObjectWrapper wrapper) return CastFrom(wrapper.Value);
            if (source is GH_GridPoint grid)
            {
                Value = grid.Value;
                Corners = grid.Corners == null ? null : (Point3d[])grid.Corners.Clone();
                TopologyId = grid.TopologyId; SourceVertex = grid.SourceVertex; Triangles = (int[])grid.Triangles.Clone();
                return true;
            }
            var point = new GH_Point();
            if (source is GH_Point native) point = native;
            else if (!point.CastFrom(source)) return false;
            Value = point.Value; Corners = null; TopologyId = Guid.Empty; SourceVertex = -1; Triangles = Array.Empty<int>(); return true;
        }
        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q) == typeof(Point3d)) { target = (Q)(object)Value; return true; }
            if (typeof(Q) == typeof(GH_Point)) { target = (Q)(object)new GH_Point(Value); return true; }
            return new GH_Point(Value).CastTo(out target);
        }
        // Grasshopper consumers cast through IGH_Goo's out overload. Explicitly
        // route that interface call to our conversion rather than the base goo.
        bool IGH_Goo.CastTo<Q>(out Q target)
        {
            target = default!;
            return CastTo(ref target);
        }
        public override object ScriptVariable() => Value;
        public override bool Write(GH_IWriter writer)
        {
            writer.SetDouble("X", Value.X); writer.SetDouble("Y", Value.Y); writer.SetDouble("Z", Value.Z);
            writer.SetBoolean("HasCell", HasCell);
            writer.SetInt32("CornerCount", Corners?.Length ?? 0);
            if (HasCell)
                for (int i = 0; i < Corners!.Length; i++)
                {
                    writer.SetDouble("CX", i, Corners![i].X);
                    writer.SetDouble("CY", i, Corners[i].Y);
                    writer.SetDouble("CZ", i, Corners[i].Z);
                }
            writer.SetGuid("TopologyId", TopologyId); writer.SetInt32("SourceVertex", SourceVertex);
            writer.SetInt32("TriangleCount", Triangles.Length);
            for (int i = 0; i < Triangles.Length; i++) writer.SetInt32("TriangleIndex", i, Triangles[i]);
            return true;
        }
        public override bool Read(GH_IReader reader)
        {
            Corners = null;
            TopologyId = Guid.Empty; SourceVertex = -1; Triangles = Array.Empty<int>();
            if (!reader.ItemExists("X")) return false;
            Value = new Point3d(reader.GetDouble("X"), reader.GetDouble("Y"), reader.GetDouble("Z"));
            if (reader.ItemExists("HasCell") && reader.GetBoolean("HasCell"))
            {
                int count = reader.ItemExists("CornerCount") ? reader.GetInt32("CornerCount") : 4;
                if (count != 3 && count != 4) return false;
                Corners = new Point3d[count];
                for (int i = 0; i < count; i++)
                    Corners[i] = new Point3d(reader.GetDouble("CX", i), reader.GetDouble("CY", i), reader.GetDouble("CZ", i));
            }
            if (reader.ItemExists("TopologyId"))
            {
                TopologyId = reader.GetGuid("TopologyId"); SourceVertex = reader.GetInt32("SourceVertex");
                int count = reader.GetInt32("TriangleCount");
                if (count < 0 || count % 3 != 0) return false;
                Triangles = new int[count];
                for (int i = 0; i < count; i++) Triangles[i] = reader.GetInt32("TriangleIndex", i);
            }
            return true;
        }
        public void DrawViewportWires(GH_PreviewWireArgs args) => ((IGH_PreviewData)new GH_Point(Value)).DrawViewportWires(args);
        public void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    }
}
