using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace corefx_issue_42234_read_readasync
{
    public class LogStreamReader : TextReader
    {
        private readonly TextReader _reader;

        public LogStreamReader(TextReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public override void Close()
        {
            _reader.Close();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
        }
        public override int Read(char[] buffer, int index, int count)
        {
            Log.WriteLine($"TextReader::read-begin {count}");
            var read = _reader.Read(buffer, index, count);
            Log.WriteLine($"TextReader::read-end {read}/{count} '{Log.CompressLog(new string(buffer, index, read))}'");

            return read;
        }
        public override async Task<int> ReadAsync(char[] buffer, int index, int count)
        {

            Log.WriteLine($"TextReader::readasync-begin {count}");

            var read = await _reader.ReadAsync(buffer, index, count);

            Log.WriteLine($"TextReader::readasync-end {read}/{count} '{Log.CompressLog(new string(buffer, index, read))}'");

            return read;
        }

    }
}
