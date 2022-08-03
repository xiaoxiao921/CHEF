using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CHEF.Components.Commands.Ignore;
using CHEF.Extensions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sentry;
using CHEF.Components.Watcher.Spam;

namespace CHEF.Components.Watcher
{
    public class Watcher : Component
    {
        private readonly AutoPastebin _autoPastebin;
        private readonly ImageParser _imageParser;
        private readonly SpamFilter _spamFilter;

        public Watcher(DiscordSocketClient client) : base(client)
        {
            _autoPastebin = new AutoPastebin("https://hastebin.com/", "https://www.toptal.com/developers/hastebin/", "https://r2modman-hastebin.herokuapp.com/");
            _imageParser = new ImageParser();
            _spamFilter = new SpamFilter();
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
                using (SentrySdk.Init(Environment.GetEnvironmentVariable("SENTRY_DSN")))
                {
                    if (await _spamFilter.Try(msg))
                    {
                        return;
                    }

                    var pasteBinRes = await _autoPastebin.Try(msg);

                    if (!string.IsNullOrEmpty(pasteBinRes))
                        await msg.Channel.SendMessageAsync(pasteBinRes);

                    var yandexRes = await _imageParser.Try(msg);//.WithTimeout(TimeSpan.FromSeconds(10), CancellationToken.None);

                    if (!string.IsNullOrEmpty(yandexRes))
                        await msg.Channel.SendMessageAsync(yandexRes);

                    if (msg.Content.Length < 26 && ContainsAny(msg.Content, "can i ask", "can someone help", "can anyone help"))
                    {
                        await msg.Channel.SendMessageAsync($"{msg.Author.Mention} https://dontasktoask.com/");
                    }
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
