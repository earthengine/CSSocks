using Socks5Lib;

namespace CSSocks
{
    class Program
    {
        static void Main(string[] args)
        {
            var svr = new TcpServer(8788, async rw =>
            {
                await new Socks5Handler(rw).ParseInput();
            });
            svr.Start();
        }
    }
}
