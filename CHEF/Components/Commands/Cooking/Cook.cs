using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CHEF.Components.Commands.Cooking
{
    public class Cook : Component
    {
        public Cook(DiscordSocketClient client) : base(client)
        {

        }

        public override async Task SetupAsync()
        {
            using (var context = new RecipeContext())
            {
                await context.Database.MigrateAsync();
                Logger.Log("Done migrating db.");
            }

            Client.MessageReceived += RecipeShortcutAsync;
        }

        private Task RecipeShortcutAsync(SocketMessage msg)
        {
            Task.Run(async () =>
            {
                if (!(msg is SocketUserMessage message))
                    return;

                var argPos = 0;
                if (!(message.HasCharPrefix('!', ref argPos) ||
                      message.HasMentionPrefix(Client.CurrentUser, ref argPos)) ||
                    message.Author.IsBot)
                    return;

                var cmdName = message.Content.Substring(1);

                using (var context = new RecipeContext())
                {
                    var recipe = await context.Recipes.AsAsyncEnumerable()
                        .FirstOrDefaultAsync(r => r.Name.ToLower().Equals(cmdName.ToLower()));
                    if (recipe != null)
                    {
                        await msg.Channel.SendMessageAsync(
                            $"**Recipe: {recipe.Name} (Owner: {recipe.OwnerName})**{Environment.NewLine}{recipe.Text}");
                    }
                }
            });

            return Task.CompletedTask;
        }
    }
}
