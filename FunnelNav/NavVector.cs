using System;

namespace YLCommon.Nav
{
  /// <summary>
  /// 通用的 Vector3，不依赖于 Unity 引擎
  /// </summary>
  public class NavVector
  {
    public float x;
    public float y;
    public float z;

    public NavVector() { }

    public NavVector(float v)
    {
      this.x = v;
      this.y = v;
      this.z = v;
    }

    public NavVector(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public NavVector(float x, float z)
    {
      this.x = x;
      this.y = 0;
      this.z = z;
    }

    public static NavVector Zero => new NavVector(0);
    public static NavVector One => new NavVector(1);

    #region 四则运算

    public static NavVector operator +(NavVector v1, NavVector v2)
    {
      return new NavVector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
    }
    public static NavVector operator +(NavVector v, float m)
    {
      return new NavVector(v.x + m, v.y + m, v.z + m);
    }
    public static NavVector operator +(float m, NavVector v)
    {
      return new NavVector(m + v.x, m + v.y, m + v.z);
    }
    public static NavVector operator -(NavVector v1, NavVector v2)
    {
      return new NavVector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
    }
    public static NavVector operator -(NavVector v, float m)
    {
      return new NavVector(v.x - m, v.y - m, v.z - m);
    }
    public static NavVector operator -(float m, NavVector v)
    {
      return new NavVector(m - v.x, m - v.y, m - v.z);
    }
    public static NavVector operator -(NavVector v)
    {
      return new NavVector(-v.x, -v.y, -v.z);
    }
    public static NavVector operator *(NavVector v, float m)
    {
      return new NavVector(v.x * m, v.y * m, v.z * m);
    }
    public static NavVector operator *(float m, NavVector v)
    {
      return new NavVector(m * v.x, m * v.y, m * v.z);
    }
    public static NavVector operator *(NavVector v1, NavVector v2)
    {
      return new NavVector(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
    public static NavVector operator /(NavVector v, float m)
    {
      return new NavVector(v.x / m, v.y / m, v.z / m);
    }
    public static NavVector operator /(NavVector v1, NavVector v2)
    {
      return new NavVector(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
    }
    public static NavVector operator /(float m, NavVector v)
    {
      return new NavVector(m / v.x, m / v.y, m / v.z);
    }
    #endregion 四则运算

    #region 布尔运算
    public static bool operator ==(NavVector v1, NavVector v2)
    {
      return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
    }
    public static bool operator !=(NavVector v1, NavVector v2)
    {
      return v1.x != v2.x || v1.y != v2.y || v1.z != v2.z;
    }
    #endregion 布尔运算

    public static float DistanceSq(NavVector v1, NavVector v2)
    {
      float dx = v1.x - v2.x;
      float dy = v1.y - v2.y;
      float dz = v1.z - v2.z;
      return dx * dx + dy * dy + dz * dz;
    }

    public static float DistanceXZSq(NavVector v1, NavVector v2)
    {
      float dx = v1.x - v2.x;
      float dz = v1.z - v2.z;
      return dx * dx + dz * dz;
    }

    public static float Distance(NavVector v1, NavVector v2)
    {
      return MathF.Sqrt(DistanceSq(v1, v2));
    }

    public static float DistanceXZ(NavVector v1, NavVector v2)
    {
      return MathF.Sqrt(DistanceXZSq(v1, v2));
    }

    /// <summary>
    /// 二维向量在 x-z 平面叉乘
    /// 正：v2 在 v1 逆时针
    /// 负：v2 在 v1 顺时针
    /// 零：共线
    /// </summary>
    public static float CrossXZ(NavVector v1, NavVector v2)
    {
      return v1.x * v2.z - v2.x * v1.z;
    }

    /// <summary>
    /// 二维向量在 x-z 平面点积
    /// </summary>
    public static float DotXZ(NavVector v1, NavVector v2)
    {
      return v1.x * v2.x + v1.z * v2.z;
    }

    /// <summary>
    /// 判断 x-z 平面上点 p 是否在 a-b 线上
    /// </summary>
    public static bool IsInLineXZ(NavVector p, NavVector a, NavVector b)
    {
      NavVector pa = p - a;
      NavVector pb = p - b;
      return CrossXZ(pa, pb) == 0 && DotXZ(pa, pb) <= 0;
    }

    /// <summary>
    /// 判断 x-z 平面上点 p 是否在 a-b 线及其延长线上
    /// </summary>
    public static bool IsLineXZ(NavVector p, NavVector a, NavVector b)
    {
      NavVector pa = p - a;
      NavVector pb = p - b;
      return CrossXZ(pa, pb) == 0;
    }

    public static NavVector NormalXZ(NavVector v)
    {
      float len = MathF.Sqrt(v.x * v.x + v.z * v.z);
      return new NavVector(v.x / len, 0, v.z / len);
    }

    public static NavVector Normal(NavVector v)
    {
      float len = MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
      return new NavVector(v.x / len, v.y / len, v.z / len);
    }

    /// <summary>
    /// 逆时针为负，顺时针为正
    /// </summary>
    public static float AngleXZ(NavVector a, NavVector b)
    {
      float dot = DotXZ(a, b);
      float angle = MathF.Acos(dot);
      if (CrossXZ(a, b) > 0)
        angle = -angle;
      return angle;
    }

    /// <summary>
    /// 三维叉乘
    /// </summary>
    public static NavVector Cross(NavVector v1, NavVector v2)
    {
      return new NavVector()
      {
        x = v1.y * v2.z - v1.z * v2.y,
        y = v1.z * v2.x - v1.x * v2.z,
        z = v1.x * v2.y - v1.y * v2.x
      };
    }

    public static NavVector Round(NavVector a, int digits)
    {
      return new NavVector(MathF.Round(a.x, digits), MathF.Round(a.y, digits), MathF.Round(a.z, digits));
    }

    public override string ToString()
    {
      return $"NavVector({x},{z})";
    }

    public override bool Equals(object obj)
    {
      return (obj is NavVector vector) && vector == this;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(x, y, z);
    }
  }
}