using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Sentry;

namespace CHEF
{
    internal static class Logger
    {
        private const string LogPrefix = "[CHEF]";

        internal static void Log(
            object msg,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0
        )
        {
            var parts = file.Replace('\\', '/').Split('/');
            var location = string.Join("/", parts.Skip(Math.Max(0, parts.Length - 3)));
            var log = $"{LogPrefix} [{location}:{line} {member}] {msg}";

            Console.WriteLine(log);
            SentrySdk.CaptureMessage(log);
        }
    }
}
