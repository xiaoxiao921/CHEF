using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public class Cook : Component
    {
        public Cook(DiscordSocketClient client) : base(client)
        {

        }

        public override async Task SetupAsync()
        {
            await CheckRecipeTable();
            Client.MessageReceived += RecipeShortcutAsync;
        }

        private static async Task CheckRecipeTable()
        {
            const string sql = @"CREATE TABLE IF NOT EXISTS CookCommands(name VARCHAR(255), text TEXT, owner_id VARCHAR(255), owner_name VARCHAR(255))";

            await Database.AsyncNonQuery(sql);
        }

        private async Task RecipeShortcutAsync(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message))
                return;

            var argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(Client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var cmd = message.Content.Substring(1);

            var recipe = await CookModule.InternalGetRecipe(cmd);

            if (recipe != null)
            {
                await msg.Channel.SendMessageAsync(
                    $"**Recipe: {recipe["name"]} (Owner: {recipe["owner_name"]})**{Environment.NewLine}{recipe["text"]}");
            }
        }
    }

    [Group("cooking")]
    [Alias("cook", "c")]
    public class CookModule : ModuleBase<SocketCommandContext>
    {
        [Command("recipe")]
        [Summary
            ("Retrieves a command from the cook book.")]
        [Alias("r")]
        public async Task GetRecipe(
            [Summary("The name of the recipe you want to call.")]
            string cmdName)
        {
            var recipe = await InternalGetRecipe(cmdName);
            var msg = recipe == null ? "Could not find a recipe with that name." : 
                $"**Recipe: {recipe["name"]} (Owner: {recipe["owner_name"]})**{Environment.NewLine}{recipe["text"]}";

            await ReplyAsync(msg);
        }

        [Command("list")]
        [Summary
            ("Prints a list of all availables cooking recipes.")]
        [Alias("l", "ls")]
        public async Task ListRecipes(
            [Summary("Optional name to filter the search.")]
            string cmdName = null)
        {
            var recipes = await InternalGetRecipes(cmdName+"%");
            var embedBuilder = new EmbedBuilder();
            foreach (DataRow recipeRow in recipes)
            {
                var recipeText = recipeRow["text"].ToString();
                var recipeTextLength = recipeText.Length > 20 ? 20 : recipeText.Length;
                embedBuilder.AddField($"{recipeRow["name"]}", $"{recipeText.Substring(0, recipeTextLength)}");
            }

            if (recipes.Count > 0)
            {
                await ReplyAsync("Here's a list of recipes with their authors: ", false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync("Oh no ! My recipe book is empty... :(");
            }
        }

        [Command("new")]
        [Summary
            ("Creates a new command and add it to the cook book.")]
        [Alias("n", "mk", "create")]
        [RequireRole("mod developer")]
        public async Task NewRecipe(
            [Summary("The name to give to the command")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            var existing = await InternalGetRecipe(cmdName);
            if (existing != null)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"A recipe called `{cmdName}` already exists in the cook book, the owner is {existing["owner_name"]}.");
                var canModify = Context.User.Id.ToString().Equals(existing["owner_id"].ToString())
                    ? $"You can modify it by using !c e `{cmdName}` <new_text>"
                    : $"You can't modify that cooking recipe. You aren't {existing["owner_name"]} !";
                botAnswer.AppendLine(canModify);

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            var allStaticCmdAliases = CommandHandler.Service.Commands.SelectMany(cmdInfo => cmdInfo.Aliases);

            if (allStaticCmdAliases.Any(alias => alias.Equals(cmdName)))
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"A static command called `{cmdName}` already exists, you can't have a recipe called the same as one of those.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            if (text.Equals(string.Empty))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has no text.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            var ownerId = Context.User.Id.ToString();
            var ownerName = Context.User.ToString();
            const string sql = @"INSERT INTO CookCommands VALUES
                                 (
                                    (@name), (@text), (@owner_id), (@owner_name)
                                 )";

            var param = new Dictionary<string, string>
            {
                {"name", cmdName},
                {"text", text},
                {"owner_id", ownerId},
                {"owner_name", ownerName},
            };

            var res = await Database.AsyncNonQuery(sql, param);
            if (res == 1)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"Successfully added a new cooking recipe called `{cmdName}`");
            }
            await ReplyAsync(botAnswer.ToString());
        }

        [Command("edit")]
        [Summary
            ("Edits a command that already exists in cook book")]
        [Alias("e")]
        [RequireRole("mod developer")]
        public async Task EditRecipe(
            [Summary("The name of the command to edit")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            var existing = await InternalGetRecipe(cmdName);
            if (existing == null)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"No recipe called `{cmdName}` exists in my cook book.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            var gUser = (SocketGuildUser)Context.User;
            if (!Context.User.Id.ToString().Equals(existing["owner_id"].ToString()) && !gUser.Roles.Any(role => role.Name.Contains("core developer")))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You don't own that recipe / are not atleast a core dev, you can't change it.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            if (text.Equals(string.Empty))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has no text.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }
            
            const string sql = @"UPDATE CookCommands
                                 SET text = (@text)
                                 WHERE name = (@name)";

            var param = new Dictionary<string, string>
            {
                {"name", cmdName},
                {"text", text}
            };

            var res = await Database.AsyncNonQuery(sql, param);
            if (res == 1)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"Successfully modified the cooking recipe called `{cmdName}`");
            }
            await ReplyAsync(botAnswer.ToString());
        }

        [Command("delete")]
        [Summary
            ("Deletes an exisiting command from the cook book.")]
        [Alias("d", "del", "rm", "remove")]
        [RequireRole("mod developer")]
        public async Task DeleteRecipe(
            [Summary("The name of the command to delete")]
            string cmdName)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            var existing = await InternalGetRecipe(cmdName);
            if (existing == null)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"No recipe called `{cmdName}` exists in my cooking recipe database.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            var gUser = (SocketGuildUser) Context.User;
            if (!Context.User.Id.ToString().Equals(existing["owner_id"].ToString()) && !gUser.Roles.Any(role => role.Name.Contains("core developer")))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You don't own that recipe / are not atleast a core dev, you can't delete it.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            const string sql = @"DELETE FROM CookCommands
                                 WHERE name = (@name)";

            var param = new Dictionary<string, string>
            {
                {"name", cmdName}
            };

            var res = await Database.AsyncNonQuery(sql, param);
            if (res == 1)
            {
                botAnswer.Clear();
                botAnswer.AppendLine($"Successfully deleted the cooking recipe called `{cmdName}`");
            }
            await ReplyAsync(botAnswer.ToString());
        }

        internal static async Task<DataRow> InternalGetRecipe(string cmdName)
        {
            const string sql = @"SELECT *
                                 FROM CookCommands
                                 WHERE name=(@name)";

            var param = new Dictionary<string, string>
            {
                {"name", cmdName}
            };

            var res = await Database.AsyncQuery(sql, param);
            return res.Count > 0 ? res[0] : null;
        }

        internal static async Task<DataRowCollection> InternalGetRecipes(string cmdName = null)
        {
            var sql = @"SELECT name, text
                        FROM CookCommands";
            Dictionary<string, string> param = null;
            if (cmdName != null)
            {
                sql += " WHERE name LIKE (@name)";
                param = new Dictionary<string, string>
                {
                    {"name", cmdName}
                };
            }

            return await Database.AsyncQuery(sql, param);
        }
    }
}
