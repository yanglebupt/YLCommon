
using System.Collections.Generic;

namespace YLCommon
{
    public class TCPConnectionPool<H> where H : TCPHeader
    {
        private readonly Stack<TCPConnection<H>> pool;

        public int Size => pool.Count;

        public TCPConnectionPool(int capacity)
        {
            pool = new(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var con = new TCPConnection<H>();
                pool.Push(con);
            }
        }

        public TCPConnection<H> Pop() {
            lock (pool)
            {
                return pool.Pop();
            }
        }

        public void Push(TCPConnection<H> con)
        {
            if (con == null) return;
            lock (pool)
            {
                // 非复用变量置空
                con.ID = 0;
                con.OnMessage = null;
                con.OnError = null;
                con.OnDisconnected = null;
                con.state = TCPConnection<H>.ConnectionState.None;
                pool.Push(con);
            }
        }
    }
}
