using System;
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

    public class DelegateAsyncReadWrite : IAsyncReadWrite
    {
        private Action finishWrite;
        private Func<int, Task<byte[]>> read;
        private Func<byte[], Task> write;

        public DelegateAsyncReadWrite(Action finishWrite, Func<int, Task<byte[]>> read, Func<byte[], Task> write)
        {
            this.finishWrite = finishWrite;
            this.read = read;
            this.write = write;
        }

        public void FinishWrite()
        {
            finishWrite();
        }

        public Task<byte[]> Read(int count)
        {
            return read(count);
        }

        public Task Write(byte[] data)
        {
            return write(data);
        }
    }
}
