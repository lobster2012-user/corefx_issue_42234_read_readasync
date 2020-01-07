using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace corefx_issue_42234_read_readasync
{
    public static class DataGenerator
    {
        public static byte[] ToBytes(string s)
        {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }
        static string ToString(object o)
        {
            return JsonConvert.SerializeObject(o);
        }
        static object Create(char fill, Int32 count)
        {
            return new
            {
                Prop = new string(fill, count)
            };
        }
        static string CreateString(char fill, Int32 msgSize, Int32 count)
        {
            string s = "";
            for (var i = 0; i < count; ++i)
            {
                s += ToString(Create((char)(fill + i), msgSize));
            }
            return s;
        }

        public static IEnumerable<(Int32 timeout, string data)> Generate(Int32 lastMessageDelay)
        {
            var buffers = new List<(Int32 timeout, string data)>();

            var msg1 = CreateString('1', 400, 1);
            buffers.Add((0, (msg1)));

            var msg2 = CreateString('2', 400, 3);
            buffers.Add((0, (msg2)));

            var msg3 = CreateString('5', 400, 1);
            buffers.Add((0, (msg3)));

            var msg4 = CreateString('8', 400, 1);
            buffers.Add((lastMessageDelay, (msg4)));

            return buffers;
        }
    }
}