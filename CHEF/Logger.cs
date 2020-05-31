using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CHEF
{
    internal static class Logger
    {
        private const string LogPrefix = "[CHEF]";

        internal static void Log(string msg)
        {
            Console.WriteLine($"{LogPrefix} {msg}");
        }

        internal static void LogClassInit([CallerFilePath]string filePath = "")
        {
            Log($"Initializing {Path.GetFileNameWithoutExtension(filePath)}");
        }
    }
}
