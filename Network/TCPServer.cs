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
    /// 提供两个使用方法，一种是直接 new TCPServer，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPServer<T>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class TCPServer<T> where T : TCPMessage
    {
        public class NetSession
        {
            private TCPServer<T> server;
            public ulong ID;
            public NetSession(TCPServer<T> server, ulong ID)
            {
                this.server = server;
                this.ID = ID;
            }

            public void SendTo(ulong ID, T message)
            {
                server.SendTo(ID, message);
            }
            public void SendTo(ulong ID, byte[] message)
            {
                server.SendTo(ID, message);
            }
            public void Send(T message)
            {
                server.SendTo(ID, message);
            }
            public void Send(byte[] message)
            {
                server.SendTo(ID, message);
            }
            public void SendAll(T message)
            {
                server.SendAll(message);
            }
            public void SendAll(byte[] message)
            {
                server.SendAll(message);
            }
            public void SendAll(T message, ulong ID)
            {
                server.SendAll(message, ID);
            }
            public void SendAll(byte[] message, ulong ID)
            {
                server.SendAll(message, ID);
            }
        }
        public class NetPackage
        {
            public NetSession session;
            public T message;
        }
        private ConcurrentQueue<NetPackage>? packages;

        private Socket socket;
        private SocketAsyncEventArgs saea;

        private TCPConnectionPool<T> conPool;
        private Dictionary<ulong, TCPConnection<T>> clients;

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
        public Action<ulong, T>? OnMessage;

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
            TCPConnection<T> con = conPool.Pop();
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

        private void PackMessage(ulong ID, T message)
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

        public void Tick()
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

        public void SendTo(ulong ID, T message)
        {
            if(clients.TryGetValue(ID, out var con))
                con.Send(message);
        }

        public void SendTo(ulong ID, byte[] message)
        {
            if (clients.TryGetValue(ID, out var con))
                con.Send(message);
        }

        public void SendAll(T message)
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

        public void SendAll(T message, ulong ID)
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
    }


    /// <summary>
    /// 提供两个使用方法，一种是直接 new TCPServer，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPServer<T>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public abstract class ITCPServer<T> : TCPServer<T> where T : TCPMessage
    {
        protected ITCPServer(ServerConfig config) : base(config) {
            OnClientDisconnected += ClientDisconnected;
            OnClientConnected += ClientConnected;
            OnMessage += Message;
            OnPackage += Package;
            OnError += Error;
        }

        /// <summary>
        /// 客户端连接回调
        /// </summary>
        public virtual void ClientDisconnected(ulong ID) { }

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        public virtual void ClientConnected(ulong ID) { }

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public virtual void Message(ulong ID, T message) { }

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
