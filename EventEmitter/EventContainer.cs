using System;
using System.Collections.Generic;

namespace YLCommon
{
  using Callback = Action<object>;

  /// <summary>
  /// 事件的容器类，非线程安全
  /// </summary>
  public class EventContainer<T>
  {
    // 事件名到回调函数的映射
    private readonly Dictionary<T, List<Callback>> event2func = new();
    // 可有可无：注册对象到事件名的映射，一般再注册对象销毁后，其注册的事件也要取消
    private readonly Dictionary<object, List<T>> target2event = new();

    public void Clear()
    {
      event2func.Clear();
      target2event.Clear();
    }

    /// <summary>
    /// 判断是否存在某个事件
    /// </summary>
    /// <param name="evt">事件名</param>
    /// <returns>是否存在</returns>
    public bool Has(T evt)
    {
      return event2func.ContainsKey(evt);
    }

    /// <summary>
    /// 获取某个对象的全部事件
    /// </summary>
    /// <param name="target">对象</param>
    /// <returns>事件集合</returns>
    public List<T> GetEvents(object target)
    {
      target2event.TryGetValue(target, out List<T> evts);
      return evts;
    }

    /// <summary>
    /// 获取事件的全部回调
    /// </summary>
    /// <param name="target">事件</param>
    /// <returns>回调函数集合</returns>
    public List<Callback> GetActions(T evt)
    {
      event2func.TryGetValue(evt, out List<Callback> cbs);
      return cbs;
    }

    /// <summary>
    /// 添加一个事件回调
    /// </summary>
    /// <param name="evt">事件名称</param>
    /// <param name="cb">回调函数，接受一个 object 的参数，无返回值</param>
    public void Add(T evt, Callback cb)
    {
      if (cb == null) return;
      // 1. 先出来 event2func
      // 是否存在 event，不存在则创建对应容器
      if (!event2func.ContainsKey(evt))
        event2func[evt] = new List<Callback>();
      List<Callback> cbs = event2func[evt];

      // 判断回调是否已经存在，防止重复注册，同一个函数多次回调
      Callback t_cb = cbs.Find((Callback c) => c.Equals(cb));
      if (t_cb != null) return;

      cbs.Add(cb);

      // 2. 根据 cb.Target 确定注册对象
      object target = cb.Target;
      if (target == null) return;

      if (!target2event.ContainsKey(target))
        target2event[target] = new List<T>();
      List<T> evts = target2event[target];

      // 事件可以重复，但没有必要，这里可以警告一下
      // 如果一个 target 的两个不同的 function 注册了同一个事件，重复注册了
      evts.Add(evt);
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="evt">事件名称</param>
    public void Remove(T evt)
    {
      if (!event2func.ContainsKey(evt)) return;

      // 需要删除每个回调函数，以及回调函数注册对象的事件名
      List<Callback> cbs = event2func[evt];
      foreach (Callback cb in cbs)
      {
        object target = cb.Target;
        if (target == null || !target2event.ContainsKey(target)) continue;
        // 从注册对象中移除该事件
        List<T> evts = target2event[target];
        evts.RemoveAll((T e) => evt.Equals(evt));

        if (evts.Count == 0)
          target2event.Remove(target);
      }

      event2func.Remove(evt);
    }

    /// <summary>
    /// 移除对象上的全部事件
    /// </summary>
    /// <param name="evt">待移除对象</param>
    public void Remove(object target)
    {
      if (!target2event.ContainsKey(target)) return;
      List<T> evts = target2event[target];
      foreach (T evt in evts)
      {
        if (!event2func.ContainsKey(evt)) continue;
        List<Callback> cbs = event2func[evt];
        cbs.RemoveAll((Callback c) => c.Target == target);
        if (cbs.Count == 0)
          event2func.Remove(evt);
      }
      target2event.Remove(target);
    }
  }
}


