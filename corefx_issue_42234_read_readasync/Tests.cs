using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace corefx_issue_42234_read_readasync
{

    public class Tests
    {
        void StartTestServer(Int32 port, IEnumerable<(Int32 timeout, string data)> buffers)
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            var _ = listener.AcceptTcpClientAsync().ContinueWith(async task =>
            {
                var server = task.Result;
                using (var stream = server.GetStream())
                {
                    foreach (var buf in buffers)
                    {
                        await Task.Delay(buf.timeout)/*.ConfigureAwait(false)*/;
                        var data = DataGenerator.ToBytes(buf.data);
                        Log.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} written-before {buf.data}");

                        await stream.WriteAsync(data, 0, data.Length)/*.ConfigureAwait(false)*/;
                        await stream.FlushAsync()/*.ConfigureAwait(false)*/;
                        Log.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} written-after");
                    }
                    await Task.Delay(-1).ConfigureAwait(false);
                }
            }, TaskContinuationOptions.LongRunning);
        }
        Task<Int32> ReadAsync(TextReader reader, Int32 bufferSize)
        {
            return reader.ReadAsync(new char[1023], 0, 1023);
        }
        Int32 Read(TextReader reader, Int32 bufferSize)
        {
            return reader.Read(new char[bufferSize], 0, bufferSize);
        }
        async Task ProcessClientStream(Stream stream, bool useReadFromAsync)
        {
            using (var cts = new System.Threading.CancellationTokenSource())
            {
                cts.CancelAfter(2000);

                cts.Token.Register(() => stream.Close());
                using (var textReader = new StreamReader(new LogStream(stream)))
                {
                    using (var reader = new LogStreamReader(textReader))
                    {
                        var totalRead = 0;
                        totalRead += await ReadAsync(reader, 1023);
                        Log.WriteLine($"totalRead: {totalRead}");

                        totalRead += await ReadAsync(reader, 612);
                        Log.WriteLine($"totalRead: {totalRead}");

                        if (useReadFromAsync)
                        {
                            totalRead += await ReadAsync(reader, 1024);
                            Log.WriteLine($"totalRead: {totalRead}");

                            totalRead += await ReadAsync(reader, 2048);
                            Log.WriteLine($"totalRead: {totalRead}");

                        }
                        else
                        {
                            totalRead += Read(reader, 1024);
                            Log.WriteLine($"totalRead: {totalRead}");

                            totalRead += Read(reader, 2048);
                            Log.WriteLine($"totalRead: {totalRead}");

                        }

                        cts.Token.ThrowIfCancellationRequested();
                    }
                }
            }
        }
        async Task StartClientAndProcessStream(Int32 port, bool useReadFromAsync)
        {
            using (var socket = new TcpClient())
            {
                await socket.ConnectAsync("127.0.0.1", port);
                using (var stream = socket.GetStream())
                {
                    await ProcessClientStream(stream, useReadFromAsync);
                }
            }
        }

        [TestCase(0, 46565, true)]
        [TestCase(5000, 46566, true)]
        [TestCase(0, 46567, false)]
        [TestCase(5000, 46568, false)]
        public async Task ReadAsync_Test(Int32 lastMessageDelay, Int32 port, bool useReadFromAsync)
        {
            var preparedServerData = DataGenerator.Generate(lastMessageDelay);
            StartTestServer(port, preparedServerData);
            await StartClientAndProcessStream(port, useReadFromAsync);
        }
    }
}