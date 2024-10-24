
using System.Collections.Generic;

namespace YLCommon
{
    public class TCPConnectionPool<T> where T : TCPMessage
    {
        private readonly Stack<TCPConnection<T>> pool;

        public int Size => pool.Count;

        public TCPConnectionPool(int capacity)
        {
            pool = new(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var con = new TCPConnection<T>();
                con.isServer = true;
                pool.Push(con);
            }
        }

        public TCPConnection<T> Pop() {
            lock (pool)
            {
                return pool.Pop();
            }
        }

        public void Push(TCPConnection<T> con)
        {
            if (con == null) return;
            lock (pool)
            {
                // 非复用变量置空
                con.ID = 0;
                con.OnMessage = null;
                con.OnError = null;
                con.OnDisconnected = null;
                con.state = TCPConnection<T>.ConnectionState.None;
                pool.Push(con);
            }
        }
    }
}
