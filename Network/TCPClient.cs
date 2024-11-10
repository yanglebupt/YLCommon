using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;


namespace YLCommon
{
    public class ClientConfig
    {
        public string ip;
        public short port;
        public bool connectImmediately = false;
        public bool external_handle = false;
    }

    /// <summary>
    /// 提供两个使用方法，一种是直接 new TCPClient，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPClient<TCPMessage<H>>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class TCPClient<H> where H : TCPHeader
    {
        public class NetSession
        {
            private TCPClient<H> client;
            public ulong ID;
            public NetSession(TCPClient<H> client, ulong ID)
            {
                this.client = client;
                this.ID = ID;
            }

            public void Send(TCPMessage<H> message)
            {
                client.Send(message);
            }
            public void Send(byte[] data)
            {
                client.Send(data);
            }
        }
        public class NetPackage
        {
            public NetSession session;
            public TCPMessage<H> message;
        }
        private ConcurrentQueue<NetPackage>? packages;

        private Socket socket;
        private SocketAsyncEventArgs saea;
        
        private TCPConnection<H> ?connection;

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
        /// 接收消息回调，OnMessage 和 OnPackage 是两种不同风格的形式，只需要写一个即可
        /// </summary>
        public Action<TCPMessage<H>>? OnMessage;

        /// <summary>
        /// 接收消息回调，OnMessage 和 OnPackage 是两种不同风格的形式，只需要写一个即可
        /// </summary>
        public Action<NetPackage>? OnPackage;

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public Action<SocketError>? OnError;

        public ClientConfig config;

        public TCPClient(ClientConfig config) {
            this.config = config;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(config.ip), config.port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // 异步连接
            saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = endPoint;
            saea.Completed += Saea_Completed;

            if (config.external_handle) packages = new();

            if (config.connectImmediately) Connect();
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
            connection.OnMessage += PackMessage;
            connection.OnDisconnected += (ulong _) => OnDisconnected?.Invoke();
            OnConnected?.Invoke();
        }

        private void PackMessage(ulong ID, TCPMessage<H> message)
        {
            if (config.external_handle)
            {
                NetPackage package = new NetPackage { message = message, session = new NetSession(this, ID) };
                packages!.Enqueue(package);
            }
            else
            {
                OnMessage?.Invoke(message);
                NetPackage package = new NetPackage { message = message, session = new NetSession(this, ID) };
                OnPackage?.Invoke(package);
            }
        }

        public void Tick()
        {
            if (!config.external_handle || packages == null) return;
            while (!packages.IsEmpty)
            {
                if (packages.TryDequeue(out NetPackage package))
                {
                    OnMessage?.Invoke(package.message);
                    OnPackage?.Invoke(package);
                }
            }
        }

        // 客户端主动关闭连接
        public void Close()
        {
            connection?.Close();
        }

        public void Send(TCPMessage<H> message)
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
    /// 另一种是先实现抽象类 ITCPClient<TCPMessage<H>>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public abstract class ITCPClient<H>: TCPClient<H> where H : TCPHeader
    {
        public ITCPClient(ClientConfig config) : base(config) {
            OnConnected += Connected;
            OnConnectionFailed += ConnectionFailed;
            OnDisconnected += Disconnected;
            OnMessage += Message;
            OnPackage += Package;
            OnError += Error;
        }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        public virtual void Connected() { }

        /// <summary>
        /// 连接失败回调
        /// </summary>
        public virtual void ConnectionFailed() { }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        public virtual void Disconnected() { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public virtual void Message(TCPMessage<H> message) { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public virtual void Package(NetPackage package) { }

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public virtual void Error(SocketError error) { }
    }
}
