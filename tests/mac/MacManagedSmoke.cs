using System;
using System.Linq;
using System.Reflection;
using System.Drawing;
using Rhino.Geometry;
using Grasshopper.Kernel;
using SpaceVisual.Core.Graph;
using SpaceVisual.Core.Params;
using Grasshopper.Kernel.Types;
using SpaceVisual.Core.Visual;
class MacManagedSmoke
{
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS: " + label); }
    static void Main()
    {
        var asm = typeof(VisibilityGraph).Assembly;
        Check(asm.GetName().Name == "SpaceVisual", "Stable production assembly identity");
        Check(asm.GetName().Version == new Version(0,2,3,0), "Version 0.2.3.0");
        Check(asm.GetName().ProcessorArchitecture == ProcessorArchitecture.MSIL, "AnyCPU assembly");
        Check(asm.GetTypes().Count(t => !t.IsAbstract && typeof(GH_Component).IsAssignableFrom(t)) == 11, "All 11 component types resolve against Mac Rhino/Grasshopper");
        Check(asm.GetManifestResourceNames().Count(n => n.EndsWith(".png")) == 12, "12 embedded icons");
        var gridPoint = new GH_GridPoint(new Point3d(1,2,3));
        GH_Point converted;
        Check(((IGH_Goo)gridPoint).CastTo(out converted) && converted.Value == gridPoint.Value, "Grid point converts through IGH_Goo to native GH_Point");
        Point3d rawPoint;
        Check(((IGH_Goo)gridPoint).CastTo(out rawPoint) && rawPoint == gridPoint.Value, "Grid point converts through IGH_Goo to Point3d");
        var pointParam = new Grasshopper.Kernel.Parameters.Param_Point();
        Check(pointParam.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, gridPoint), "Native Point parameter accepts grid-point output");
        Check(((GH_Point)pointParam.VolatileData.get_Branch(0)[0]).Value == gridPoint.Value, "Native Point parameter preserves coordinates");
        var graph = new VisibilityGraph(new[]{new Point3d(0,0,0),new Point3d(1,0,0),new Point3d(2,0,0),new Point3d(8,0,0)}, new[]{new[]{1},new[]{0,2},new[]{1},new int[0]});
        Check(BFS.FromSource(graph,0).SequenceEqual(new[]{0,1,2,-1}), "BFS connected and disconnected nodes");
        var matrix = BFS.AllPairs(graph);
        bool same = true;
        BFS.AllPairsEnumerateParallel(graph, (i, row) => { for(int j=0;j<row.Length;j++) if(matrix[i,j] != row[j]) same=false; });
        Check(same, "Parallel BFS matches serial traversal on Mono");
        var heap = new MinHeap<int>(1);
        foreach(int n in new[]{4,1,8,2,0,5}) heap.Enqueue(n,n);
        var values = new System.Collections.Generic.List<int>();
        while(heap.TryDequeue(out int value,out double priority)) values.Add(value);
        Check(values.SequenceEqual(new[]{0,1,2,4,5,8}), "Priority queue ordering and resizing");
        var gradient = new HeatGradient(new[]{(0.0,Color.Black),(1.0,Color.White)});
        var colors = gradient.MapValues(new[]{0.0,1.0,double.NaN});
        Check(colors[0].ToArgb()==Color.Black.ToArgb() && colors[1].ToArgb()==Color.White.ToArgb() && colors[2].R==128, "Heatmap endpoints and missing values");
        Console.WriteLine("Managed smoke checks passed. Native geometry, icons and viewport still require Rhino host testing.");
    }
}
