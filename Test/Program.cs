using System.IO;
using System.Net.Sockets;
using System.Xml.Linq;
using YLCommon;

#region LoggerTest
Logger.cfg.enableSave = true;
Logger.cfg.saveOverride = false;
Logger.cfg.showTrace = false;
Console.WriteLine(Logger.EnableSetting());

/*
Logger.Log("Log {0}-{1}", "Mike", 10);
Logger.ColorLog(LogColor.Cyan, "ColorLog Cyan {0}-{1}", "Mike", 10);
Logger.ColorLog(LogColor.Magenta, "ColorLog Magenta {0}-{1}", "Mike", 10);
Logger.Warn("Warn {0}-{1}", "Mike", 10);
Logger.Error("Error {0}-{1}", "Mike", 10);

Logger.Log("Log {0}-{1}", "Tom", 10);
Logger.ColorLog(LogColor.Cyan, "ColorLog Cyan {0}-{1}", "Tom", 10);
Logger.ColorLog(LogColor.Magenta, "ColorLog Magenta {0}-{1}", "Tom", 10);
Logger.Warn("Warn {0}-{1}", "Tom", 10);
Logger.Error("Error {0}-{1}", "Tom", 10);

new LoggerTest().Print();
*/
#endregion

#region TimerTest
/*
// TickTimer timer = new TickTimer(10, true, true);
AsyncTimer timer = new AsyncTimer(false);
uint interval = 66;
uint count = 50;
int error_sum = 0;
int loops = 0;
uint tid = 0;
Task _ = Task.Run(async () =>
{
    await Task.Delay(2000);
    DateTime his = DateTime.UtcNow;
    tid = timer.AddTask(
        interval, 
        (uint tid) => { 
            DateTime now = DateTime.UtcNow;
            int delta = (int)((now - his).TotalMilliseconds - interval);
            Logger.Warn($"Tid: {tid} 任务运行，偏差时间: {delta} ms");
            error_sum += Math.Abs(delta);
            his = now;
            loops++;
        }, 
        (uint tid) => {
            Logger.Error($"Tid: {tid} 任务取消");
        },
        count);
});

// 外部处理循环计时和任务回调
Task __ = Task.Run(async () =>
{
    while (true)
    {
        // timer.Update();
        // timer.HandleTaskCallback();
        // await Task.Delay(1);
    }
});

while (true)
{
    string? ipt = Console.ReadLine();
    if (ipt == "calc") 
        Logger.Warn($"平均偏差: {error_sum * 1f / loops} ms");
    else if(ipt == "del")
        timer.CancelTask(tid);
}
*/
#endregion

#region FrameTimerTest
/*
ulong frame = 100;
uint interval = 13;
FrameTimer ft = new FrameTimer(frame);
uint tid = 0;
Task.Run(async () =>
{
    await Task.Delay(2000);
    ulong his = frame;
    tid = ft.AddTask(interval, (uint tid) => {
        ulong delta = frame - his - interval;
        his = frame;
        Logger.Warn($"Tid: {tid} 任务运行，偏差帧数: {delta}");
    }, (uint tid) => {
        Logger.Error($"Tid: {tid} 任务取消");
    }, 0);

    while (true)
    {
        await Task.Delay(66);
        frame++;
        ft.Update();
    }
});

while (true)
{
    string? ipt = Console.ReadLine();
    if (ipt == "del")
        ft.CancelTask(tid);
}
*/
#endregion

#region NetworkTest

NetworkConfig.logger.warn = Logger.Warn;
NetworkConfig.logger.error = Logger.Error;
NetworkConfig.logger.info = Logger.Log;
NetworkConfig.logger.ok = (string m) => Logger.ColorLog(LogColor.Green, m);

Client client = new("127.0.0.1", 3000);

/*
TCPClient<NetMsg> client = new("127.0.0.1", 3000);
client.OnConnected += () =>
{
    Logger.ColorLog(LogColor.Green, "Connected OK");
};
client.OnConnectionFailed += () =>
{
    Logger.Error("Connection Failed");
};
client.OnDisconnected += () =>
{
    Logger.Warn("Disconnect");
};
client.OnMessage += (NetMsg msg) => {
    Logger.ColorLog(LogColor.Green, msg.name);
};
*/


while (true)
{
    string? ipt = Console.ReadLine();
    if (ipt == null) continue;
    if (ipt == "del") client.Close();
    else
    {
        NetMsg msg = new NetMsg() { name = ipt };
        client.Send(msg);
    }
}

#endregion

/*

UsingReaderWriterLockSlim s = new();

using (s.UpgradeableRead()) {

    using (s.Read())
    {

        using (s.Write(true))
        {
            Console.WriteLine(s.LockMode);
        }

        Console.WriteLine(s.LockMode);

    }

    Console.WriteLine(s.LockMode);

}

Console.WriteLine(s.LockMode);

MemoryStream ms = new();

string text = "Hello, World!";
byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
ms.Write(data, 1, data.Length-1);

foreach (var item in BitConverter.GetBytes(1))
{
    Console.Write(item);
    Console.Write(",");
}
Console.WriteLine(BitConverter.IsLittleEndian);

ms.Seek(0, SeekOrigin.Begin);

byte[] buffer = new byte[13];
int bytesRead = ms.Read(buffer, 1, buffer.Length-1);
Console.WriteLine(bytesRead);
Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, 1, bytesRead));


/// <summary>
/// 使用 using 的语法糖来获取锁/释放锁
/// </summary>
public class UsingReaderWriterLockSlim
{
    /// <summary>
    /// 读写锁的模式，普通 rw 之间不能直接切换，必须先 exit
    /// rw/UpgradeableRead 和 UpgradeableRead 不能直接切换
    /// UpgradeableRead 和 rw 可以直接切换
    /// </summary>
    public enum ReaderWriterLockSlimMode
    {
        None,
        Write,
        Read,
        /// <summary>
        /// 是读的一种特殊状态，可以直接（不需要 exit）升级到 Write，也可以直接（不需要 exit）降级到普通的 Read
        /// 在任何给定时间，只有一个线程可以进入可升级模式。 
        /// 如果线程处于可升级模式，并且没有线程等待进入写入模式，则任何其他线程都可以进入读取模式，即使有线程正在等待进入可升级模式。
        /// 如果一个或多个线程正在等待进入写入模式，则调用方法的 EnterUpgradeableReadLock 线程会阻塞，直到这些线程超时或进入写入模式，然后退出该模式。
        /// </summary>
        UpgradeableRead,
    }

    public static ReaderWriterLockSlimMode GetLockMode(ReaderWriterLockSlim _rwLock)
    {
        if (_rwLock.IsReadLockHeld) return ReaderWriterLockSlimMode.Read;
        else if (_rwLock.IsWriteLockHeld) return ReaderWriterLockSlimMode.Write;
        else if (_rwLock.IsUpgradeableReadLockHeld) return ReaderWriterLockSlimMode.UpgradeableRead;
        else return ReaderWriterLockSlimMode.None;
    }

    /// <summary>
    /// 自动管理锁的释放
    /// </summary>
    private class Lock : IDisposable
    {
        /// <summary>
        /// 引用的读写锁，外部传入
        /// </summary>
        private readonly ReaderWriterLockSlim? _rwLock = null;
        private readonly ReaderWriterLockSlimMode _mode;

        public Lock(ReaderWriterLockSlim rwLock, ReaderWriterLockSlimMode mode, bool ForceExitLast = false)
        {
            if (mode == ReaderWriterLockSlimMode.None) return;

            bool rw = rwLock.IsReadLockHeld || rwLock.IsWriteLockHeld;

            // 如果是要切换到 UpgradeableRead，那么前面的锁不能处于任何一个状态，如果是切换到正常的 rw，那么前面可以是 UpgradeableRead
            if ((mode == ReaderWriterLockSlimMode.UpgradeableRead) ? (rwLock.IsUpgradeableReadLockHeld || rw): rw)
            {
                if (ForceExitLast) ExitLock(rwLock);
                else return;
            }

            if (mode == ReaderWriterLockSlimMode.Write) rwLock.EnterWriteLock();
            else if (mode == ReaderWriterLockSlimMode.Read) rwLock.EnterReadLock();
            else if (mode == ReaderWriterLockSlimMode.UpgradeableRead) rwLock.EnterUpgradeableReadLock();

            _rwLock = rwLock;
            _mode = mode;
        }

        public static void ExitLock(ReaderWriterLockSlim rwLock)
        {
            if (rwLock.IsWriteLockHeld) rwLock.ExitWriteLock();
            else if (rwLock.IsReadLockHeld) rwLock.ExitReadLock();
            else if (rwLock.IsUpgradeableReadLockHeld) rwLock.ExitUpgradeableReadLock();
        }

        public void Dispose()
        {
            if (_rwLock != null && _mode == GetLockMode(_rwLock)) ExitLock(_rwLock);
        }
    }

    /// <summary>
    /// 空的可释放对象
    /// </summary>
    private class Disposable : IDisposable
    {
        public static readonly Disposable Empty = new Disposable();
        public void Dispose() { }
    }

    private ReaderWriterLockSlim _rwLock = new();
    public bool Enable = true;

    public UsingReaderWriterLockSlim()
    {
        Enable = true;
    }

    /// <summary>
    /// 进入读模式
    /// </summary>
    /// <param name="ForceExitLast">是否强制退出上一个模式</param>
    /// <returns></returns>
    public IDisposable Read(bool ForceExitLast = false)
    {
        if (Enable) return new Lock(_rwLock, ReaderWriterLockSlimMode.Read, ForceExitLast);
        else return Disposable.Empty;
    }

    /// <summary>
    /// 进入写模式
    /// </summary>
    /// <param name="ForceExitLast">是否强制退出上一个模式</param>
    /// <returns></returns>
    public IDisposable Write(bool ForceExitLast = false)
    {
        if (Enable) return new Lock(_rwLock, ReaderWriterLockSlimMode.Write, ForceExitLast);
        else return Disposable.Empty;
    }

    /// <summary>
    /// 进入升级写模式
    /// </summary>
    /// <param name="ForceExitLast">是否强制退出上一个模式</param>
    /// <returns></returns>
    public IDisposable UpgradeableRead(bool ForceExitLast = false)
    {
        if (Enable) return new Lock(_rwLock, ReaderWriterLockSlimMode.UpgradeableRead, ForceExitLast);
        else return Disposable.Empty;
    }

    /// <summary>
    /// 获取当前的锁状态
    /// </summary>
    public ReaderWriterLockSlimMode LockMode => GetLockMode(_rwLock);
};

*/