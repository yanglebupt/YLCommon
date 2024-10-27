using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace YLCommon
{
    /// <summary>
    /// 提供两个使用方法，一种是直接 new TCPServer，然后注册回调函数进行处理
    /// 另一种是先实现抽象类 ITCPServer<T>，在类的抽象方法里面进行处理，然后在 new 继承的类即可
    /// </summary>
    /// <typeparam name="T">数据包类型</typeparam>
    public class TCPServer<T> where T : TCPMessage
    {
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
        /// 接收消息回调
        /// </summary>
        public Action<ulong, T>? OnMessage;
        
        /// <summary>
        /// 其他错误回调
        /// </summary>
        public Action<SocketError>? OnError;

        public TCPServer(short port, int connectionPoolSize = 100 ,int backlog = 10) {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);

            saea = new SocketAsyncEventArgs();
            saea.Completed += Saea_Completed;

            
            conPool = new(connectionPoolSize);
            clients = new();
            acceptSemaphore = new(connectionPoolSize, connectionPoolSize);
            

            // 开始监听，并接收请求
            socket.Listen(backlog);

            NetworkConfig.logger.ok?.Invoke($"Server Start at {port} Port");

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
            con.OnMessage += OnMessage;
            con.OnError += OnError;
            NetworkConfig.logger.ok?.Invoke($"New Connection {socket.RemoteEndPoint}");
            OnClientConnected?.Invoke(con.ID);
            StartAccept();
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
        protected ITCPServer(short port, int connectionPoolSize = 100, int backlog = 10) : base(port, connectionPoolSize, backlog) {
            OnClientDisconnected += ClientDisconnected;
            OnClientConnected += ClientConnected;
            OnMessage += Message;
            OnError += Error;
        }

        /// <summary>
        /// 客户端连接回调
        /// </summary>
        public abstract void ClientDisconnected(ulong ID);

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        public abstract void ClientConnected(ulong ID);

        /// <summary>
        /// 接收消息回调
        /// </summary>
        public abstract void Message(ulong ID, T message);

        /// <summary>
        /// 其他错误回调
        /// </summary>
        public virtual void Error(SocketError error) { }
    }
}
