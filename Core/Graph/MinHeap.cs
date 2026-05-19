using System;
using System.Collections.Generic;

namespace SpaceVisual.Core.Graph
{
    /// <summary>
    /// Minimal binary min-heap with (element, priority) entries. Polyfills
    /// System.Collections.Generic.PriorityQueue&lt;TElement, TPriority&gt; which is
    /// only available on .NET 6+ — Space Visual targets net48 so we ship our own.
    ///
    /// API mirrors PriorityQueue: Enqueue, TryDequeue, Count.
    /// </summary>
    public sealed class MinHeap<TElement>
    {
        private readonly struct Node
        {
            public readonly TElement Element;
            public readonly double Priority;
            public Node(TElement element, double priority) { Element = element; Priority = priority; }
        }

        private Node[] _heap;
        private int _count;

        public MinHeap(int capacity = 16)
        {
            _heap = new Node[Math.Max(1, capacity)];
            _count = 0;
        }

        public int Count => _count;

        public void Enqueue(TElement element, double priority)
        {
            if (_count == _heap.Length)
            {
                var bigger = new Node[_heap.Length * 2];
                Array.Copy(_heap, bigger, _count);
                _heap = bigger;
            }
            _heap[_count] = new Node(element, priority);
            SiftUp(_count);
            _count++;
        }

        public bool TryDequeue(out TElement element, out double priority)
        {
            if (_count == 0)
            {
                element = default!;
                priority = 0;
                return false;
            }
            var top = _heap[0];
            element = top.Element;
            priority = top.Priority;

            _count--;
            if (_count > 0)
            {
                _heap[0] = _heap[_count];
                SiftDown(0);
            }
            // Clear reference at vacated slot so the GC can reclaim element.
            _heap[_count] = default;
            return true;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_heap[i].Priority >= _heap[parent].Priority) break;
                (_heap[i], _heap[parent]) = (_heap[parent], _heap[i]);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int left = 2 * i + 1;
                int right = left + 1;
                int smallest = i;
                if (left  < _count && _heap[left].Priority  < _heap[smallest].Priority) smallest = left;
                if (right < _count && _heap[right].Priority < _heap[smallest].Priority) smallest = right;
                if (smallest == i) break;
                (_heap[i], _heap[smallest]) = (_heap[smallest], _heap[i]);
                i = smallest;
            }
        }
    }
}
