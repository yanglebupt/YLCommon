using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace YLCommon
{
    /// <summary>
    /// 用于大量并发任务的定时，支持在多线程环境添加任务，不依赖于 Unity
    /// 采用 async/await 异步驱动计时，是线程池循环计时，不支持外部循环计时
    /// 同样支持在外部线程处理定时任务回调
    /// </summary>
    public class AsyncTimer : ITimer
    {
        class AsyncTask
        {
            public uint tid;
            public uint delay;
            public uint count;
            public Action<uint> taskCB;
            public Action<uint> cancelCB;

            // 最开始的启动时间
            public DateTime startTime;

            public ulong loopIndex;

            // 取消异步任务的信号
            public CancellationTokenSource cts;
            public CancellationToken ct;

            // 修复计时偏移
            public double fixDelta;

            public AsyncTask(uint tid, uint delay, uint count, Action<uint> taskCB, Action<uint> cancelCB, DateTime startTime)
            {
                this.tid = tid;
                this.delay = delay;
                this.count = count;
                this.taskCB = taskCB;
                this.cancelCB = cancelCB;
                this.startTime = startTime;
                loopIndex = 0;
                cts = new CancellationTokenSource();
                ct = cts.Token;
                fixDelta = 0;
            }
        }

        private readonly object mutex = new();
        private readonly ConcurrentDictionary<uint, AsyncTask> taskMap = new();

        private readonly bool external_handle = false;
        private readonly ConcurrentQueue<TaskPack>? taskPacks = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="external_handle">是否使用外部处理任务回调</param>
        public AsyncTimer(bool external_handle = false)
        {
            this.external_handle = external_handle;
            if (external_handle)
                taskPacks = new();
        }

        /// <summary>
        /// 用于从已完成的任务队列中取出回调执行，可以放在不同的线程环境中执行
        /// </summary>
        public void HandleTaskCallback()
        {
            while (taskPacks != null && taskPacks.Count > 0)
            {
                if (taskPacks.TryDequeue(out TaskPack tp))
                    tp.cb.Invoke(tp.tid);
                else
                    logger.error?.Invoke("taskpack dequeue failed! ");
            }
        }

        public override uint AddTask(uint delay, Action<uint> taskCB, Action<uint> cancelCB, uint count = 1)
        {
            uint tid = GenerateTid();
            AsyncTask task = new AsyncTask(tid, delay, count, taskCB, cancelCB, DateTime.UtcNow);
            if (taskMap.TryAdd(tid, task)) {
                // 添加一个异步任务，用于计时
                Task.Run(async () =>
                {
                    bool finite = task.count > 0;
                    Func<bool> cond = finite ? () => task.count > 0 : () => true;
                    while (cond())
                    {
                        double d = task.delay + task.fixDelta;
                        if (d > 0) 
                            await Task.Delay(TimeSpan.FromMilliseconds(d), task.ct);
                        // 到达
                        if (external_handle)
                            taskPacks!.Enqueue(new TaskPack(task.tid, task.taskCB));
                        else
                            task.taskCB.Invoke(tid);
                        if (finite) task.count--;
                        task.loopIndex++;
                        // 计算偏移，一般是负号
                        TimeSpan ts = task.startTime.AddMilliseconds(Convert.ToDouble(task.loopIndex * task.delay)) - DateTime.UtcNow;
                        task.fixDelta = ts.TotalMilliseconds;
                    }
                    // 完成了，需要移除
                    if (finite && !taskMap.TryRemove(tid, out AsyncTask _))
                        logger.warn?.Invoke($"Task: {tid} remove after finish failed!");
                });
                return tid;
            }
            else
            {
                logger.warn?.Invoke($"Task: {tid} already exists!");
                return 0;
            }
        }

        public override bool CancelTask(uint tid)
        {
            if (taskMap.TryRemove(tid, out AsyncTask task))
            {
                // 取消异步任务
                task.cts.Cancel();
                if (task.cancelCB != null)
                {
                    if (external_handle)
                        taskPacks!.Enqueue(new TaskPack(tid, task.cancelCB));
                    else
                        task.cancelCB.Invoke(tid);
                }
                return true;
            }
            else
            {
                logger.warn?.Invoke($"Task: {tid} cancel failed!");
                return false;
            }
        }

        public override void Reset()
        {
            // 需要等待已完成的任务队列全部取出
            taskMap.Clear();
            tid = 0;
        }

        protected override uint GenerateTid()
        {
            lock (mutex)
            {
                while (true)
                {
                    tid++;
                    if (tid == uint.MaxValue)
                        tid = 0;
                    if (!taskMap.ContainsKey(tid))
                        return tid;
                }
            }
        }
    }

}
