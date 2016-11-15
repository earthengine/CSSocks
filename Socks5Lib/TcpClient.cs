using System;
using System.Net;
using System.Threading.Tasks;

namespace Socks5Lib
{
    internal class TcpClient
    {
        private IPEndPoint remote;
        private IPEndPoint local;

        public TcpClient(IPEndPoint ep)
        {
            remote = ep;
        }

        public async Task Connect()
        {
            var sock = SocketHelper.GetSocket();
            await SocketHelper.ConnectAsync(sock, remote);
            local = (IPEndPoint)sock.LocalEndPoint;
        }
        public IPAddress Source { get { return local.Address; } }
        public ushort Port { get { return (ushort)local.Port; } }

        public void BindWith(IAsyncReadWrite readWrite)
        {
            readWrite.
        }
    }
}