using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
            sli = SocketHelper.GetSocket();
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
            var s = await SocketHelper.ReceiveAsync(sock, buf);
            if (s == 0)
            {
                ms.FinishWrite();
                sock.Disconnect(true);
                SocketHelper.ReturnSocket(sock);
            }
            else
            {
                ms.Write(buf, 0, s);
                ReceiveData(sock, buf, ms);
            }
        }

        private async void AcceptConnection(Socket sli)
        {
            var sock = await SocketHelper.AcceptAsync(sli);
            AcceptConnection(sli);
            var buf = new byte[4096];
            var mf = new MemoryBuffer();
            ReceiveData(sock, buf, mf);
            handler(new DelegateAsyncReadWrite(
                () => sock.Shutdown(SocketShutdown.Send),
                count => mf.ReadExactAsync(count),
                data =>  SendAsync(sock, data)
            ));
        }
        
        private static Task SendAsync(Socket s, byte[] buf)
        {
            var ev = GetEventArgs();
            ev.SetBuffer(buf, 0, buf.Length);
            var tsk = GetAsyncCallTask(ev);
            s.SendAsync(ev);
            return tsk;
        }
    }

}
