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

        private async void AcceptConnection(Socket sli)
        {
            var sock = await SocketHelper.AcceptAsync(sli);
            AcceptConnection(sli);
            SocketHelper.ConnectAndHandle(sock, handler);
        }
        
    }

}
