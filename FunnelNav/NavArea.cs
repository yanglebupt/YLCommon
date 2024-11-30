using System;
using System.Collections.Generic;

namespace YLCommon.Nav
{
  /// <summary>
  /// 一个多边形区域
  /// </summary>
  public class NavArea
  {
    /// <summary>
    /// 区块 id
    /// </summary>
    public int id;

    /// <summary>
    /// 该区块的顶点，通过顺时针 strip-loop 模式，连接成一个多边形
    /// </summary>
    public NavPoint[] points;

    /// <summary>
    /// 共用边界定义了邻居
    /// </summary>
    public List<NavBorder> borders;

    /// <summary>
    /// 最小边界
    /// </summary>
    public NavVector min = new NavVector(float.MaxValue);
    /// <summary>
    /// 最大边界
    /// </summary>
    public NavVector max = new NavVector(float.MinValue);

    /// <summary>
    /// 中心点
    /// </summary>
    public NavVector center = NavVector.Zero;

    public NavArea(int id, NavPoint[] points)
    {
      this.id = id;
      this.points = points;

      for (int i = 0, n = points.Length; i < n; i++)
      {
        NavPoint p = points[i];
        p.ownerAreas.Add(this);
        if (p.x < min.x) min.x = p.x;
        if (p.y < min.y) min.y = p.y;
        if (p.z < min.z) min.z = p.z;
        if (p.x > max.x) max.x = p.x;
        if (p.y > max.y) max.y = p.y;
        if (p.z > max.z) max.z = p.z;
        center += p / (float)n;
      }
    }

    /// <summary>
    /// 判断点在多边形内，多边形边上，多边形顶点
    /// </summary>
    public (bool, NavBorder, NavPoint) GetPointInAreaInfo(NavVector pos)
    {
      if (pos.x < min.x || pos.x > max.x || pos.z < min.z || pos.z > max.z) return (false, null, null);
      bool rt = false;
      for (int j = 0, m = points.Length, k = m - 1; j < m; k = j++)
      {
        NavPoint a = points[k];
        NavPoint b = points[j];

        // 点是否在线上
        NavVector pa = pos - a;
        NavVector pb = pos - b;
        bool InLine = NavVector.CrossXZ(pa, pb) == 0 && NavVector.DotXZ(pa, pb) <= 0;
        if (InLine)
        {
          // 查找边界
          NavBorder border = null;
          NavPoint point = null;

          if (NavVector.DotXZ(pa, pb) == 0)
          {
            bool IsSameA = pos.x == a.x && pos.z == a.z;
            point = IsSameA ? a : b;
          }
          else
          {
            foreach (var br in borders)
            {
              if ((br.point1.Equals(a) && br.point2.Equals(b)) || (br.point1.Equals(b) && br.point2.Equals(a)))
              {
                border = br;
                break;
              }
            }
          }
          return (true, border, point);
        }
        // PNPoly 算法
        if ((a.z < pos.z) != (b.z < pos.z) && (pos.x < (b.x - a.x) * (pos.z - a.z) / (b.z - a.z) + a.x))
          rt = !rt;
      }
      return (rt, null, null);
    }

    public override bool Equals(object obj)
    {
      return (obj is NavArea area) && area.id == id;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(id);
    }
  }
}