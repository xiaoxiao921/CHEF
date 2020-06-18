using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CHEF
{
    internal static class Logger
    {
        private const string LogPrefix = "[CHEF]";

        private static DiscordSocketClient _client;
        private static IGuildUser _reportToUser;
        
        internal static void Init(DiscordSocketClient client)
        {
            _client = client;
            var guild = _client.GetGuild(562704639141740588);
            _reportToUser = ((IGuild)guild).GetUserAsync(125598628310941697).Result;
        }

        internal static void Log(string msg)
        {
            var log = $"{LogPrefix} {msg}";

            Console.WriteLine(log);

            Task.Run(async () => { await _reportToUser.SendMessageAsync(log); });
        }

        internal static void LogClassInit([CallerFilePath]string filePath = "")
        {
            Log($"Initializing {Path.GetFileNameWithoutExtension(filePath)}");
        }
    }
}
