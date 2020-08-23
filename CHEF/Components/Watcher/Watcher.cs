using System;
using System.Threading.Tasks;
using CHEF.Components.Commands.Ignore;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sentry;

namespace CHEF.Components.Watcher
{
    public class Watcher : Component
    {
        private readonly AutoPastebin _autoPastebin;
        private readonly ImageParser _imageParser;

        public Watcher(DiscordSocketClient client) : base(client)
        {
            _autoPastebin = new AutoPastebin();
            _imageParser = new ImageParser();
        }

        public override async Task SetupAsync()
        {
            using (var context = new IgnoreContext())
            {
                await context.Database.MigrateAsync();
                Logger.Log("Done migrating ignore table.");
            }

            Client.MessageReceived += MsgWatcherAsync;

            await Task.CompletedTask;
        }

        private Task MsgWatcherAsync(SocketMessage msg)
        {
            Task.Run(async () =>
            {
                using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DSN")))
                {
                    var pasteBinRes = await _autoPastebin.Try(msg);

                    if (!string.IsNullOrEmpty(pasteBinRes))
                        await msg.Channel.SendMessageAsync(pasteBinRes);

                    var yandexRes = await _imageParser.Try(msg);

                    if (!string.IsNullOrEmpty(yandexRes))
                        await msg.Channel.SendMessageAsync(yandexRes);

                    if (msg.Content.Contains("can i ask", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await msg.Channel.SendMessageAsync($"{msg.Author.Mention} https://dontasktoask.com/");
                    }
                }
                
            });

            return Task.CompletedTask;
        }
    }
}
