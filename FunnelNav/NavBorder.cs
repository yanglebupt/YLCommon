using System;

namespace YLCommon.Nav
{
  /// <summary>
  /// 区块边界信息，连通两个区块
  /// </summary>
  public class NavBorder
  {
    /// <summary>
    /// 边界线连接的两个区块
    /// </summary>
    public NavArea area1 = null;
    public NavArea area2 = null;

    /// <summary>
    /// 边界线两端顶点
    /// </summary>
    public NavPoint point1;
    public NavPoint point2;

    public bool IsShared => area2 != null;

    /// <summary>
    /// 边界共线
    /// </summary>
    public bool IsLine(NavBorder border)
    {
      return NavVector.IsLineXZ(border.point1, point1, point2) && NavVector.IsLineXZ(border.point2, point1, point2);
    }

    public override bool Equals(object obj)
    {
      return (obj is NavBorder border) &&
            border.point1.Equals(point1) &&
            border.point2.Equals(point2);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(point1.id, point2.id);
    }

    public bool OwnedBy(NavArea area)
    {
      return area1.id == area.id || area2.id == area.id;
    }

    public NavArea GetNeighborArea(NavArea area)
    {
      return area.id == area1.id ? area2 : area1;
    }
  }
}