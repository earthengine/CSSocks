﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace Socks5Lib
{
    public interface IMemoryBuffer {
        Task<byte[]> ReadExactAsync(int count);
    }
    public class MemoryBuffer : Stream, IMemoryBuffer
    {
        #region private fields
        private MemoryStream readMs = new MemoryStream();
        private MemoryStream writeMs = new MemoryStream();
        private TaskCompletionSource<long> dataWrote = new TaskCompletionSource<long>();
        private bool nomorewrite = false;
        #endregion

        #region properties
        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        #endregion

        #region unsuportted operations
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        public override long Position
        {
            get { throw new NotSupportedException(); }
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
        #endregion

        #region overridden methods
        public override void Flush()
        {
            lock (readMs)
            lock(writeMs)
            { 
                readMs.Flush();
                writeMs.Flush();
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (readMs)
            {
                var result = readMs.Read(buffer, offset, count);
                if (result < count)
                {
                    lock (writeMs)
                    {
                        var m = writeMs;
                        writeMs = readMs;
                        readMs = m;
                    }
                    readMs.Seek(0, SeekOrigin.Begin);
                    result = readMs.Read(buffer, offset + result, count - result) + result;
                    if (result < count && !nomorewrite) dataWrote = new TaskCompletionSource<long>();
                }
                return result;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (writeMs)
            {
                if (nomorewrite) throw new InvalidOperationException("No more write");
                writeMs.Write(buffer, offset, count);
                dataWrote.TrySetResult(count);
            }
        }
        #endregion

        #region additional methods
        private async Task ReadExactAsync(byte[] data, int offset, int count)
        {
            var s = Read(data, offset, count);
            if (s < count)
            {
                await dataWrote.Task;
                await ReadExactAsync(data, offset + s, count - s);
            }
        }
        public async Task<byte[]> ReadExactAsync(int count)
        {
            var data = new byte[count];
            await ReadExactAsync(data, 0, count);
            return data;
        }
        public void FinishWrite()
        {
            nomorewrite = true;
            dataWrote.TrySetResult(0);
        }
        #endregion
    }
}