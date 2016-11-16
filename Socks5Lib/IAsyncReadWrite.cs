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
        event EventHandler<int> DataWritten;
        int Unread { get; }
    }

    public class DelegateAsyncReadWrite : IAsyncReadWrite
    {
        private Action finishWrite;
        private Func<int, Task<byte[]>> read;
        private Func<byte[], Task> write;

        public int Unread { get; private set; }

        private EventHandler<int> dataWritten = EmptyActions.EmptyAction;
        public event EventHandler<int> DataWritten
        {
            add { if (dataWritten == EmptyActions.EmptyAction) dataWritten = value; else dataWritten += value; }
            remove { if (dataWritten == value) dataWritten = EmptyActions.EmptyAction; else dataWritten -= value; }
        }

        public DelegateAsyncReadWrite(Action finishWrite, Func<int, Task<byte[]>> read, Func<byte[], Task> write)
        {
            this.finishWrite = finishWrite;
            this.read = read;
            this.write = write;
            Unread = 0;
        }

        public void FinishWrite()
        {
            finishWrite();
            dataWritten(this, -1);
        }

        public Task<byte[]> Read(int count)
        {
            var r = read(count);
            Unread -= count;
            return r;
        }

        public async Task Write(byte[] data)
        {
            await write(data);
        }

        public void RaiseDataWritten(int l)
        {
            Unread += l;
            dataWritten(this, l);
        }
    }
}
