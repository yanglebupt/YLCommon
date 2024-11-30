using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace YLCommon
{
  using Callback = Action<object?>;
  /// <summary>
  /// 事件的订阅和发布类，线程安全
  /// </summary>
  public class EventEmitter<T>
  {
    public class Emitter
    {
      public T evt = default;
      public object? payload = null;
      public Emitter(T evt, object? payload)
      {
        this.evt = evt;
        this.payload = payload;
      }
    }
    private readonly object mutex = new();
    private readonly EventContainer<T> container = new();
    private readonly ConcurrentQueue<Emitter> emitters = new();

    /// <summary>
    /// 是否存在该事件
    /// </summary>
    /// <param name="evt">事件</param>
    /// <returns>是否存在</returns>
    public bool Has(T evt)
    {
      return container.Has(evt);
    }

    /// <summary>
    /// 获取某个对象的全部事件
    /// </summary>
    /// <param name="target">对象</param>
    /// <returns>事件集合</returns>
    public List<T>? GetEvents(object target)
    {
      return container.GetEvents(target);
    }

    /// <summary>
    /// 获取事件的全部回调
    /// </summary>
    /// <param name="target">事件</param>
    /// <returns>回调函数集合</returns>
    public List<Callback>? GetActions(T evt)
    {
      return container.GetActions(evt)?.cbs;
    }

    public bool IsOnce(T evt)
    {
      return container.GetActions(evt)?.once ?? false;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
      emitters.Clear();
    }


    /// <summary>
    /// 取消所有的事件，待触发事件不受影响
    /// </summary>
    public void UnInit()
    {
      // 执行剩余的待触发事件
      Tick();
      emitters.Clear();
      container.Clear();
    }

    /// <summary>
    /// 每帧处理前面所有的待触发事件
    /// </summary>
    public void Tick()
    {
      while (emitters.TryDequeue(out Emitter emitter))
        EmitImmediate(emitter.evt, emitter.payload);
    }

    /// <summary>
    /// 触发一个事件，并立即依次执行事件的回调函数
    /// </summary>
    /// <param name="evt">事件名字</param>
    /// <param name="payload">事件携带的参数，默认为 null</param>
    public void EmitImmediate(T evt, object? payload = null)
    {
      lock (mutex)
      {
        if (!(container.GetActions(evt) is EventCallback ec)) return;
        foreach (Callback cb in ec.cbs)
          cb(payload);
        if (ec.once)
          container.Remove(evt);
      }
    }

    /// <summary>
    /// 触发一个事件，等到下一个 Tick 才开始执行事件
    /// </summary>
    /// <param name="evt">事件名字</param>
    /// <param name="payload">事件携带的参数，默认为 null</param>
    public void Emit(T evt, object? payload = null)
    {
      lock (mutex)
      {
        if (!container.Has(evt)) return;
      }
      emitters.Enqueue(new Emitter(evt, payload));
    }

    /// <summary>
    /// 注册一个事件回调
    /// </summary>
    /// <param name="evt">事件名称</param>
    /// <param name="cb">回调函数，接受一个 object 的参数，无返回值</param>
    public void On(T evt, Callback cb)
    {
      lock (mutex)
      {
        container.Add(evt, cb, false);
      }
    }

    public void Once(T evt, Callback cb)
    {
      lock (mutex)
      {
        container.Add(evt, cb, true);
      }
    }

    /// <summary>
    /// 取消事件
    /// </summary>
    /// <param name="evt">事件名称</param>
    public void Off(T evt)
    {
      lock (mutex)
      {
        container.Remove(evt);
      }
    }

    /// <summary>
    /// 取消注册对象上的全部事件
    /// </summary>
    /// <param name="evt">注册对象</param>
    public void Off(object target)
    {
      lock (mutex)
      {
        container.Remove(target);
      }
    }
  }
}