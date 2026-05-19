using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace SpaceVisual.Core.Graph
{
    /// <summary>
    /// Immutable visibility graph: for each source point, the indices of points
    /// it can see without obstacle occlusion. The same object is consumed by
    /// SV_VGAMetrics2D, SV_FromViewpoint2D, and SV_VisualPath2D, so it is the
    /// single most important shared data structure in the 2D analysis pipeline.
    /// </summary>
    public sealed class VisibilityGraph
    {
        public Point3d[] Points { get; }

        /// <summary>Adjacency[i] = sorted list of indices visible from point i.</summary>
        public int[][] Adjacency { get; }

        public int Count => Points.Length;

        public VisibilityGraph(Point3d[] points, int[][] adjacency)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));
            if (points.Length != adjacency.Length)
                throw new ArgumentException("points and adjacency must have the same length.");

            Points = points;
            Adjacency = adjacency;
        }

        public int Degree(int i) => Adjacency[i].Length;

        /// <summary>
        /// Enumerate unique edges (i,j) with j > i. Useful for producing the
        /// visible-line geometry without duplicates.
        /// </summary>
        public IEnumerable<(int i, int j)> Edges()
        {
            for (int i = 0; i < Adjacency.Length; i++)
            {
                var row = Adjacency[i];
                for (int k = 0; k < row.Length; k++)
                {
                    int j = row[k];
                    if (j > i) yield return (i, j);
                }
            }
        }

        /// <summary>Materialise visible line segments (i→j with j&gt;i).</summary>
        public Line[] GetVisibleLines()
        {
            int total = 0;
            for (int i = 0; i < Adjacency.Length; i++)
            {
                var row = Adjacency[i];
                for (int k = 0; k < row.Length; k++)
                    if (row[k] > i) total++;
            }

            var lines = new Line[total];
            int idx = 0;
            for (int i = 0; i < Adjacency.Length; i++)
            {
                var row = Adjacency[i];
                for (int k = 0; k < row.Length; k++)
                {
                    int j = row[k];
                    if (j > i) lines[idx++] = new Line(Points[i], Points[j]);
                }
            }
            return lines;
        }
    }
}
