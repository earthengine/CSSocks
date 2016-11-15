using System;
using System.Collections.Concurrent;
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
    }
}
