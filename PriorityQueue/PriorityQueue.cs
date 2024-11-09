
using System;
using System.Collections.Generic;

namespace YLCommon
{
    public class PriorityQueue<T> : Heap<T> where T : IComparable<T>
    {
        public PriorityQueue(int capacity) : base(capacity)
        {
        }

        public int Count => data.Count;
        public void Enqueue(T item)
        {
            AddNode(item);
        }

        public T? Dequeue()
        {
            return RemoveTop();
        }
    }
}
