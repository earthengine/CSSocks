using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Socks5Lib
{
    public interface IAsyncReadWrite
    {
        /// <summary>
        /// Read specific bytes of data, wait until avaiable or wrote finished. In case the written finished, length of the result might less than requested
        /// </summary>
        /// <param name="count">Amount of data requested</param>
        /// <returns>The data read</returns>
        Task<byte[]> Read(int count);
        /// <summary>
        /// Write data
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns></returns>
        Task Write(byte[] data);
        /// <summary>
        /// Indicate writing is finished and no more data to be written again
        /// </summary>
        void FinishWrite();
    }

    public class SocketAsyncReadWrite : IAsyncReadWrite
    {
        private Socket sock;
        private IMemoryBuffer buf;
        private SocketAsyncReadWrite(Socket sock, IMemoryBuffer buf)
        {
            this.sock = sock;
            this.buf = buf;
        }
        public void FinishWrite()
        {
            sock.Shutdown(SocketShutdown.Send);
        }

        public Task<byte[]> Read(int count)
        {
            return buf.ReadExactAsync(count);
        }

        public Task Write(byte[] data)
        {
            var las = new List<ArraySegment<byte>>() { new ArraySegment<byte>(data, 0, data.Length) };
            return Task.Factory.FromAsync(sock.BeginSend, sock.EndSend, las, SocketFlags.None, null);
        }
        public static IAsyncReadWrite Create(Socket sock, IMemoryBuffer buf)
        {
            return new SocketAsyncReadWrite(sock, buf);
        }
    }
}
