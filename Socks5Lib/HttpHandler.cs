﻿using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Socks5Lib
{
    public class HttpHandler
    {
        private IAsyncReadWrite buf;
        public HttpHandler(IAsyncReadWrite buf)
        {
            this.buf = buf;
        }

        private async Task<byte[]> ReadUntil(byte b)
        {
            var ms = new MemoryBuffer();
            var l = 0;
            for (;;)
            {
                var c = await buf.Read(1);
                if (c[0] == Convert.ToByte('\r')) return await ms.ReadExactAsync(l);
                ms.Write(c, 0, 1);
                l++;
            }
        }
        private async Task<byte[]> ReadLine()
        {
            var ms = new MemoryBuffer();
            var c = await ReadUntil(Convert.ToByte('\r'));
            var d = await buf.Read(1);
            var l = c.Length;
            ms.Write(c, 0, c.Length);
            if (d[0] != Convert.ToByte('\n'))
            {
                ms.Write(new[] { Convert.ToByte('\r') }, 0, 1);
                ms.Write(d, 0, 1);
                var rest = await ReadLine();
                ms.Write(rest, 0, rest.Length);
                l += rest.Length + 2;
            }
            return await ms.ReadExactAsync(l);
        }

        public async Task ParseInput()
        {
            var tc = new TcpClient(new IPEndPoint(Dns.GetHostAddresses("www.facebook.com")[0], 80));
            await tc.Connect();
            //await buf.Write(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nConnection: Closed\r\n\r\nHello, World!"));
            //buf.FinishWrite();
            tc.Handle(async aw =>
            {
                for (;;)
                {
                    var bl = await ReadLine();
                    var l = Encoding.ASCII.GetString(bl);
                    if (l.StartsWith("Host: "))
                        bl = Encoding.ASCII.GetBytes("Host: www.facebook.com:80");
                    await aw.Write(bl);
                    await aw.Write(Encoding.ASCII.GetBytes("\r\n"));
                    if (l.Length == 0) break;
                }
                SocketHelper.RelayHandler(buf, aw);
            });
        }
    }
}
