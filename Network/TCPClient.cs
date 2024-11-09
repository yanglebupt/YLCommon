using System;
using System.Net;
using System.Net.Sockets;


namespace YLCommon
{
    /// <summary>
    /// 提供两个使用方法，一种是直接 new TCPClient，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPClient<T>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class TCPClient<T> where T : TCPMessage
    {
        private Socket socket;
        private SocketAsyncEventArgs saea;
        
        private TCPConnection<T> ?connection;

        // 外部信号

        /// <summary>
        /// 连接成功回调
        /// </summary>
        public Action? OnConnected;

        /// <summary>
        /// 连接失败回调
        /// </summary>
        public Action? OnConnectionFailed;

        /// <summary>
        /// 断开连接回调
        /// </summary>
        public Action? OnDisconnected;

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public Action<T>? OnMessage;

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public Action<SocketError>? OnError;

        public TCPClient(string ip, short port, bool connectImmediately = false) {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // 异步连接
            saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = endPoint;
            saea.Completed += Saea_Completed;

            if (connectImmediately) Connect();
        }

        public void Connect()
        {
            // 返回IO是否挂起
            // 如果立刻返回 false，代表连接建立成功，同步执行
            // 返回 true，代表连接还未建立成功，需要异步等待完成事件触发
            bool suspend = socket.ConnectAsync(saea);
            if (!suspend)
                Connected();
        }

        private void Saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            SocketError error = saea.SocketError;
            if (error == SocketError.Success)
                Connected();
            else if (error == SocketError.ConnectionRefused)
                OnConnectionFailed?.Invoke();
            else
                OnError?.Invoke(error);
        }

        private void Connected()
        {
            connection = new();
            connection.Init(socket, 1);
            connection.OnError += OnError;
            connection.OnMessage += (ulong _, T m) => OnMessage?.Invoke(m);
            connection.OnDisconnected += (ulong _) => OnDisconnected?.Invoke();
            OnConnected?.Invoke();
        }

        
        // 客户端主动关闭连接
        public void Close()
        {
            connection?.Close();
        }

        public void Send(T message)
        {
            connection?.Send(message);
        }

        public void Send(byte[] data)
        {
            connection?.Send(data);
        }
    }

    /// <summary>
    /// 提供两个使用方法，一种是直接 new TCPClient，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPClient<T>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public abstract class ITCPClient<T> : TCPClient<T> where T: TCPMessage
    {
        public ITCPClient(string ip, short port, bool connectImmediately = false) : base(ip, port, connectImmediately) {
            OnConnected += Connected;
            OnConnectionFailed += ConnectionFailed;
            OnDisconnected += Disconnected;
            OnMessage += Message;
            OnError += Error;
        }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        public abstract void Connected();

        /// <summary>
        /// 连接失败回调
        /// </summary>
        public abstract void ConnectionFailed();

        /// <summary>
        /// 断开连接回调
        /// </summary>
        public abstract void Disconnected();

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public abstract void Message(T message);

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public virtual void Error(SocketError error) { }
    }
}
