using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Socks5Lib
{
    public class Socks5Handler
    {
        private IAsyncReadWrite readWrite;

        private Task<IPEndPoint> ReadEndPoint(int type)
        {
            switch (type)
            {
                case 0x01: //IP v4 end point
                    return ReadIPAddress();
                case 0x03: //Domain name
                    return ReadDomain();
                case 0x04: //IPv6 end point
                    return ReadIP6Address();
            }
            return null;
        }
        private void RunCommand(IPEndPoint ep, int cmd)
        {
            switch (cmd)
            {
                case 0x01: //CONNECT
                    ConnectCommand(ep);
                    break;
                case 0x02: //BIND
                    BindCommand(ep);
                    break;
                case 0x03: //UDP ASSOCIATE
                    UdpAssociateCommand(ep);
                    break;
            }
        }

        private Task<IPEndPoint> ReadIP6Address()
        {
            throw new NotImplementedException();
        }
        private async Task<IPEndPoint> ReadDomain()
        {
            var c = await readWrite.Read(1);
            var s = Encoding.ASCII.GetString(await readWrite.Read(c[0]));
            var p = await readWrite.Read(2);
            var addresses = Dns.GetHostAddresses(s);
            if (addresses.Length == 0)
            {
                throw new Exception("Domain name not found");
            }
            else
            {
                return new IPEndPoint(addresses[0], (p[0] << 8) | p[1]);
            }
        }
        private async Task<IPEndPoint> ReadIPAddress()
        {
            var d = await readWrite.Read(4);
            var p = await readWrite.Read(2);
            return new IPEndPoint(new IPAddress(d), (p[0] << 8) | p[1]);
        }

        private void BindCommand(IPEndPoint ep)
        {
            throw new NotImplementedException();
        }
        private void ConnectCommand(IPEndPoint ep)
        {
            throw new NotImplementedException();
        }
        private void UdpAssociateCommand(IPEndPoint ep)
        {
            throw new NotImplementedException();
        }

        public Socks5Handler(IAsyncReadWrite readWrite)
        {
            this.readWrite = readWrite;
        }
        public async Task ParseInput()
        {
            //byte 0: 0x05 -- SOCKS 5 ver
            var input = await readWrite.Read(2);
            //byte 1: number of methods
            await readWrite.Read(input[1]);

            await readWrite.Write(new byte[] { 0x05, 0x00 });

            for (;;)
            {
                input = await readWrite.Read(4);
                IPEndPoint ep = await ReadEndPoint(input[3]);
                if (ep == null) break;
                RunCommand(ep, input[1]);
            }

            readWrite.FinishWrite();
        }
    }

}
