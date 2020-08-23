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

        private static void TestSentry()
        {
            using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DNS")))
            {
                throw new Exception("Sentry Test");
            }
        }

        internal static void Log(string msg)
        {
            var log = $"{LogPrefix} {msg}";

            using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DNS")))
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
