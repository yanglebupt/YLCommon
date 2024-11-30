using System;

namespace YLCommon
{
  public static class Timer
  {
    private static readonly DateTime era = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    /// <summary>
    /// 距离计算纪元的毫秒数
    /// </summary>
    /// <returns>毫秒数</returns>
    public static double GetUTCMilliseconds()
    {
      TimeSpan ts = DateTime.UtcNow - era;
      return ts.TotalMilliseconds;
    }

    // 日志输出
    public class Logger
    {
        // 常规打印
        public Action<string>? info;
        // 警告打印
        public Action<string>? warn;
        // 错误打印
        public Action<string>? error;
    };

    public static Logger logger = new();
   }

  /// <summary>
  /// 定时器接口
  /// </summary>
  public abstract class ITimer
  {
    // 任务回调包
    protected class TaskPack
    {
        public uint tid;
        public Action<uint> cb;
        public TaskPack(uint tid, Action<uint> cb)
        {
            this.tid = tid;
            this.cb = cb;
        }
    }

    /// <summary>
    /// 创建一个定时任务
    /// </summary>
    /// <param name="delay">定时任务执行时间 ms 单位</param>
    /// <param name="taskCB">到时触发回调</param>
    /// <param name="cancelCB">取消触发的回调</param>
    /// <param name="count">定时任务重复次数，默认为 1，为 0 代表一直重复</param>
    /// <returns>如果添加成功，返回定时任务的唯一ID。返回 0，则添加失败</returns>
    public abstract uint AddTask(uint delay, Action<uint> taskCB, Action<uint> cancelCB, uint count = 1);

    /// <summary>
    /// 删除取消一个定时任务
    /// </summary>
    /// <param name="tid">定时任务的唯一ID</param>
    /// <returns>删除是否成功</returns>
    public abstract bool CancelTask(uint tid);

    /// <summary>
    /// 重置定时器，删除所有定时任务，并停止循环计时
    /// </summary>
    /// <returns></returns>
    public abstract void Reset();

    // 全局的计时 ID，是一个 ring loop 的形式
    protected uint tid = 0;
    protected abstract uint GenerateTid();
  }
}