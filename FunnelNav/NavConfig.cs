using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace YLCommon.Nav
{
  public class NavConfig
  {
    /// <summary>
    /// 整个地图的全部多边形区域顶点索引集合，里面每个数组代表一个多边形区域的全部顶点索引
    /// </summary>
    public List<int[]> indices = new();

    /// <summary>
    /// 整个地图的全部多边形区域顶点集合
    /// </summary>
    public List<NavVector> vertices = new();

    /// <summary>
    /// 从 json 文件中加载地图配置
    /// </summary>
    /// <param name="filename">文件路径</param>
    /// <returns></returns>
    public static NavConfig LoadFromFile(string filename)
    {
      return JsonConvert.DeserializeObject<NavConfig>(File.ReadAllText(filename));
    }
  }
}
