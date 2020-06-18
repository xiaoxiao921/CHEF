using System;
using System.Threading.Tasks;
using CHEF.Components;
using Discord;
using Discord.WebSocket;

namespace CHEF
{
    public class CHEF
    {
        private DiscordSocketClient _client;

        public static void Main(string[] args)
            => new CHEF().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            await SetupBotLogin();

            await ComponentHandler.Init(_client);

            _client.Ready += () =>
            {
                Logger.Init(_client);
                Database.Init();
                return Task.CompletedTask;
            };

            await Task.Delay(-1);
        }

        private async Task SetupBotLogin()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            _client = new DiscordSocketClient(config);
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await _client.StartAsync();
        }

        private static Task Log(LogMessage msg)
        {
            Logger.Log(msg.ToString());
            return Task.CompletedTask;
        }
    }
}