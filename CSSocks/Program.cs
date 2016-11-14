using Socks5Lib;

namespace CSSocks
{
    class Program
    {
        static void Main(string[] args)
        {
            var svr = new Socks5Server();
            svr.Start();
        }
    }
}
