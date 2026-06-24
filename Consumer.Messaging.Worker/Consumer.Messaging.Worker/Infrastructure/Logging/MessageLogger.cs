using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Consumer.Messaging.Worker.Infrastructure.Logging
{
    public static class MessageLogger
    {
        private static long _counter = 0;
        private static readonly object _lock = new();

        public static long NextSeq()
        {
            lock (_lock) { return ++_counter; }
        }

        public static void Log(long? seq, string ev, Dictionary<string, object?>? fields = null)
        {
            var actualSeq = (seq is null || seq == 0) ? NextSeq() : seq.Value;
            var ts = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            if (fields != null)
            {
                foreach (var kv in fields)
                {
                    if (kv.Value == null) continue;
                    var s = kv.Value is string str
                        ? "\"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
                        : kv.Value.ToString();
                    sb.Append(' ').Append(kv.Key).Append('=').Append(s);
                }
            }
            var line = $"[consumer] {ts} [#{actualSeq}] {ev}{sb}";
            Console.WriteLine(line);
        }
    }
}
