using System;
using System.Linq;
using System.Threading.Tasks;
using CHEF.Components.Commands.Ignore;
using CHEF.Components.Watcher.Spam;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sentry;

namespace CHEF.Components.Watcher
{
    public class Watcher : Component
    {
        private readonly SpamFilter _spamFilter;
        private readonly LogScan _logScan;

        public Watcher(DiscordSocketClient client) : base(client)
        {
            _spamFilter = new SpamFilter();
            _logScan = new LogScan();
        }

        public override async Task SetupAsync()
        {
            using (var context = new IgnoreContext())
            {
                await context.Database.MigrateAsync();
            }
            using (var context = new SpamFilterContext())
            {
                await context.Database.MigrateAsync();
            }

            Client.MessageReceived += MsgWatcherAsync;

            await Task.CompletedTask;
        }

        private Task MsgWatcherAsync(SocketMessage msg)
        {
            Task.Run(async () =>
            {
                try
                {
                        if (await _spamFilter.Try(msg))
                        {
                            return;
                        }

                        var logScanRes = await _logScan.Try(msg);

                        if (!string.IsNullOrEmpty(logScanRes))
                        {
                            var embed = new EmbedBuilder()
                                .WithTitle("Log Scan Report")
                                .WithColor(Color.DarkOrange);

                            embed.Description = logScanRes.Length <= 4096
                                ? logScanRes
                                : logScanRes[..4093] + "...";

                            await msg.Channel.SendMessageAsync(embed: embed.Build(), messageReference: new MessageReference(msg.Id));
                        }
                        else if (msg.Content.Length < 26 && ContainsAny(msg.Content, "can i ask", "can someone help", "can anyone help"))
                        {
                            await msg.Channel.SendMessageAsync(text: $"https://dontasktoask.com/", messageReference: new MessageReference(msg.Id));
                        }
                    }
                catch (Exception e)
                {
                    Logger.Log(e);
                }
            });

            return Task.CompletedTask;
        }

        private static bool ContainsAny(string text, params string[] testStrings)
        {
            return testStrings.Any(testStr => text.Contains(testStr, StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool ContainsAll(string text, params string[] testStrings)
        {
            return testStrings.All(testStr => text.Contains(testStr, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
