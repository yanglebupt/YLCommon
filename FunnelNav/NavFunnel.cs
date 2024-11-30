using System;
using System.Collections.Generic;

namespace YLCommon.Nav
{
  public class NavFunnel : IComparable<NavFunnel>
  {
    /// <summary>
    /// 漏斗优先级，值越低，优先级越高
    /// </summary>
    public float priority;

    #region funnel define
    public NavArea posArea;
    public NavBorder posBorder;
    NavVector funnelPos = NavVector.Zero;
    NavVector targetPos = NavVector.Zero;
    NavPoint leftPoint = null;
    NavPoint rightPoint = null;
    NavPoint leftLimitPoint = null;
    NavPoint rightLimitPoint = null;
    NavVector leftLimitDir = NavVector.Zero;
    NavVector rightLimitDir = NavVector.Zero;
    NavVector leftCheckDir = NavVector.Zero;
    NavVector rightCheckDir = NavVector.Zero;
    List<NavPoint> leftConnerLst = new List<NavPoint>();
    List<NavPoint> rightConnerLst = new List<NavPoint>();
    enum FunnelShirkEnum
    {
      None,
      LeftToLeft,
      LeftToCenter,
      LeftToRight,
      RightToRight,
      RightToCenter,
      RightToLeft
    }
    FunnelShirkEnum leftFSE, rightFSE;
    List<NavVector> posLst = new();
    public List<NavVector> tempPosLst = new();
    float sumDis = 0;
    public List<NavArea> areaLst = new();
    List<NavBorder> borders = new();
    #endregion


    private NavFunnel() { }
    public NavFunnel(NavArea startArea, NavBorder border, NavVector startPos, NavVector endPos)
    {
      posArea = startArea;
      posBorder = border;
      funnelPos = startPos;
      targetPos = endPos;

      var v1 = posBorder.point1 - startPos;
      var v2 = posBorder.point2 - startPos;
      float cross = NavVector.CrossXZ(v1, v2);
      if (cross < 0)
      {
        leftPoint = posBorder.point1;
        rightPoint = posBorder.point2;
      }
      else if (cross > 0)
      {
        leftPoint = posBorder.point2;
        rightPoint = posBorder.point1;
      }
      else
      {
        NavMap.logger.error?.Invoke("invaild NavFunnel border contains startPos");
        return;
      }

      leftLimitPoint = leftPoint;
      rightLimitPoint = rightPoint;
      leftLimitDir = leftLimitPoint - funnelPos;
      rightLimitDir = rightLimitPoint - funnelPos;

      posLst = new List<NavVector> { startPos };
      areaLst = new List<NavArea> { startArea };
      borders = new List<NavBorder> { border };
    }

    // 进行扩张，进行新边界
    public bool Growth(NavArea expandArea, NavBorder expandBorder)
    {
      // 边界重复，无效扩张
      for (int i = 0; i < borders.Count; i++)
      {
        if (borders[i].Equals(expandBorder))
        {
          return false;
        }
      }

      areaLst.Add(expandArea);
      borders.Add(expandBorder);

      int offset = 0;
      int count = expandArea.points.Length;
      for (int i = 0; i < count; i++)
      {
        if (leftPoint.Equals(expandArea.points[i]))
        {
          offset = i;
          break;
        }
      }
      NavPoint leftAdd = null;
      NavPoint rightAdd = null;
      for (int i = 0; i < count; i++)
      {
        NavPoint curPoint = expandArea.points[(i + offset) % count];
        if (curPoint.Equals(expandBorder.point1))
        {
          leftAdd = expandBorder.point1;
          rightAdd = expandBorder.point2;
          break;
        }
        else if (curPoint.Equals(expandBorder.point2))
        {
          leftAdd = expandBorder.point2;
          rightAdd = expandBorder.point1;
          break;
        }
      }

      if (leftAdd is not null && rightAdd is not null)
      {
        GrowthByPoint(leftAdd, rightAdd);
        // 扩张后需要重新计算优先级
        ReckonPriority();
      }

      return true;
    }

    void GrowthByPoint(NavPoint leftAdd, NavPoint rightAdd)
    {
      tempPosLst.Clear();

      if (leftPoint.Equals(leftAdd) && rightPoint.Equals(rightAdd))
      {
        NavMap.logger.error?.Invoke("漏斗外沿顶点未变化，检测调用数据。（左右点必须至少有一个变化）");
        return;
      }
      else
      {
        leftPoint = leftAdd;
        rightPoint = rightAdd;
      }

      #region Calc CheckDir and Update LimitDir
      leftCheckDir = leftPoint - funnelPos;
      rightCheckDir = rightPoint - funnelPos;
      if (leftLimitDir == NavVector.Zero) leftLimitDir = leftCheckDir;
      if (rightLimitDir == NavVector.Zero) rightLimitDir = rightCheckDir;
      #endregion

      #region Calc Funnel Shirk or Expand
      leftFSE = CalcLeftFunnelChange();
      rightFSE = CalcRightFunnelChange();
      if (leftFSE == FunnelShirkEnum.LeftToLeft) leftConnerLst.Add(leftAdd);
      if (rightFSE == FunnelShirkEnum.RightToRight) rightConnerLst.Add(rightAdd);
      #endregion

      #region LeftFSE
      switch (leftFSE)
      {
        case FunnelShirkEnum.None:
          leftLimitPoint = leftPoint;
          break;
        case FunnelShirkEnum.LeftToCenter:
          leftLimitPoint = leftPoint;
          leftLimitDir = leftCheckDir;
          leftConnerLst.Clear();
          break;
        case FunnelShirkEnum.LeftToRight:
          CalcLeftToRightLimitIndex();
          break;
        default:
          break;
      }
      #endregion

      #region RightFSE
      switch (rightFSE)
      {
        case FunnelShirkEnum.None:
          rightLimitPoint = rightPoint;
          break;
        case FunnelShirkEnum.RightToCenter:
          rightLimitPoint = rightPoint;
          rightLimitDir = rightCheckDir;
          rightConnerLst.Clear();
          break;
        case FunnelShirkEnum.RightToLeft:
          CalcRightToLeftLimitIndex();
          break;
        default:
          break;
      }
      #endregion

      NavVector lastPos = posLst[posLst.Count - 1];
      for (int i = 0; i < tempPosLst.Count; i++)
      {
        if (i == 0)
        {
          sumDis += NavVector.DistanceXZ(lastPos, tempPosLst[i]);
        }
        else
        {
          sumDis += NavVector.DistanceXZ(tempPosLst[i - 1], tempPosLst[i]);
        }
      }

      if (tempPosLst.Count > 0)
      {
        posLst.AddRange(tempPosLst);
      }
    }
    FunnelShirkEnum CalcLeftFunnelChange()
    {
      FunnelShirkEnum leftFSE;
      float ll = NavVector.CrossXZ(leftLimitDir, leftCheckDir);
      if (ll > 0)
      {
        leftFSE = FunnelShirkEnum.LeftToLeft;
      }
      else if (ll == 0)
      {
        leftFSE = FunnelShirkEnum.None;
      }
      else
      {
        float lr = NavVector.CrossXZ(rightLimitDir, leftCheckDir);
        if (lr > 0 || rightLimitDir == NavVector.Zero)
        {
          leftFSE = FunnelShirkEnum.LeftToCenter;
        }
        else
        {
          leftFSE = FunnelShirkEnum.LeftToRight;
        }
      }
      return leftFSE;
    }
    FunnelShirkEnum CalcRightFunnelChange()
    {
      FunnelShirkEnum rightFSE;
      float rr = NavVector.CrossXZ(rightLimitDir, rightCheckDir);
      if (rr < 0)
      {
        rightFSE = FunnelShirkEnum.RightToRight;
      }
      else if (rr == 0)
      {
        rightFSE = FunnelShirkEnum.None;
      }
      else
      {
        float rl = NavVector.CrossXZ(leftLimitDir, rightCheckDir);
        if (rl < 0 || leftLimitDir == NavVector.Zero)
        {
          rightFSE = FunnelShirkEnum.RightToCenter;
        }
        else
        {
          rightFSE = FunnelShirkEnum.RightToLeft;
        }
      }
      return rightFSE;
    }
    void CalcLeftToRightLimitIndex()
    {
      funnelPos = rightLimitPoint;
      tempPosLst.Add(funnelPos);

      bool updateLimit = false;
      int connerIndex = 0;
      NavVector rldn = NavVector.NormalXZ(rightLimitDir);
      while (connerIndex < rightConnerLst.Count)
      {
        float rad = float.MaxValue;
        for (int i = connerIndex; i < rightConnerLst.Count; i++)
        {
          NavVector ckdn = NavVector.NormalXZ(rightConnerLst[i] - funnelPos);
          float curRad = MathF.Abs(NavVector.AngleXZ(rldn, ckdn));
          if (curRad <= rad)
          {
            connerIndex = i;
            rad = curRad;
          }
        }
        updateLimit = true;
        rightLimitPoint = rightConnerLst[connerIndex];
        rightLimitDir = rightConnerLst[connerIndex] - funnelPos;
        leftLimitPoint = leftPoint;
        leftLimitDir = leftPoint - funnelPos;
        float cross = NavVector.CrossXZ(leftLimitDir, rightLimitDir);
        if (cross > 0)
        {
          funnelPos = rightConnerLst[connerIndex];
          tempPosLst.Add(funnelPos);
          ++connerIndex;
          if (connerIndex >= rightConnerLst.Count)
          {
            rightLimitDir = NavVector.Zero;
            leftLimitDir = leftPoint - funnelPos;
          }
        }
        else
        {
          for (int i = 0; i < connerIndex + 1; i++)
          {
            rightConnerLst.RemoveAt(0);
          }
          break;
        }
      }

      if (!updateLimit)
      {
        rightLimitPoint = null;
        rightLimitDir = NavVector.Zero;
        leftLimitDir = leftPoint - funnelPos;
      }
    }
    void CalcRightToLeftLimitIndex()
    {
      funnelPos = leftLimitPoint;
      tempPosLst.Add(funnelPos);

      bool updateLimit = false;
      int connerIndex = 0;
      NavVector lldn = NavVector.NormalXZ(leftLimitDir);
      while (connerIndex < leftConnerLst.Count)
      {
        float rad = float.MaxValue;
        for (int i = connerIndex; i < leftConnerLst.Count; i++)
        {
          NavVector ckdn = NavVector.NormalXZ(leftConnerLst[i] - funnelPos);
          float curRad = MathF.Abs(NavVector.AngleXZ(lldn, ckdn));
          if (curRad <= rad)
          {
            connerIndex = i;
            rad = curRad;
          }
        }
        updateLimit = true;
        leftLimitPoint = leftConnerLst[connerIndex];
        leftLimitDir = leftConnerLst[connerIndex] - funnelPos;
        rightLimitPoint = rightPoint;
        rightLimitDir = rightPoint - funnelPos;
        float cross = NavVector.CrossXZ(leftLimitDir, rightLimitDir);
        if (cross > 0)
        {
          funnelPos = leftConnerLst[connerIndex];
          tempPosLst.Add(funnelPos);
          ++connerIndex;
          if (connerIndex >= leftConnerLst.Count)
          {
            leftLimitDir = NavVector.Zero;
            rightLimitDir = rightPoint - funnelPos;
          }
        }
        else
        {
          for (int i = 0; i < connerIndex + 1; i++)
          {
            leftConnerLst.RemoveAt(0);
          }
          break;
        }
      }

      if (!updateLimit)
      {
        leftLimitPoint = null;
        leftLimitDir = NavVector.Zero;
        rightLimitDir = rightPoint - funnelPos;
      }
    }

    public void ReckonPriority()
    {
      tempPosLst.Clear();
      // 计算目标点处于漏斗左右极限的哪边
      NavVector targetVec = targetPos - funnelPos;
      bool inLeft = NavVector.CrossXZ(leftLimitDir, targetVec) > 0;
      bool inRight = NavVector.CrossXZ(rightLimitDir, targetVec) < 0;
      // 直通
      if (!inLeft && !inRight)
        tempPosLst.Add(targetPos);
      else
      {
        // 计算并缓存当前拐点
        if (inLeft && !inRight)
        {
          CalcEndConner(leftConnerLst, leftLimitPoint, leftLimitDir, targetPos);
        }
        else if (inRight && !inLeft)
        {
          CalcEndConner(rightConnerLst, rightLimitPoint, rightLimitDir, targetPos);
        }
        else
        {
          float leftDis = NavVector.DistanceXZSq(leftPoint, targetPos);
          float rightDis = NavVector.DistanceXZSq(rightPoint, targetPos);
          if (leftDis < rightDis)
          {
            CalcEndConner(leftConnerLst, leftLimitPoint, leftLimitDir, targetPos);
          }
          else
          {
            CalcEndConner(rightConnerLst, rightLimitPoint, rightLimitDir, targetPos);
          }
        }
      }

      // 计算优先级
      priority = sumDis;
      for (int i = 0, n = tempPosLst.Count; i < n; i++)
      {
        // 计算第一个新拐点和已经存在的最后一个拐点的距离
        if (i == 0)
          priority += NavVector.DistanceXZ(posLst[posLst.Count - 1], tempPosLst[i]);
        // 计算每个新拐点之间的距离
        else
          priority += NavVector.DistanceXZ(tempPosLst[i - 1], tempPosLst[i]);
      }

      // 把已经存在的拐点，插入到新拐点之前
      tempPosLst.InsertRange(0, posLst);
    }

    void CalcEndConner(List<NavPoint> connerLst, NavPoint limitPoint, NavVector limitDir, NavVector end)
    {
      NavVector tempFunnelPos = limitPoint;
      tempPosLst.Add(tempFunnelPos);
      List<NavVector> connerPosLst = new List<NavVector>();
      for (int i = 0; i < connerLst.Count; i++)
      {
        connerPosLst.Add(connerLst[i]);
      }
      bool isExist = false;
      for (int i = 0; i < connerPosLst.Count; i++)
      {
        if (connerPosLst[i] == end)
        {
          isExist = true;
          break;
        }
      }
      if (!isExist) connerPosLst.Add(end);

      NavVector ln = NavVector.NormalXZ(limitDir);
      int connerIndex = 0;
      while (connerIndex < connerPosLst.Count)
      {
        float rad = float.MaxValue;
        for (int j = connerIndex; j < connerPosLst.Count; j++)
        {
          NavVector checkVec = connerPosLst[j] - tempFunnelPos;
          NavVector ckn = NavVector.NormalXZ(checkVec);
          float curRad = MathF.Abs(NavVector.AngleXZ(ln, ckn));
          if (curRad < rad)
          {
            rad = curRad;
            connerIndex = j;
          }
        }
        tempFunnelPos = connerPosLst[connerIndex];
        tempPosLst.Add(tempFunnelPos);
        connerIndex++;
      }
    }

    public void InsertStartBorder(NavBorder border)
    {
      borders.Insert(0, border);
    }

    public NavFunnel Clone(NavArea area, NavBorder border)
    {
      NavFunnel cloneFunnel = new NavFunnel
      {
        posArea = area,
        posBorder = border,
        funnelPos = funnelPos,
        targetPos = targetPos,
        leftPoint = leftPoint,
        rightPoint = rightPoint,
        leftLimitPoint = leftLimitPoint,
        rightLimitPoint = rightLimitPoint,
        leftLimitDir = leftLimitDir,
        rightLimitDir = rightLimitDir,
        sumDis = sumDis
      };

      cloneFunnel.leftConnerLst.AddRange(leftConnerLst);
      cloneFunnel.rightConnerLst.AddRange(rightConnerLst);
      cloneFunnel.areaLst.AddRange(areaLst);
      cloneFunnel.borders.AddRange(borders);
      cloneFunnel.posLst.AddRange(posLst);

      return cloneFunnel;
    }

    public NavArea GetExpandArea()
    {
      if (posBorder.area1.Equals(posArea)) return posBorder.area2;
      else return posBorder.area1;
    }

    public int CompareTo(NavFunnel other)
    {
      if (priority < other.priority) return -1;
      else if (priority > other.priority) return 1;
      else return 0;
    }
  }
}

