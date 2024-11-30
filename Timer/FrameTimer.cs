using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace YLCommon
{
    /// <summary>
    /// 主要用于逻辑帧数的定时，只支持单线程添加任务，不依赖于 Unity
    /// 只能右外部循环驱动计数
    /// 定时回调也只能在驱动中运行
    /// </summary>
    public class FrameTimer : ITimer
    {
        class FrameTask
        {
            public uint tid;
            public uint delay;
            public uint count;
            public Action<uint> taskCB;
            public Action<uint> cancelCB;
            public ulong dstFrame;


            public FrameTask(uint tid, uint delay, uint count, Action<uint> taskCB, Action<uint> cancelCB, ulong dstFrame)
            {
                this.tid = tid;
                this.delay = delay;
                this.count = count;
                this.taskCB = taskCB;
                this.cancelCB = cancelCB;
                this.dstFrame = dstFrame;
            }
        }

        private ulong currentFrame;
        private readonly object mutex = new();
        private readonly Dictionary<uint, FrameTask> taskMap = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="frame">
        /// 计时起始帧，注意后面 AddTask 是以添加的时候的 currentFrame 为准，而不是这个起始帧
        /// Update 里面 currentFrame 会自增
        /// </param>
        public FrameTimer(ulong frame) {
            currentFrame = frame;
        }

        public override uint AddTask(uint delay, Action<uint> taskCB, Action<uint> cancelCB, uint count = 1)
        {
            uint tid = GenerateTid();
            FrameTask task = new FrameTask(tid, delay, count, taskCB, cancelCB, currentFrame+delay);
            if (taskMap.TryAdd(tid, task)) return tid;
            else
            {
                Timer.logger.warn?.Invoke($"Task: {tid} already exists!");
                return 0;
            }
        }

        public void Update()
        {
            List<uint> finishedTid = new List<uint>();
            currentFrame++;
            foreach (var item in taskMap)
            {
                uint tid = item.Key;
                FrameTask task = item.Value;
                if (currentFrame < task.dstFrame) continue;
                task.taskCB.Invoke(tid);
                task.dstFrame+=task.delay;
                // 有限循环
                if (task.count > 0)
                {
                    task.count--;
                    if (task.count == 0) finishedTid.Add(tid);
                }
            }

            for (int i = 0; i < finishedTid.Count; i++)
            {
                uint tid = finishedTid[i];
                if (!taskMap.Remove(tid))
                    Timer.logger.warn?.Invoke($"Task: {tid} remove after finish failed!");
            }
        }

        public override bool CancelTask(uint tid)
        {
            if (taskMap.TryGetValue(tid, out FrameTask task))
            {
                if (taskMap.Remove(tid))
                {
                    task.cancelCB?.Invoke(tid);
                    return true;
                }
                else
                {
                    Timer.logger.warn?.Invoke($"Task: {tid} cancel failed!");
                    return false;
                }
            }
            else
            {
                Timer.logger.warn?.Invoke($"Task: {tid} not exist!");
                return false;
            }
        }

        public override void Reset()
        {
            // 需要等待已完成的任务队列全部取出
            taskMap.Clear();
            tid = 0;
            currentFrame = 0;
        }

        protected override uint GenerateTid()
        {
            lock (mutex)
            {
                while (true)
                {
                    tid++;
                    if (tid == uint.MaxValue)
                        tid = 1;
                    if (!taskMap.ContainsKey(tid))
                        return tid;
                }
            }
        }
    }
}