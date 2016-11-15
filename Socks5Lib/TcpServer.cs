using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Socks5Lib
{
    public class TcpServer
    {
        private Socket sli;
        private readonly ushort port;
        private readonly Action<IAsyncReadWrite> handler;

        public TcpServer(ushort port, Action<IAsyncReadWrite> handler)
        {
            this.port = port;
            this.handler = handler;
        }

        public void Start()
        {
            sli = GetSocket();
            sli.Bind(new IPEndPoint(IPAddress.Any, port));
            sli.Listen(5);
            for (var i = 0; i < 5; ++i)
            {
                AcceptConnection(sli);
            }
            Dispatcher.Run();
        }

        private async void ReceiveData(Socket sock, byte[] buf, MemoryBuffer ms)
        {
            var las = new List<ArraySegment<byte>>() { new ArraySegment<byte>(buf, 0, buf.Length) };
            var s = await ReceiveAsync(sock, buf);
            if (s == 0)
            {
                ms.FinishWrite();
                sock.Disconnect(true);
                socks.Add(sock);
            }
            else
            {
                ms.Write(buf, 0, s);
                ReceiveData(sock, buf, ms);
            }
        }

        private async void AcceptConnection(Socket sli)
        {
            var sock = await AcceptAsync(sli);
            AcceptConnection(sli);
            var buf = new byte[4096];
            var mf = new MemoryBuffer();
            ReceiveData(sock, buf, mf);
            handler(SocketAsyncReadWrite.Create(sock, mf));
        }
        
        private static async void ContinueWith(Task task, Action a)
        {
            await task.ContinueWith(t => a());
        }

        private static Task GetAsyncCallTask(SocketAsyncEventArgs ev)
        {
            var tcs = new TaskCompletionSource<int>();
            EventHandler<SocketAsyncEventArgs> setResult = (o, e) => tcs.TrySetResult(0);
            ev.Completed += setResult;
            ContinueWith(tcs.Task, () => ev.Completed -= setResult);
            return tcs.Task;
        }

        private static async Task<int> ReceiveAsync(Socket s, byte[] buf)
        {
            var ev = GetEventArgs();
            ev.SetBuffer(buf, 0, buf.Length);
            var tsk = GetAsyncCallTask(ev);
            s.ReceiveAsync(ev);
            await tsk;
            return ev.BytesTransferred;
        }

        private static async Task<Socket> AcceptAsync(Socket s)
        {
            var ev = GetEventArgs();
            ev.AcceptSocket = GetSocket();
            var tsk = GetAsyncCallTask(ev);
            s.AcceptAsync(ev);
            await tsk;
            return ev.AcceptSocket;
        }

        private static ConcurrentBag<SocketAsyncEventArgs> eventArgs = new ConcurrentBag<SocketAsyncEventArgs>();
        private static SocketAsyncEventArgs GetEventArgs()
        {
            SocketAsyncEventArgs ev;
            if (eventArgs.TryTake(out ev)) return ev;
            else return new SocketAsyncEventArgs();
        }

        private static ConcurrentBag<Socket> socks = new ConcurrentBag<Socket>();
        private static Socket GetSocket()
        {
            Socket sock;
            if (socks.TryTake(out sock)) return sock;
            else return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }

}
