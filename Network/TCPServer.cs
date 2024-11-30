using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace YLCommon
{
    public class ServerConfig
    {
        public short port;
        public bool startImmediately = false;
        public bool external_handle = false;
        public int connectionPoolSize = 100;
        public int backlog = 10;
    }
    /// <summary>
    /// 提供两个使用方法，一种是直接 += 注册回调，另一种是 override 回调
    /// </summary>
    /// <typeparam name="H">数据包头类型</typeparam>
    /// <typeparam name="T">事件类型</typeparam>
    public class TCPServer<H, T> : EventEmitter<T> where H : TCPHeader
    {
        // TODO: 还需要加权限，用户一旦退出不能使用
        public class NetSession
        {
            private TCPServer<H, T> server;
            public ulong ID;
            public NetSession(TCPServer<H, T> server, ulong ID)
            {
                this.server = server;
                this.ID = ID;
            }

            public void SendTo(ulong ID, TCPMessage<H> message)
            {
                server.SendTo(ID, message);
            }
            public void SendTo(ulong ID, byte[] message)
            {
                server.SendTo(ID, message);
            }
            public void Send(TCPMessage<H> message)
            {
                server.SendTo(ID, message);
            }
            public void Send(byte[] message)
            {
                server.SendTo(ID, message);
            }
            public void SendAll(TCPMessage<H> message)
            {
                server.SendAll(message);
            }
            public void SendAll(byte[] message)
            {
                server.SendAll(message);
            }
            public void SendAll(TCPMessage<H> message, ulong ID)
            {
                server.SendAll(message, ID);
            }
            public void SendAll(byte[] message, ulong ID)
            {
                server.SendAll(message, ID);
            }

            public static bool operator ==(NetSession s1, NetSession? s2)
            {
                return s1.Equals(s2);
            }

            public static bool operator !=(NetSession s1, NetSession? s2)
            {
                return !(s1 == s2);
            }

            public override bool Equals(object? obj)
            {
                return obj is NetSession s && s.ID == ID && s.server == server;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ID, server.GetHashCode());
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

        private TCPConnectionPool<H> conPool;
        private Dictionary<ulong, TCPConnection<H>> clients;

        // 通过信号量 限制最大连接数
        private Semaphore acceptSemaphore;
        private ulong clientID = 100;
        public int ClientCount => clients.Count;

        // 外部信号

        /// <summary>
        /// 客户端连接回调
        /// </summary>
        public Action<ulong>? OnClientDisconnected;

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        public Action<ulong>? OnClientConnected;

        /// <summary>
        /// 接收消息回调，OnMessage 和 OnPackage 是两种不同风格的形式，只需要写一个即可
        /// </summary>
        public Action<ulong, TCPMessage<H>>? OnMessage;

        /// <summary>
        /// 接收消息回调，OnMessage 和 OnPackage 是两种不同风格的形式，只需要写一个即可
        /// </summary>
        public Action<NetPackage>? OnPackage;

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public Action<SocketError>? OnError;

        public ServerConfig config;

        public TCPServer(ServerConfig config) {
            this.config = config;

            OnClientDisconnected += ClientDisconnected;
            OnClientConnected += ClientConnected;
            OnMessage += Message;
            OnPackage += Package;
            OnError += Error;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, config.port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);

            saea = new SocketAsyncEventArgs();
            saea.Completed += Saea_Completed;

            conPool = new(config.connectionPoolSize);
            clients = new();
            acceptSemaphore = new(config.connectionPoolSize, config.connectionPoolSize);

            if (config.external_handle) packages = new();

            if (config.startImmediately) Start();
        }

        public void Start()
        {
            // 开始监听，并接收请求
            socket.Listen(config.backlog);
            NetworkConfig.logger.info?.Invoke($"Server Start at {config.port} Port");
            StartAccept();
            Init();
        }

        private void StartAccept()
        {
            // 信号量减一, 池子里有才监听下一个请求
            acceptSemaphore.WaitOne();
            // 需要置 null 才能接收下一个 socket
            saea.AcceptSocket = null;
            bool suspend = socket.AcceptAsync(saea);
            if (!suspend)
                OnConnection();
        }

        private void Saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            SocketError error = saea.SocketError;
            if (error == SocketError.Success)
                OnConnection();
            else
                OnError?.Invoke(error);
        }

        private void OnConnection()
        {
            Socket socket = saea.AcceptSocket;
            TCPConnection<H> con = conPool.Pop();
            con.Init(socket, ++clientID);
            lock (clients)
            {
                clients.Add(con.ID, con);
            }
            // 用户下线回调
            con.OnDisconnected += OnDisconnect;
            // 用户发送了消息回调
            con.OnMessage += PackMessage;
            con.OnError += OnError;
            NetworkConfig.logger.info?.Invoke($"New Connection {socket.RemoteEndPoint}");
            OnClientConnected?.Invoke(con.ID);
            StartAccept();
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
                OnMessage?.Invoke(ID, message);
                NetPackage package = new NetPackage { message = message, session = new NetSession(this, ID) };
                OnPackage?.Invoke(package);
            }
        }

        public new void Tick()
        {
            if (!config.external_handle || packages == null) return;
            
            while (!packages.IsEmpty)
            {
                if (packages.TryDequeue(out NetPackage package))
                {
                    OnMessage?.Invoke(package.session.ID, package.message);
                    OnPackage?.Invoke(package);
                }
            }

            base.Tick();
        }


        private void OnDisconnect(ulong ID)
        {
            if (clients.ContainsKey(ID))
            {
                conPool.Push(clients[ID]);
                lock (clients)
                {
                    clients.Remove(ID);
                }
                // 信号量加一
                acceptSemaphore.Release();
                OnClientDisconnected?.Invoke(ID);
            }
            else
                NetworkConfig.logger.error?.Invoke($"Remove client [{ID}] not found");
        }

        // 服务器主动关闭连接
        public void Disconnect(ulong ID)
        {
            if (clients.TryGetValue(ID, out var con))
                con.Close();
        }

        // TODO: 关闭整个服务器
        public void Close()
        {
            UnInit();
        }

        public void SendTo(ulong ID, TCPMessage<H> message)
        {
            if(clients.TryGetValue(ID, out var con))
                con.Send(message);
        }

        public void SendTo(ulong ID, byte[] message)
        {
            if (clients.TryGetValue(ID, out var con))
                con.Send(message);
        }

        public void SendAll(TCPMessage<H> message)
        {
            foreach (var item in clients)
            {
                item.Value.Send(message);
            }
        }
        public void SendAll(byte[] message)
        {
            foreach (var item in clients)
            {
                item.Value.Send(message);
            }
        }

        public void SendAll(TCPMessage<H> message, ulong ID)
        {
            foreach (var item in clients)
            {
                if (item.Key == ID) continue;
                item.Value.Send(message);
            }   
        }

        public void SendAll(byte[] message, ulong ID)
        {
            foreach (var item in clients)
            {
                if (item.Key == ID) continue;
                item.Value.Send(message);
            }
        }

        /// <summary>
        /// 客户端连接回调
        /// </summary>
        protected virtual void ClientDisconnected(ulong ID) { }

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        protected virtual void ClientConnected(ulong ID) { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        protected virtual void Message(ulong ID, TCPMessage<H> message) { }

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
