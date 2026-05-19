using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaceVisual.Core.Graph
{
    /// <summary>
    /// Breadth-first search over a VisibilityGraph. All metrics in
    /// SV_VGAMetrics2D and SV_FromViewpoint2D ultimately derive from
    /// the depth array(s) produced here.
    /// </summary>
    public static class BFS
    {
        /// <summary>
        /// Single-source BFS. Returns step depth from <paramref name="start"/>
        /// to each node; -1 if the node is in a different connected component.
        /// </summary>
        public static int[] FromSource(VisibilityGraph g, int start)
        {
            int n = g.Count;
            var depth = new int[n];
            for (int i = 0; i < n; i++) depth[i] = -1;
            if (n == 0 || start < 0 || start >= n) return depth;

            depth[start] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                int du = depth[u];
                var neighbors = g.Adjacency[u];
                for (int k = 0; k < neighbors.Length; k++)
                {
                    int v = neighbors[k];
                    if (depth[v] < 0)
                    {
                        depth[v] = du + 1;
                        queue.Enqueue(v);
                    }
                }
            }
            return depth;
        }

        /// <summary>
        /// All-pairs BFS streamed row by row. <paramref name="onRow"/> is invoked
        /// once per source with (sourceIndex, depthArray). The depth array is
        /// reused between calls, so callers must copy it if they need to retain it.
        ///
        /// SV_VGAMetrics2D consumes this directly: integration / entropy /
        /// connectivity / control / clustering are all derivable from per-row data
        /// + the adjacency table, never requiring the full N×N matrix in memory.
        /// </summary>
        public static void AllPairsEnumerate(VisibilityGraph g, Action<int, int[]> onRow)
        {
            if (onRow == null) throw new ArgumentNullException(nameof(onRow));
            int n = g.Count;
            if (n == 0) return;

            var depth = new int[n];
            var queue = new Queue<int>();

            for (int s = 0; s < n; s++)
            {
                for (int i = 0; i < n; i++) depth[i] = -1;
                depth[s] = 0;
                queue.Clear();
                queue.Enqueue(s);

                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    int du = depth[u];
                    var neighbors = g.Adjacency[u];
                    for (int k = 0; k < neighbors.Length; k++)
                    {
                        int v = neighbors[k];
                        if (depth[v] < 0)
                        {
                            depth[v] = du + 1;
                            queue.Enqueue(v);
                        }
                    }
                }
                onRow(s, depth);
            }
        }

        /// <summary>
        /// All-pairs BFS materialised into a dense matrix. Memory: 4·N² bytes.
        /// Prefer <see cref="AllPairsEnumerate"/> when N is large.
        /// </summary>
        public static int[,] AllPairs(VisibilityGraph g)
        {
            int n = g.Count;
            var matrix = new int[n, n];
            AllPairsEnumerate(g, (s, row) =>
            {
                for (int i = 0; i < n; i++) matrix[s, i] = row[i];
            });
            return matrix;
        }

        private sealed class ThreadLocalBfsState
        {
            public int[] Depth;
            public Queue<int> Queue;
            public ThreadLocalBfsState(int n)
            {
                Depth = new int[n];
                Queue = new Queue<int>(Math.Min(n, 64));
            }
        }

        /// <summary>
        /// Parallel all-pairs BFS. Each worker thread keeps its own depth/queue
        /// scratch, so the callback receives a thread-local depth array that
        /// must not be retained beyond the call. Callbacks fire concurrently
        /// — they may write to disjoint indices of shared output arrays without
        /// locking, but anything else they touch must be thread-safe.
        /// </summary>
        public static void AllPairsEnumerateParallel(VisibilityGraph g, Action<int, int[]> onRow)
        {
            if (onRow == null) throw new ArgumentNullException(nameof(onRow));
            int n = g.Count;
            if (n == 0) return;

            Parallel.For(0, n,
                () => new ThreadLocalBfsState(n),
                (s, _, local) =>
                {
                    var depth = local.Depth;
                    for (int i = 0; i < n; i++) depth[i] = -1;
                    depth[s] = 0;
                    local.Queue.Clear();
                    local.Queue.Enqueue(s);

                    while (local.Queue.Count > 0)
                    {
                        int u = local.Queue.Dequeue();
                        int du = depth[u];
                        var neighbors = g.Adjacency[u];
                        for (int k = 0; k < neighbors.Length; k++)
                        {
                            int v = neighbors[k];
                            if (depth[v] < 0)
                            {
                                depth[v] = du + 1;
                                local.Queue.Enqueue(v);
                            }
                        }
                    }

                    onRow(s, depth);
                    return local;
                },
                _ => { });
        }
    }
}
