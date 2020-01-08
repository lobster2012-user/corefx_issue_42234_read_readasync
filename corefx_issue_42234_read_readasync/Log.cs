using System;

namespace corefx_issue_42234_read_readasync
{
    public static class Log
    {
        private static readonly object SyncRoot = new object();
        public static void WriteLine(String s)
        {
            
            lock (SyncRoot)
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} {s}");
            }
            
        }

        public static string CompressLog(string s)
        {
            for (var i = 0; i <= 5; ++i)
            {
                var search = new string(i.ToString()[0], 5);

                var start = s.IndexOf(search);
                if (start < 0)
                    continue;
                start = s.IndexOf(search, start + 3);
                if (start < 0)
                    continue;
                var end = s.LastIndexOf(search);
                if (end < 0)
                    continue;
                if (start + search.Length >= end + search.Length)
                    continue;

                s = s.Substring(0, start) + "...." + s.Substring(end);
            }
            return s;
        }
    }
}
