using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace corefx_issue_42234_read_readasync
{
    public class LogStream : Stream
    {
        private readonly Stream _stream;

        public LogStream(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override bool CanRead => true;

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            Log.WriteLine($"Stream::read-begin {count}");
            var read = _stream.Read(buffer, index, count);
            Log.WriteLine($"Stream::read-end {read}/{count} '{Log.CompressLog(System.Text.Encoding.UTF8.GetString(buffer, index, read))}'");            
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            _stream.Close();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
        }
    }
}
