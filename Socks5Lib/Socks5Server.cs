using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Socks5Lib
{
    public class ReadWriteMemoryStream : Stream
    {
        private MemoryStream ms = new MemoryStream();
        private TaskCompletionSource<long> dataWrote = new TaskCompletionSource<long>();

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get { return ms.Length; } }
        public override long Position {
            get { return ms.Position; }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            lock (ms)
            {
                ms.Flush();
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (ms)
            {
                var result = ms.Read(buffer, offset, count);
                if (result < count)
                    dataWrote = new TaskCompletionSource<long>();
                return result;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (ms)
            {
                var pos = ms.Position;
                ms.Seek(0, SeekOrigin.End);
                ms.Write(buffer, offset, count);
                ms.Seek(pos, SeekOrigin.Begin);
                dataWrote.TrySetResult(Length - Position);
            }
        }

        private async Task ReadFullAsync(byte[] data, int offset, int count)
        {
            var s = Read(data, offset, count);
            if (s < count)
            {
                await dataWrote.Task;
                await ReadAsync(data, offset + s, count - s);
            }
        }

        public async Task<byte[]> ReadFullAsync(int count)
        {
            var data = new byte[count];
            await ReadFullAsync(data, 0, count);
            return data;
        }
        public async Task<byte> ReadFullAsync()
        {
            return (await ReadFullAsync(1))[0];
        }
        public byte[] ReadAllAvailable()
        {
            var data = new byte[ms.Length - ms.Position];
            Read(data, 0, data.Length);
            return data;
        }
    }

    public class Socks5Server
    {
        private Socket sli;        

        public void Start()
        {
            sli = GetSocket();
            sli.Bind(new IPEndPoint(IPAddress.Any, 8788));
            sli.Listen(5);
            for (var i = 0; i < 5; ++i)
            {
                AcceptAsync(sli);
            }
            Dispatcher.Run();
        }

        private async Task ParseInput(Socket sock, ReadWriteMemoryStream ms)
        {
            //byte 0: 0x05 -- SOCKS 5 ver
            var input = await ms.ReadFullAsync(2);
            //byte 1: number of methods
            await ms.ReadFullAsync(input[1]);

            sock.Send(new byte[] { 0x05, 0x00 });

            for (; ;){
                input = await ms.ReadFullAsync(4);
                IPEndPoint ep;
                switch (input[3])
                {
                    case 0x01: //IP v4 end point
                        ep = ReadIPAddress(ms);
                        break;
                    case 0x03: //Domain name
                        ep = ReadDomain(ms);
                        break;
                    case 0x04: //IPv6 end point
                        ep = ReadIP6Address(ms);
                        break;
                }
                switch (input[1])
                {
                    case 0x01: //CONNECT
                        ConnectCommand(sock, ms, ep);
                        break;
                    case 0x02: //BIND
                        BindCommand(sock, ms, ep);
                        break;
                    case 0x03: //UDP ASSOCIATE
                        UdpAssociateCommand(sock, ms, ep);
                        break;
                }
            }

            sock.Shutdown(SocketShutdown.Send);            
        }

        private IPEndPoint ReadIP6Address(ReadWriteMemoryStream ms)
        {
            throw new NotImplementedException();
        }

        private async Task<IPEndPoint> ReadDomain(ReadWriteMemoryStream ms)
        {
            var c = await ms.ReadFullAsync();
            var s = Encoding.ASCII.GetString(await ms.ReadFullAsync(c));
            var p = await ms.ReadFullAsync(2);
            var addresses = Dns.GetHostAddresses(s);
            if (addresses.Length == 0)
            {
                throw new Exception("Domain name not found");
            } else
            {
                return new IPEndPoint(addresses[0], (p[0] << 8) | p[1]);
            }
        }

        private async Task<IPEndPoint> ReadIPAddress(ReadWriteMemoryStream ms)
        {
            var d = await ms.ReadFullAsync(4);
            var p = await ms.ReadFullAsync(2);
            return new IPEndPoint(new IPAddress(d), (p[0] << 8) | p[1]);
        }

        private void BindCommand(Socket sock, ReadWriteMemoryStream ms, IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        private void ConnectCommand(Socket sock, ReadWriteMemoryStream ms, IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        private void UdpAssociateCommand(Socket sock, ReadWriteMemoryStream ms, IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        private void StartReceive(Socket sock, byte[] buf, ReadWriteMemoryStream ms)
        {
            sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, ir =>
            {
                var s = sock.EndReceive(ir);
                if (s == 0)
                {
                    sock.Disconnect(false);
                    sock.Close();
                }
                else
                {
                    ms.Write(buf, 0, s);
                    StartReceive(sock, buf, ms);
                }
            }, null);

        }

        private void AcceptAsync(Socket sli)
        {
            sli.BeginAccept(GetSocket(), 0, ir => {
                var sock = sli.EndAccept(ir);
                var buf = new byte[4096];
                var ms = new ReadWriteMemoryStream();
                StartReceive(sock, buf, ms);
                Task.Run(() => ParseInput(sock, ms));
                AcceptAsync(sli);
            }, null);
        }

        private Socket GetSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }

}
