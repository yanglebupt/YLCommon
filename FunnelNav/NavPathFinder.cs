using System;
using System.Collections.Generic;
using System.Linq;

namespace YLCommon.Nav
{
  public class NavPathFinder
  {
    PriorityQueue<NavFunnel> waitDetected = new(5);
    public Action<NavArea> OnExpandArea;
    public NavPathFinder() { }
    public NavPathFinder(Action<NavArea> OnExpandArea)
    {
      this.OnExpandArea += OnExpandArea;
    }
    public List<NavVector> Search(NavMap navMap, NavVector startPos, NavVector endPos)
    {
      waitDetected.Clear();

      (NavArea startArea, NavBorder startBorder, NavPoint startPoint) = navMap.GetPointInAreaInfo(startPos);
      (NavArea endArea, NavBorder _, NavPoint _) = navMap.GetPointInAreaInfo(endPos);

      // endPos 可以不在区域中，但是 startPos 必须在区域中
      if (startArea == null || endArea == null)
      {
        NavMap.logger.warn?.Invoke("startPos and endPos must be in NavMesh");
        return new();
      }

      // 同一个区块
      if (startArea.Equals(endArea))
      {
        OnExpandArea?.Invoke(startArea);
        return new List<NavVector>() { startPos, endPos };
      }
      else
        return CalcAStarFunnel(startPos, endPos, startArea, endArea, startBorder, startPoint);
    }

    public List<NavVector> CalcAStarFunnel(NavVector startPos, NavVector endPos, NavArea startArea, NavArea endArea, NavBorder startBorder = null, NavPoint startPoint = null)
    {
      // 压入初始漏斗
      if (startBorder == null && startPoint is null)
      {
        // 在区域内部
        foreach (var border in startArea.borders)
          waitDetected.Enqueue(CreateNavFunnel(startArea, startPos, endPos, border));
      }
      else if (startBorder != null)
      {
        // 区块相连
        if (endArea != null && (startBorder.area1.Equals(endArea) || startBorder.area2.Equals(endArea)))
        {
          OnExpandArea?.Invoke(endArea);
          return new List<NavVector>() { startPos, endPos };
        }
        // 边界
        List<NavBorder> startBorders = new();
        startBorders.AddRange(startBorder.area1.borders);
        startBorders.Union(startBorder.area2.borders);
        foreach (var border in startBorders)
        {
          // 不共线
          if (!startBorder.IsLine(border))
          {
            // 判断当前边界属于哪个区域
            bool IsArea1 = startBorder.area1.Equals(border.area1) || startBorder.area1.Equals(border.area2);
            NavFunnel navFunnel = CreateNavFunnel(IsArea1 ? startBorder.area1 : startBorder.area2, startPos, endPos, border);
            // 插入起始边界，防止根据 border 扩展漏斗回到起始边界
            navFunnel.InsertStartBorder(startBorder);
            waitDetected.Enqueue(navFunnel);
          }
        }
      }
      else
      {
        // 点
        List<NavBorder> startBorders = new();
        foreach (var area in startPoint.ownerAreas)
        {
          // 区块相连
          if (endArea != null && area.Equals(endArea))
          {
            OnExpandArea?.Invoke(endArea);
            return new List<NavVector>() { startPos, endPos };
          }

          if (startBorders.Count == 0)
            startBorders.AddRange(area.borders);
          else
            startBorders.Union(area.borders);
        }

        foreach (var border in startBorders)
        {
          // 点不在边界上
          if (!NavVector.IsLineXZ(startPoint, border.point1, border.point2))
          {
            // 判断当前边界属于哪个区域
            foreach (var area in startPoint.ownerAreas)
            {
              if (area.Equals(border.area1) || area.Equals(border.area2))
              {
                waitDetected.Enqueue(CreateNavFunnel(area, startPos, endPos, border));
                break;
              }
            }
          }
        }
      }

      List<NavVector> foundPos = new List<NavVector>();

      OnExpandArea?.Invoke(startArea);
      while (waitDetected.Count > 0)
      {
        NavFunnel navFunnel = waitDetected.Dequeue();
        // 根据漏斗边界--》找到下一个区域的有效边界作为漏斗的新边界，进行扩展
        NavArea expandArea = navFunnel.GetExpandArea();
        OnExpandArea?.Invoke(expandArea);
        // 找到目标区域，结束
        if (expandArea.Equals(endArea))
        {
          foundPos.AddRange(navFunnel.tempPosLst);
          break;
        }
        else
        {
          // 找到下一个区域的有效边界作为漏斗的新边界，进行扩展
          List<NavBorder> expandBorders = expandArea.borders;
          NavBorder curBorder = navFunnel.posBorder;
          for (int i = 0, n = expandBorders.Count; i < n; i++)
          {
            NavBorder expandBorder = expandBorders[i];
            if (!expandBorder.Equals(curBorder))
            {
              NavFunnel expandFunnel;
              if (n == 2)
              {
                // 只有一个扩展，直接复用之前的
                expandFunnel = navFunnel;
                expandFunnel.posBorder = expandBorder;
                expandFunnel.posArea = expandArea;
              }
              else
              {
                // 否则进行拷贝
                expandFunnel = navFunnel.Clone(expandArea, expandBorder);
              }

              // 进行边界的扩展
              if (expandFunnel.Growth(expandArea, expandBorder))
                waitDetected.Enqueue(expandFunnel);
            }
          }
        }
      }

      return foundPos;
    }

    public NavFunnel CreateNavFunnel(NavArea startArea, NavVector startPos, NavVector endPos, NavBorder border)
    {
      NavFunnel navFunnel = new NavFunnel(startArea, border, startPos, endPos);
      navFunnel.ReckonPriority();
      return navFunnel;
    }
  }
}
