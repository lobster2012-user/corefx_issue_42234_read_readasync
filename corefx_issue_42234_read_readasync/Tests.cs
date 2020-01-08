using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace corefx_issue_42234_read_readasync
{

    public class Tests
    {
        public static void StartTestServer(Int32 port, IEnumerable<(Int32 timeout, string data)> buffers)
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            var _ = listener.AcceptTcpClientAsync().ContinueWith(async task =>
            {
                var server = task.Result;
                using (var stream = server.GetStream())
                {
                    Int32 total = 0;
                    foreach (var buf in buffers)
                    {
                        await Task.Delay(buf.timeout)/*.ConfigureAwait(false)*/;
                        var data = DataGenerator.ToBytes(buf.data);
                        Log.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} written-before {buf.data}");

                        await stream.WriteAsync(data, 0, data.Length)/*.ConfigureAwait(false)*/;
                        await stream.FlushAsync()/*.ConfigureAwait(false)*/;

                        total += data.Length;

                        Log.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} written-after  total={total}");
                    }
                    await Task.Delay(-1).ConfigureAwait(false);
                }
            }, TaskContinuationOptions.LongRunning);
        }
        Task<Int32> ReadAsync(TextReader reader, Int32 bufferSize)
        {
            return reader.ReadAsync(new char[bufferSize], 0, bufferSize);
        }
        Int32 Read(TextReader reader, Int32 bufferSize)
        {
            return reader.Read(new char[bufferSize], 0, bufferSize);
        }
        async Task<Int32> ProcessClientStream(
            Stream stream
            , Int32[] asyncSizes
            , Int32[] syncSizes
            , bool useReadFromAsync
            , Int32 maxReadSize)
        {
            using (var cts = new System.Threading.CancellationTokenSource())
            {
                cts.CancelAfter(2000);
                try
                {
                    cts.Token.Register(() => stream.Close());
                    using (var textReader = new StreamReader(new LogStream(stream)))
                    {
                        using (var reader = new LogStreamReader(textReader))
                        {
                            var totalRead = 0;
                            Int32 i = 0;
                            foreach (var size in asyncSizes)
                            {
                                totalRead += await ReadAsync(reader, size);
                                ++i;
                                Log.WriteLine($"totalRead-async[{i}]: {totalRead}");
                                if (totalRead >= maxReadSize)
                                    return totalRead;
                            }

                            if (useReadFromAsync)
                            {
                                foreach (var size in syncSizes)
                                {
                                    totalRead += await ReadAsync(reader, size);
                                    ++i;
                                    Log.WriteLine($"totalRead-async[{i}]: {totalRead}");
                                    if (totalRead >= maxReadSize)
                                        return totalRead;
                                }
                            }
                            else
                            {
                                foreach (var size in syncSizes)
                                {
                                    totalRead += Read(reader, size);
                                    ++i;
                                    Log.WriteLine($"totalRead-sync[{i}]: {totalRead}");
                                    if (totalRead >= maxReadSize)
                                        return totalRead;
                                }
                            }

                            return totalRead;
                        }
                    }
                }
                finally
                {
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }
        async Task<Int32> StartClientAndProcessStream(
              Int32 port
            , Int32[] asyncSizes
            , Int32[] syncSizes
            , bool useReadFromAsync
            , Int32 maxReadSize)
        {
            using (var socket = new TcpClient())
            {
                await socket.ConnectAsync("127.0.0.1", port);
                using (var stream = socket.GetStream())
                {
                    return await ProcessClientStream(
                          stream: stream
                        , asyncSizes: asyncSizes
                        , syncSizes: syncSizes
                        , useReadFromAsync: useReadFromAsync
                        , maxReadSize: maxReadSize);
                }
            }
        }

        [TestCase(0, 42565, true)]
        [TestCase(5000, 42566, true)]
        [TestCase(0, 42567, false)]
        [TestCase(5000, 42568, false)]
        public async Task ReadAsync_Test(Int32 lastMessageDelay, Int32 port, bool useReadFromAsync)
        {
            var preparedServerData = DataGenerator.Generate(lastMessageDelay).ToArray();
            var preparedTotalSize = preparedServerData.Sum(z => z.data.Length);

            var expectedMinimumSize = preparedServerData
                           .SkipLast(1)
                           .Sum(z => z.data.Length);

            Log.WriteLine($"PreparedTotalSize = {preparedTotalSize}");
            Log.WriteLine($"expectedMinimumSize = {expectedMinimumSize}");

            foreach (var size in preparedServerData.Select(z => z.data.Length))
            {
                Log.WriteLine($"chunk.size = {size}");
            }

            var asyncSizes = new[] { 1023, 612 };
            var syncSizes = new[] { 1024, 2048 };

            StartTestServer(port, preparedServerData);

            var totalRead = await StartClientAndProcessStream(
                  port: port,
                  asyncSizes: asyncSizes
                , syncSizes: syncSizes
                , useReadFromAsync: useReadFromAsync
                , maxReadSize: expectedMinimumSize);

           

            Assert.LessOrEqual(expectedMinimumSize, totalRead);
        }
    }
}