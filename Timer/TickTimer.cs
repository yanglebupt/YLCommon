using System;
using System.Collections.Concurrent;
using System.Threading;

namespace YLCommon
{
  /// <summary>
  /// 用于高频高精度的毫秒级定时，支持在多线程环境添加任务，不依赖于 Unity
  /// 支持外部循环计时（例如 Unity 的 Update 里面），支持内部新建一个线程进行循环计时
  /// 支持外部线程处理定时任务（主要是当定时任务执行很耗时，会堵塞循环计时，需要另开一个线程处理）
  /// </summary>
  public class TickTimer : ITimer
  {
    class TickTask
    {
      public uint tid;
      public uint delay;
      public uint count;
      public Action<uint> taskCB;
      public Action<uint> cancelCB;

      // 最开始的启动时间
      public double startTime;

      // 定时到达时间
      public double dstTime;

      public ulong loopIndex;

      public TickTask(uint tid, uint delay, uint count, Action<uint> taskCB, Action<uint> cancelCB, double startTime)
      {
        this.tid = tid;
        this.delay = delay;
        this.count = count;
        this.taskCB = taskCB;
        this.cancelCB = cancelCB;
        this.startTime = startTime;
        dstTime = startTime + Convert.ToDouble(delay);
        loopIndex = 0;
      }
    }

    private readonly object mutex = new();
    private readonly ConcurrentDictionary<uint, TickTask> taskMap = new();

    private readonly Thread? timerThread = null;

    private readonly bool external_handle = false;
    private readonly ConcurrentQueue<TaskPack>? taskPacks = null;

    private readonly int interval = 0;
    private readonly bool need_sleep = true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">循环检测的 ms 间隔，等于 0 代表使用外部循环，大于 0 则内部开启线程进行循环检测，越小检测越精确</param>
    /// <param name="need_sleep">循环检测开启循环检测的间隔，不开启则不会 sleep，计时很精准，但是线程负担很大</param>
    /// <param name="external_handle">是否使用外部处理任务回调</param>
    public TickTimer(int interval = 0, bool need_sleep = true, bool external_handle = false)
    {
      this.external_handle = external_handle;
      this.interval = interval;
      this.need_sleep = need_sleep;
      if (interval > 0)
      {
        timerThread = new Thread(Run);
        timerThread.Start();
      }
      if (external_handle)
        taskPacks = new();
    }
    private void Run()
    {
      try
      {
        while (true)
        {
          Update();
          if (need_sleep)
            Thread.Sleep(interval);
        }
      }
      catch (ThreadAbortException e)
      {
        Timer.logger.warn?.Invoke($"Tick Thread Aborted: {e.Message}");
      }
    }

    /// <summary>
    /// 检测定时任务的到达
    /// </summary>
    public void Update()
    {
      foreach (var item in taskMap)
      {
        TickTask task = item.Value;
        uint tid = task.tid;
        // 未到达
        double now = Timer.GetUTCMilliseconds();
        if (now < task.dstTime) continue;
        // 到达
        if (external_handle)
          taskPacks!.Enqueue(new TaskPack(task.tid, task.taskCB));
        else
          task.taskCB.Invoke(tid);
        task.loopIndex++;
        bool nextTask = true;
        // 有限重复
        if (task.count > 0)
        {
          task.count--;
          // 结束后移除，下一次就不会遍历了
          if (task.count == 0)
          {
            // 线程安全字典，遍历过程中可以移除
            if (!taskMap.TryRemove(tid, out TickTask _))
              Timer.logger.warn?.Invoke($"Task: {tid} remove after finish failed!");
            nextTask = false;
          }
        }

        // 更新定时目标时间，进行下一次定时
        if (nextTask)
          task.dstTime = task.startTime + Convert.ToDouble((task.loopIndex + 1) * task.delay);
      }
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
          Timer.logger.error?.Invoke("taskpack dequeue failed! ");
      }
    }

    public override uint AddTask(uint delay, Action<uint> taskCB, Action<uint> cancelCB, uint count = 1)
    {
      uint tid = GenerateTid();
      double startTime = Timer.GetUTCMilliseconds();
      TickTask task = new(tid, delay, count, taskCB, cancelCB, startTime);
      if (taskMap.TryAdd(tid, task)) return tid;
      else
      {
        Timer.logger.warn?.Invoke($"Task: {tid} already exists!");
        return 0;
      }
    }

    public override bool CancelTask(uint tid)
    {
      if (taskMap.TryRemove(tid, out TickTask task))
      {
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
        Timer.logger.warn?.Invoke($"Task: {tid} cancel failed!");
        return false;
      }
    }

    public override void Reset()
    {
      // 需要等待已完成的任务队列全部取出
      HandleTaskCallback();
      taskPacks?.Clear();
      taskMap.Clear();
      timerThread?.Abort();
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