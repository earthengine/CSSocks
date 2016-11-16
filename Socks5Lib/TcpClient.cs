using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Socks5Lib
{
    internal class TcpClient
    {
        private IPEndPoint remote;
        private IPEndPoint local;
        private Socket sock;

        public TcpClient(IPEndPoint ep)
        {
            remote = ep;
        }

        public async Task Connect()
        {
            sock = SocketHelper.GetSocket();
            await SocketHelper.ConnectAsync(sock, remote);
            local = (IPEndPoint)sock.LocalEndPoint;
        }
        public IPAddress Source { get { return local.Address; } }
        public ushort Port { get { return (ushort)local.Port; } }

        public void Handle(Action<IAsyncReadWrite> handler)
        {
            SocketHelper.ConnectAndHandle(sock, handler);
        }
    }
}