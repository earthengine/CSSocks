using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Socks5Lib
{
    public static class SocketHelper
    {
        private static async void ContinueWith(Task task, Action a)
        {
            await task.ContinueWith(t => a());
        }

        private static Task GetAsyncCallTask(SocketAsyncEventArgs ev, Func<SocketAsyncEventArgs, bool> act)
        {
            var tcs = new TaskCompletionSource<int>();
            EventHandler<SocketAsyncEventArgs> setResult = (o, e) => tcs.TrySetResult(0);
            ev.Completed += setResult;
            ContinueWith(tcs.Task, () => ev.Completed -= setResult);
            if (!act(ev)) tcs.TrySetResult(0);
            return tcs.Task;
        }

        private static T ReturnEventArgs<T>(SocketAsyncEventArgs ev, Func<SocketAsyncEventArgs, T> get)
        {
            var r = get(ev);
            eventArgs.Add(ev);
            return r;
        }

        internal static Task ConnectAsync(Socket sock, IPEndPoint ep)
        {
            var ev = GetEventArgs();
            ev.RemoteEndPoint = ep;
            return GetAsyncCallTask(ev, sock.ConnectAsync);
        }
        public static async Task<int> ReceiveAsync(Socket s, byte[] buf)
        {
            var ev = GetEventArgs();
            ev.SetBuffer(buf, 0, buf.Length);
            await GetAsyncCallTask(ev, s.ReceiveAsync);
            return ReturnEventArgs(ev, e => e.BytesTransferred);
        }

        public static async Task<Socket> AcceptAsync(Socket s)
        {
            var ev = GetEventArgs();
            ev.AcceptSocket = GetSocket();
            await GetAsyncCallTask(ev, s.AcceptAsync);
            return ReturnEventArgs(ev, e => e.AcceptSocket);
        }

        public static Task SendAsync(Socket s, byte[] buf)
        {
            var ev = GetEventArgs();
            ev.SetBuffer(buf, 0, buf.Length);
            return GetAsyncCallTask(ev, s.SendAsync);
        }

        private static ConcurrentBag<SocketAsyncEventArgs> eventArgs = new ConcurrentBag<SocketAsyncEventArgs>();
        private static SocketAsyncEventArgs GetEventArgs()
        {
            SocketAsyncEventArgs ev;
            if (eventArgs.TryTake(out ev)) return ev;
            else return new SocketAsyncEventArgs();
        }

        private static ConcurrentBag<Socket> socks = new ConcurrentBag<Socket>();
        public static Socket GetSocket()
        {
            Socket sock;
            if (socks.TryTake(out sock)) return sock;
            else return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public static void ReturnSocket(Socket s)
        {
            socks.Add(s);
        }

        private static async void ReceiveData(Socket sock, byte[] buf, MemoryBuffer ms)
        {
            var las = new List<ArraySegment<byte>>() { new ArraySegment<byte>(buf, 0, buf.Length) };
            var s = await ReceiveAsync(sock, buf);
            if (s == 0)
            {
                ms.FinishWrite();
                try {
                    sock.Disconnect(true);
                } catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
                ReturnSocket(sock);
            }
            else
            {
                ms.Write(buf, 0, s);
                ReceiveData(sock, buf, ms);
            }
        }
        public static void ConnectAndHandle(Socket sock, Action<IAsyncReadWrite> handler)
        {
            var buf = new byte[4096];
            var mf = new MemoryBuffer();
            ReceiveData(sock, buf, mf);
            var rw = new DelegateAsyncReadWrite(
                () => sock.Shutdown(SocketShutdown.Send),
                count => mf.ReadExactAsync(count),
                data => SendAsync(sock, data));
            mf.DataWritten += (o, l) => rw.RaiseDataWritten(l);
            handler(rw);
        }
        private static async Task Transfer(IAsyncReadWrite read, IAsyncReadWrite write, int c)
        {
            if (c == 0) return;
            if (c < 0) write.FinishWrite();
            else {
                var data = await read.Read(c);
                await write.Write(data);
            }
        }

        public static async void RelayHandler(IAsyncReadWrite from, IAsyncReadWrite to)
        {
            to.DataWritten += async (o, c) => await Transfer(to, from, c);
            from.DataWritten += async (o, c) => await Transfer(from, to, c);
            await Transfer(from, to, from.Unread);
        }
    }
}
