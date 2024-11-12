using System;
using MessagePack;

namespace YLCommon
{
    [MessagePackObject(keyAsPropertyName: true)]
    public partial class TCPHeader { }

    [MessagePackObject(keyAsPropertyName: true)]
    public partial class TCPMessage<H> where H: TCPHeader {
        public H header;
        public byte[] body;

        public void SetBody<T>(T obj)
        {
            byte[]? buffer = NetworkConfig.Serialize(obj);
            if( buffer != null)
                body = buffer;
        }

        public T? GetBody<T>()
        {
            T? obj = NetworkConfig.Deserialize<T>(body);
            return obj;
        }
    }

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
        internal static byte[]? Serialize<T>(T message)
        {
            return LZ4MessagePackSerializer.Serialize(message);
        }

        public static byte[]? SerializePack<T>(T message)
        {
            byte[]? data = Serialize(message);
            if (data == null) return null;
            // 拼上包的长度
            int size = data.Length;
            byte[] pack = new byte[4 + size];
            BitConverter.GetBytes(size).CopyTo(pack, 0);
            data.CopyTo(pack, 4);
            return pack;
        }

        // 反序列化
        internal static T? Deserialize<T>(byte[] data)
        {
            return LZ4MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
