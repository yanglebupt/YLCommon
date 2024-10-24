using System;

namespace YLCommon
{
    public enum LogLevel
    {
        Log,
        Warn,
        Error
    }
    public enum LogColor
    {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Yellow,
        Magenta
    }
    public enum LoggerType
    {
        Console,
        Unity
    }

    /// <summary>
    /// 打印配置
    /// </summary>
    public class LoggerConfig
    {
        // 是否开启 log
        public bool enable = true;
        // 是否打印时间
        public bool showTime = true;
        // 是否打印线程 ID
        public bool showThreadID = true;
        // 是否打印堆栈信息
        public bool showTrace = false;
        // 是否保存到文件
        public bool enableSave = false;
        // 写文件是否覆盖，不覆盖每次都会新建一个文件保存日志
        public bool saveOverride = true;
        // logger 文件的最大字节数，超过则会新建一个文件进行保存
        public int savefileMaxSize = 10 * 1024 * 1024;

        // 默认保存路径在根目录下的 logs 文件夹
        public string saveDir = $"{AppDomain.CurrentDomain.BaseDirectory}logs\\";
        // log 文件名
        public string saveFilename = "ConsoleLog";
        // 打印的前缀
        public string logPrefix = "#";
        // 打印的分割符
        public string logSeparate = ">>";

        // 打印的类型
        public LoggerType loggerType = LoggerType.Console;

        // 打印的 level，小于等于该 level 的都将打印，大于的则不打印
        public LogLevel logLevel = LogLevel.Error;
    }

    interface ILogger
    {
        void Log(string message, LogColor logColor = LogColor.None);
        void Warn(string message);
        void Error(string message);
    }
}
