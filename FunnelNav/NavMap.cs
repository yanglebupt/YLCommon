using System;
using System.Collections.Generic;

namespace YLCommon.Nav
{
  /// <summary>
  /// 整个导航地图
  /// </summary>
  public class NavMap
  {
    public class Logger
    {
      // 常规打印
      public Action<string> info;
      // 警告打印
      public Action<string> warn;
      // 错误打印
      public Action<string> error;
    };

    public static Logger logger = new();

    /// <summary>
    /// 整个地图的全部多边形的顶点集合
    /// </summary>
    public NavPoint[] vertices;

    /// <summary>
    /// 整个地图的全部多边形区域
    /// </summary>
    public NavArea[] areas;

    public Action<NavArea> OnCreateArea;

    public NavMap(NavConfig navConfig, Action<NavArea> OnCreateArea = null)
    {
      if (OnCreateArea != null)
        this.OnCreateArea += OnCreateArea;

      // 初始化顶点
      int allPointCount = navConfig.vertices.Count;
      vertices = new NavPoint[allPointCount];
      for (int i = 0; i < allPointCount; i++)
        vertices[i] = new NavPoint(i, navConfig.vertices[i]);

      // 初始化区域，边界
      int areaCount = navConfig.indices.Count;
      areas = new NavArea[areaCount];
      Dictionary<string, NavBorder> border_dic = new();
      for (int i = 0; i < areaCount; i++)
      {
        int[] areaIndices = navConfig.indices[i];
        int pointCount = areaIndices.Length;
        int areaID = i;
        NavPoint[] points = new NavPoint[pointCount];
        for (int j = 0; j < pointCount; j++)
          points[j] = vertices[areaIndices[j]];
        NavArea area = new NavArea(areaID, points);

        // 记录每条边的区块的引用
        for (int j = 0, m = pointCount, k = m - 1; j < m; k = j++)
        {
          int i1 = areaIndices[j], i2 = areaIndices[k];
          NavPoint p1 = vertices[i1], p2 = vertices[i2];
          string key = ComposeOrderKey(i1, i2);
          NavBorder border;
          if (border_dic.TryGetValue(key, out border))
          {
            border.area2 = area;
          }
          else
          {
            border = new NavBorder() { point1 = p1, point2 = p2, area1 = area };
            border_dic.Add(key, border);
          }
        }

        areas[i] = area;
        OnCreateArea?.Invoke(area);
      }

      // 再遍历每一个区块，筛选共享边界，也就是邻居
      for (int i = 0; i < areaCount; i++)
      {
        NavArea area = areas[i];
        List<NavBorder> borders = new();
        foreach (var item in border_dic)
        {
          NavBorder border = item.Value;
          if (border.IsShared && border.OwnedBy(area))
            borders.Add(border);
        }
        area.borders = borders;
      }
    }

    /// <summary>
    /// 以大小顺序组合两个数字成字符串
    /// </summary>
    public string ComposeOrderKey(int index1, int index2)
    {
      if (index1 < index2) return $"{index1}_{index2}";
      else return $"{index2}_{index1}";
    }

    /// <summary>
    /// 判断点在多边形内，多边形边上，多边形顶点
    /// </summary>
    public (NavArea, NavBorder, NavPoint) GetPointInAreaInfo(NavVector pos)
    {
      foreach (NavArea area in areas)
      {
        (bool rt, NavBorder border, NavPoint point) = area.GetPointInAreaInfo(pos);
        if (rt)
          return (area, border, point);
      }
      return (null, null, null);
    }
  }
}