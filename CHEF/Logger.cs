using System;
using System.IO;
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
            var fileName = Path.GetFileName(file);
            var log = $"{LogPrefix} [{fileName}:{line} {member}] {msg}";

            using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DSN")))
            {
                Console.WriteLine(log);
                SentrySdk.CaptureMessage(log);
            }
        }
    }
}
