using System;
using System.Collections.Generic;

namespace YLCommon.Nav
{
  public class NavPoint : NavVector
  {
    public int id;
    // 记录该点被哪些区块引用
    public List<NavArea> ownerAreas = new();

    public NavPoint(int id, float x, float y, float z) : base(x, y, z)
    {
      this.id = id;
    }
    public NavPoint(int id, NavVector pos) : base(pos.x, pos.y, pos.z)
    {
      this.id = id;
    }

    public override bool Equals(object obj)
    {
      return (obj is NavPoint point) && point.id == id;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(id);
    }
  }
}

