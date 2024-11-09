
using System;
using System.Collections.Generic;

namespace YLCommon
{
    /// <summary>
    /// 基于完全二叉树实现的小顶堆
    /// </summary>
    public class Heap<T> where T : IComparable<T>
    {
        protected readonly List<T> data;
        public Heap(int capacity) {
            data = new(capacity);
        }

        /// <summary>
        /// 往堆里面添加一个节点
        /// </summary>
        /// <param name="value">可比较节点</param>
        public void AddNode(T value)
        {
            // 新节点插入最末端
            data.Add(value);
            UpHeapify(data.Count - 1);
        }

        /// <summary>
        /// 移除堆顶节点：
        /// 
        /// 1. 交换首尾节点，移除尾节点
        /// 2. 将首节点向下堆化
        /// </summary>
        /// <returns>返回被移除的堆顶元素</returns>
        public T? RemoveTop()
        {
            if(data.Count == 0) return default;

            T value = data[0];
            int endIndex = data.Count - 1;
            // 交换首尾节点，移除尾节点
            data[0] = data[endIndex];
            data.RemoveAt(endIndex);

            DownHeapify(0);
            return value;
        }

        public T? Top()
        {
            if (data.Count == 0) return default;
            return data[0];
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public bool IsEmpty()
        {
            return data.Count == 0;
        }

        public List<T> ToList()
        {
            return data;
        }

        public T[] ToArray()
        {
            return data.ToArray();
        }

        public int IndexOf(T value)
        {
            return data.IndexOf(value);
        }

        /// <summary>
        /// 移除具体元素
        /// </summary>
        /// <param name="value"></param>
        public T? Remove(T value)
        {
            int index = IndexOf(value);
            return RemoveAt(index);
        }

        /// <summary>
        /// 移除具体索引位置元素
        /// </summary>
        /// <param name="value"></param>
        public T? RemoveAt(int index)
        {
            if(index < 0 || index > data.Count - 1) return default;


            T value = data[index];
            int endIndex = data.Count - 1;

            bool isEnd = index == endIndex;

            // 移除末尾元素很简单
            if (isEnd)
                data.RemoveAt(endIndex);
            // 移除的不是末尾元素
            else
            {
                // 和尾节点交换，移除尾节点
                data[index] = data[endIndex];
                data.RemoveAt(endIndex);

                int parentIndex = (index - 1) / 2;
                // 先判断是否上堆化，如果上堆化，则不需要进行下堆化
                if (parentIndex >= 0 && data[index].CompareTo(data[parentIndex]) < 0)
                    UpHeapify(index);
                // 再尝试进行向下堆化
                else
                    DownHeapify(index);
            }

            return value;
        }


        /// <summary>
        /// 从子节点开始向上堆化：
        /// 
        /// 1. 插入节点和父节点比较，小于父节点，进行交换
        ///    不需要考虑左兄弟节点，因为如果插入节点小于父节点，而左兄弟节点是大于父节点
        ///    那么左兄弟节点必然大于插入节点，将父节点和插入节点交换后，仍然满足小堆要求
        /// 
        ///  2. 大于父节点，不需要处理
        /// </summary>
        private void UpHeapify(int childIndex) {
            int parentIndex = (childIndex - 1) / 2;
            if (parentIndex >= 0 && data[childIndex].CompareTo(data[parentIndex]) < 0)
            {
                (data[childIndex], data[parentIndex]) = (data[parentIndex], data[childIndex]);
                UpHeapify(parentIndex);
            }
        }

        /// <summary>
        /// 从父节点开始向下堆化：
        /// 
        /// 父节点和左右子节点比较，和最小的子节点进行交换
        /// </summary>
        private void DownHeapify(int parentIndex)
        {
            int minIndex = parentIndex;

            // 先比较左边
            int childIndex = parentIndex * 2 + 1;
            if (childIndex <= data.Count - 1 && data[childIndex].CompareTo(data[parentIndex]) < 0)
                minIndex = childIndex;

            // 再比较右边
            childIndex++;
            if (childIndex <= data.Count - 1 && data[childIndex].CompareTo(data[minIndex]) < 0)
                minIndex = childIndex;

            // 顶部元素已是最小
            if (minIndex == parentIndex) return;

            // 交换
            (data[minIndex], data[parentIndex]) = (data[parentIndex], data[minIndex]);
            DownHeapify(minIndex);
        }
    }
}
