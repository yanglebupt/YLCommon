using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using YLCommon;

// 扩展方法，相当于往原型链上添加实例方法
public static class ExtensionMethods
{
    public static void ColorLog(this object _, LogColor logColor, string message, params object[] args)
    {
        Logger.ColorLog(logColor, message, args);
    }
    public static void Debug(this object _, string message, params object[] args)
    {
        Logger.Debug(message, args);
    }
    public static void Info(this object _, string message, params object[] args)
    {
        Logger.Info(message, args);
    }
    public static void Warn(this object _, string message, params object[] args)
    {
        Logger.Warn(message, args);
    }
    public static void Error(this object _, string message, params object[] args)
    {
        Logger.Error(message, args);
    }

    public static void ColorLog(this object _, LogColor logColor, object message)
    {
        Logger.ColorLog(logColor, message);
    }
    public static void Debug(this object _, object message)
    {
        Logger.Debug(message);
    }
    public static void Info(this object _, object message)
    {
        Logger.Info(message);
    }
    public static void Warn(this object _, object message)
    {
        Logger.Warn(message);
    }
    public static void Error(this object _, object message)
    {
        Logger.Error(message);
    }
}


namespace YLCommon
{
    public class Logger
    {
        class UnityLogger : ILogger
        {
            // 利用反射获取 Unity 里面的打印
            readonly Type type = Type.GetType("UnityEngine.Debug, UnityEngine");
            public void Error(string message)
            {
                type.GetMethod("LogError", new Type[] { typeof(object) }).Invoke(null, new object[] { ColorUnityLog(message, LogColor.Red) });
            }

            public void Log(string message, LogColor logColor = LogColor.None)
            {
                // 染色
                type.GetMethod("Log", new Type[] { typeof(object) }).Invoke(null, new object[] { ColorUnityLog(message, logColor) });
            }

            public void Warn(string message)
            {
                type.GetMethod("LogWarning", new Type[] { typeof(object) }).Invoke(null, new object[] { ColorUnityLog(message, LogColor.Yellow) });
            }

            private string ColorUnityLog(string message, LogColor logColor) {
                string rich_message = "<color={0}>" + message + "</color>";
                switch (logColor)
                {
                    case LogColor.Red:
                        rich_message = string.Format(rich_message, "#FF0000");
                        break;
                    case LogColor.Green:
                        rich_message = string.Format(rich_message, "#00FF00");
                        break;
                    case LogColor.Blue:
                        rich_message = string.Format(rich_message, "#0000FF");
                        break;
                    case LogColor.Cyan:
                        rich_message = string.Format(rich_message, "#00FFFF");
                        break;
                    case LogColor.Magenta:
                        rich_message = string.Format(rich_message, "#FF00FF");
                        break;
                    case LogColor.Yellow:
                        rich_message = string.Format(rich_message, "#FFFF00");
                        break;
                    case LogColor.None:
                    default:
                        rich_message = message;
                        break;
                }
                return rich_message;
            }
        }

        class ConsoleLogger : ILogger
        {
            public void Error(string message)
            {
                WriteConsoleLog(message, LogColor.Red);
            }

            public void Log(string message, LogColor logColor = LogColor.None)
            {
                WriteConsoleLog(message, logColor);
            }

            public void Warn(string message)
            {
                WriteConsoleLog(message, LogColor.Yellow);
            }

            private void WriteConsoleLog(string message, LogColor logColor) {
                switch (logColor)
                {
                    case LogColor.Red:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogColor.Green:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case LogColor.Blue:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogColor.Cyan:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case LogColor.Magenta:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                    case LogColor.Yellow:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogColor.None:
                    default:
                        break;
                }
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static ILogger loggerInstance;
        private static StreamWriter? loggerFileWriter = null;
        private static string loggerSaveFile = "";
        private static string[] logLevelName = { "Debug", "Info", "Warn", "Error" };


        public static LoggerConfig cfg = new LoggerConfig();
        public static string LoggerSaveFile => loggerSaveFile;

        public static string EnableSetting()
        {
            loggerInstance = cfg.loggerType == LoggerType.Console ? new ConsoleLogger() : new UnityLogger() as ILogger;
            return CreateLogFile();
        }

        private static string CreateLogFile()
        {
            if (!cfg.enableSave) {
                loggerFileWriter = null;
                loggerSaveFile = "";
            };
            // 只写入一个文件
            if (cfg.saveOverride)
            {
                loggerSaveFile = Path.Combine(cfg.saveDir, $"{cfg.saveFilename}.txt");
                try
                {
                    // 覆盖写，则先删除原文件，然后在新建文件写入
                    if (Directory.Exists(cfg.saveDir) && File.Exists(loggerSaveFile))
                        File.Delete(loggerSaveFile);
                    else if (!Directory.Exists(cfg.saveDir)) Directory.CreateDirectory(cfg.saveDir);
                    loggerFileWriter = File.AppendText(loggerSaveFile);
                    loggerFileWriter.AutoFlush = true;
                }
                catch
                {
                    loggerFileWriter = null;
                }
            }
            else
            {
                // 文件名不同，以调用 setting 方法的时间决定，当然你可以
                loggerSaveFile = Path.Combine(cfg.saveDir, $"{cfg.saveFilename}@{DateTime.Now.ToString("yyyy-MM-dd@HH-mm-ss-fff")}.txt");
                try
                {
                    if (!Directory.Exists(cfg.saveDir))
                        Directory.CreateDirectory(cfg.saveDir);
                    loggerFileWriter = File.AppendText(loggerSaveFile);
                    loggerFileWriter.AutoFlush = true;
                }
                catch
                {
                    loggerFileWriter = null;
                }
                return loggerSaveFile;
            }
            return loggerSaveFile;
        }

        private static void ColorLog(LogColor logColor, LogLevel logLevel, string message, params object[] args)
        {
            if (!cfg.enable || cfg.logLevel < logLevel) return;
            string d_m = DecorateLog($"[{logLevelName[(int)logLevel]}]", string.Format(message, args));
            if (logLevel == LogLevel.Warn) loggerInstance.Warn(d_m);
            else if (logLevel == LogLevel.Error) loggerInstance.Error(d_m);
            else loggerInstance.Log(d_m, logColor);
            WriteToFile(d_m);
        }

        public static void ColorLog(LogColor logColor, string message, params object[] args)
        {
            ColorLog(logColor, LogLevel.Info, message, args);
        }

        public static void ColorLog(LogColor logColor, object message)
        {
            ColorLog(logColor, LogLevel.Info, message.ToString());
        }

        public static void Debug(string message, params object[] args)
        {
            ColorLog(LogColor.Blue, LogLevel.Debug, message, args);
        }

        public static void Debug(object message)
        {
            ColorLog(LogColor.Blue, LogLevel.Debug, message.ToString());
        }

        public static void Info(string message, params object[] args)
        {
            ColorLog(LogColor.Green, LogLevel.Info, message, args);
        }

        public static void Info(object message)
        {
            ColorLog(LogColor.Green, LogLevel.Info, message.ToString());
        }

        public static void Warn(string message, params object[] args)
        {
            ColorLog(LogColor.Yellow, LogLevel.Warn, message, args);
        }
        public static void Warn(object message)
        {
            ColorLog(LogColor.Yellow, LogLevel.Warn, message.ToString());
        }

        public static void Error(string message, params object[] args)
        {
            ColorLog(LogColor.Red, LogLevel.Error, message, args);
        }
        public static void Error(object message)
        {
            ColorLog(LogColor.Red, LogLevel.Error, message.ToString());
        }

        private static void WriteToFile(string message)
        {
            if (cfg.enableSave && loggerFileWriter != null)
            {
                // TODO: 这里需要添加安全队列，加锁来写入文件
                // 检测当前 log 文件大小是否超过最大
                FileInfo fileInfo = new FileInfo(loggerSaveFile);
                if (!fileInfo.Exists) return;
                if (fileInfo.Length >= cfg.savefileMaxSize)
                    CreateLogFile();
                try
                {
                    loggerFileWriter.WriteLine(message);
                }
                catch {
                    loggerFileWriter = null;
                }
            }
        }

        // 为消息添加附带信息
        private static string DecorateLog(string logPrefix, string message) {
            StringBuilder sb = new StringBuilder(logPrefix, 100);
            if (cfg.showTime) sb.AppendFormat(" {0} ", DateTime.Now.ToString("HH::mm::ss--fff"));
            if (cfg.showThreadID) sb.AppendFormat("ThreadID: {0} ", Thread.CurrentThread.ManagedThreadId);
            sb.AppendFormat("{0} {1} ", cfg.logSeparate, message);
            if (cfg.showTrace) sb.AppendFormat("\nStackTrace: {0}", GetLogTrace());
            return sb.ToString();
        }

        // 获取调用堆栈信息
        private static string GetLogTrace()
        {
            StackTrace st = new StackTrace(3, true);
            StringBuilder traceInfo = new StringBuilder(100); ;
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                var method = sf.GetMethod();
                traceInfo.AppendFormat("\n    {0}::{1}::{2} line:{3}", sf.GetFileName(), method.ReflectedType.Name, method, sf.GetFileLineNumber());
            }
            return traceInfo.ToString();
        }
    };
}
