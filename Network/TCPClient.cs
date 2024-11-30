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
    /// 提供两个使用方法，一种是直接 += 注册回调，另一种是 override 回调
    /// </summary>
    /// <typeparam name="H">数据包头类型</typeparam>
    public class TCPClient<H> where H : TCPHeader
    {
        // TODO: 还需要加权限，用户一旦退出不能使用
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

            public static bool operator ==(NetSession s1, NetSession? s2){
                return s1.Equals(s2);
            }

            public static bool operator !=(NetSession s1, NetSession? s2)
            {
                return !(s1 == s2);
            }

            public override bool Equals(object? obj)
            {
                return obj is NetSession s && s.ID == ID && s.client == client;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ID, client.GetHashCode());
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

            OnConnected += Connected;
            OnConnectionFailed += ConnectionFailed;
            OnDisconnected += Disconnected;
            OnMessage += Message;
            OnPackage += Package;
            OnError += Error;

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
                OnConnection();
        }

        private void Saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            SocketError error = saea.SocketError;
            if (error == SocketError.Success)
                OnConnection();
            else if (error == SocketError.ConnectionRefused)
                OnConnectionFailed?.Invoke();
            else
                OnError?.Invoke(error);
        }

        private void OnConnection()
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

        /// <summary>
        /// 连接成功回调
        /// </summary>
        protected virtual void Connected() { }

        /// <summary>
        /// 连接失败回调
        /// </summary>
        protected virtual void ConnectionFailed() { }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        protected virtual void Disconnected() { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        protected virtual void Message(TCPMessage<H> message) { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        protected virtual void Package(NetPackage package) { }

        /// <summary>
        /// 其他错误回调
        /// </summary>
        protected virtual void Error(SocketError error) { }
    }
}
