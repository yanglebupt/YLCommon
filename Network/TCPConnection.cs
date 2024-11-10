using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace YLCommon
{
    public class TCPConnection<H> where H : TCPHeader
    {
        public enum ConnectionState
        {
            None,
            Connected,
            HalfClosed,
            Disconnected,
        }

        public enum ErrorStage
        {
            Write,
            Read
        }

        private Socket? socket;
        private SocketAsyncEventArgs readSAEA;
        private SocketAsyncEventArgs writeSAEA;
        private SocketAsyncEventArgs closeSAEA;
        // 动态 byte 数组，需要不断增加
        private List<byte> incoming;
        // 发送队列
        private Queue<byte[]> outcoming;

        public ConnectionState state = ConnectionState.None;
        public ulong ID = 0;

        public Action<SocketError>? OnError;
        public Action<ulong> ? OnDisconnected;
        public Action<ulong, TCPMessage<H>> ? OnMessage;

        public TCPConnection() {
            // 可复用
            readSAEA = new SocketAsyncEventArgs();
            readSAEA.Completed += SAEA_Completed;
            readSAEA.SetBuffer(new byte[2048], 0, 2048);


            writeSAEA = new SocketAsyncEventArgs();
            writeSAEA.Completed += SAEA_Completed;

            closeSAEA = new SocketAsyncEventArgs();
            closeSAEA.Completed += SAEA_Completed;

            incoming = new();
            outcoming = new();
        }

        public void Init(Socket socket, ulong ID)
        {
            this.ID = ID;
            this.socket = socket;
            state = ConnectionState.Connected;
            // 开启接收数据
            Read();
        }

        private void SAEA_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    OnRead();
                    break; 
                case SocketAsyncOperation.Send:
                    OnWrite();
                    break;
                default:
                    NetworkConfig.logger.error?.Invoke("The operation not Receive or Send");
                    break;
            }
        }

        private void Read()
        {
            if (socket == null) return;
            if (!socket.ReceiveAsync(readSAEA))
                OnRead();
        }

        private void OnRead()
        {
            SocketError error = readSAEA.SocketError;
            int byteLength = readSAEA.BytesTransferred;
            // 这里一定要判断 byteLength > 0, 如果 byteLength<=0 很可能是客户端断开连接的信号
            // 此时 error == SocketError.Success，但是 byteLength 为 0，也就是读到了 FIN 信号的 EOF
            if (byteLength > 0 && error == SocketError.Success)
            {
                // incoming
                byte[] data = new byte[byteLength];
                Buffer.BlockCopy(readSAEA.Buffer, 0, data, 0, byteLength);
                incoming.AddRange(data);
                TryMessage();
                Read();
            }
            else
                Error(ErrorStage.Read, error);
        }

        // 分割消息
        private void TryMessage()
        {
            byte[]? body = null;
            // Header Size = 4
            if (incoming.Count > 4) {
                byte[] data = incoming.ToArray();
                int body_size = BitConverter.ToInt32(data, 0);
                // 只有当完整的包接收完了，才会删除已处理的，并返回
                if (incoming.Count >= 4 + body_size)
                {
                    body = new byte[body_size];
                    Buffer.BlockCopy(data, 4, body, 0, body_size);
                    incoming.RemoveRange(0, 4 + body_size);
                }
            }
            if (body == null) return;
            // 反序列化
            TCPMessage<H>? message = NetworkConfig.Deserialize<TCPMessage<H>>(body);
            if (message != null) OnMessage?.Invoke(ID, message);
            // 继续拼装消息，直到 incoming 不够
            TryMessage();
        }

        public void Send(TCPMessage<H> message) {
            if (state != ConnectionState.Connected) {
                NetworkConfig.logger.warn?.Invoke("Connection is break, cannot send message!");
                return;
            }
            byte[] ?data =  NetworkConfig.Serialize(message);
            if (data != null) {
                // 拼上 Header
                int size = data.Length;
                byte[] pack = new byte[4+size];
                BitConverter.GetBytes(size).CopyTo(pack, 0);
                data.CopyTo(pack, 4);
                Send(pack);
            }
        }

        public void Send(byte[] data)
        {
            if (state != ConnectionState.Connected) {
                NetworkConfig.logger.warn?.Invoke("Connection is break, cannot send message!");
                return;
            }
            bool isEmpty = outcoming.Count <= 0;
            outcoming.Enqueue(data);
            // 开启发送数据
            if (isEmpty)
                Write();
        }

        private void Write() {
            if (socket == null) return;
            byte[] data = outcoming.Dequeue();
            writeSAEA.SetBuffer(data, 0, data.Length);
            if (!socket.SendAsync(writeSAEA))
                OnWrite();
        }

        private void OnWrite()
        {
            SocketError error = readSAEA.SocketError;
            if (error == SocketError.Success)
            {
                if (outcoming.Count > 0)
                    Write();
            }
            else
                Error(ErrorStage.Write, error);
        }

        private void Error(ErrorStage stage, SocketError error) {
            if (error != SocketError.Success)
            {
                string stage_str = stage == ErrorStage.Write ? "[Write]" : "[Read]";
                NetworkConfig.logger.error?.Invoke($"{stage_str} {error}");
                OnError?.Invoke(error);
            }
            EndClose();
        }

        /* TCP 四次挥手
            https://juejin.cn/post/7041167124785528840
            https://zhuanlan.zhihu.com/p/422381485
            https://blog.csdn.net/weixin_30747253/article/details/96319411
            https://www.cnblogs.com/pengyusong/p/6434253.html
         */
        // 主动断开，第一次挥手，后面仍然可以接收数据
        public void Close()
        {
            if(socket != null && state == ConnectionState.Connected)
            {
                state = ConnectionState.HalfClosed;
                socket.Shutdown(SocketShutdown.Send);
                NetworkConfig.logger.error?.Invoke($"Shutdown socket");
            }
        }
        // 接收到挥手后，彻底关闭
        private void EndClose()
        {
            if (socket == null) return;
            /* state == ConnectionState.Connected 状态进入到该方法：代表被动方接收到主动方的挥手，直接断开 socket 两个通道的连接，释放 socket
             * 也就是说被动方的挥手是由程序控制的，这里我们只允许 Close 进行被动方的挥手
             * 
             * 而主动方的挥手，可能是 SocketShutdown.Send 也可能是直接 kill 掉进程（Close）
             *   - SocketShutdown.Send 发送一个 FIN 给被动方，被动方回一个 ACK，然后程序在 Read 里面触发读取到 EOF，进入到该方法，被动方关闭
             *   - 直接 kill 掉进程（Close）也发送一个 FIN 给被动方，被动方回一个 ACK，但是此时主动方是关闭了读，因此主动方回一个 RST
             *   然后被动方在 Read 里面触发 ConnectionReset 错误，进入到该方法，被动方关闭
            */
            // state == ConnectionState.HalfClosed 状态进入到该方法：代表是主动方接收到被动方的挥手，需要进一步关闭读通道，释放 socket
            
            if (state == ConnectionState.Connected)
            {
                try
                {
                    NetworkConfig.logger.error?.Invoke($"Shutdown socket");
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch { };
            }
            if (state == ConnectionState.Connected || state == ConnectionState.HalfClosed)
            {
                state = ConnectionState.Disconnected;
                incoming.Clear();
                outcoming.Clear();

                socket.Close();
                socket = null;
                NetworkConfig.logger.error?.Invoke($"Close socket");

                OnDisconnected?.Invoke(ID);
            }
        }
    }
}
