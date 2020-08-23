using System;
using System.IO;
using System.Runtime.CompilerServices;
using Sentry;

namespace CHEF
{
    internal static class Logger
    {
        private const string LogPrefix = "[CHEF]";

        internal static void Init()
        {
            Log("Logger Init");
        }

        internal static void Log(string msg)
        {
            var log = $"{LogPrefix} {msg}";

            using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DSN")))
            {
                Console.WriteLine(log);
                SentrySdk.CaptureMessage(log);
            }
        }

        internal static void LogClassInit([CallerFilePath]string filePath = "")
        {
            Log($"Initializing {Path.GetFileNameWithoutExtension(filePath)}");
        }
    }
}
