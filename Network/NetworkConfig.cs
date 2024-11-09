using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace YLCommon
{

    [Serializable]
    public class TCPMessage { }
    public class NetworkConfig
    {
        // 日志输出
        public class Logger
        {
            // 常规打印
            public Action<string>? info;
            // 警告打印
            public Action<string>? warn;
            // 错误打印
            public Action<string>? error;
        };

        public static Logger logger = new();

        // 序列化
        public static byte[]? Serialize<T>(T message) where T: TCPMessage
        {
            byte[] ?data = null;
            MemoryStream ms = new();
            BinaryFormatter bf = new();
            try
            {
                // 通过 BinaryFormatter 将 object 序列化到 MemoryStream 这个 Buffer 中
                bf.Serialize(ms, message);
                // 将 0 位置作为流的起始位置
                ms.Seek(0, SeekOrigin.Begin);
                data = ms.ToArray();
            }
            catch (SerializationException e)
            {
                logger.error?.Invoke($"Failed to Serialize: {e.Message}");
            }
            finally
            {
                ms.Close();
            }
            return data;
        }

        // 反序列化
        public static T? Deserialize<T>(byte[] data) where T : TCPMessage
        {
            T? message = null;
            // 将 bytes 写入到 MemoryStream 这个 Buffer 中
            MemoryStream ms = new(data);
            BinaryFormatter bf = new();
            try
            {
                // 通过 BinaryFormatter 进行反序列化
                message = (T)bf.Deserialize(ms);
            }
            catch (SerializationException e)
            {
                logger.error?.Invoke($"Failed to Deserialize: {e.Message}");
            }
            finally
            {
                ms.Close();
            }
            return message;
        }
    }
}
