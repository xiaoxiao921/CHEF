using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands.Cooking
{
    [Group("cooking")]
    [Alias("cook", "c")]
    public class CookModule : ModuleBase<SocketCommandContext>
    {
        // May want to retrieve this dynamically instead
        public static readonly string[] MultBotCommands = { "issue", "deprecate" };

        [Command("recipe")]
        [Summary
            ("Retrieves a command from the cook book.")]
        [Alias("r")]
        public async Task GetRecipe(
            [Summary("The name of the recipe you want to call.")]
            string cmdName)
        {
            Recipe recipe;
            using (var context = new RecipeContext())
            {
                recipe = await context.GetRecipe(cmdName);
            }

            var botAnswer = recipe == null
                ? "Could not find a recipe with that name."
                : $"**Recipe: {recipe.Name} (Owner: {recipe.RealOwnerName(Context.Guild)})**{Environment.NewLine}{recipe.Text}";
            
            await ReplyAsync(botAnswer);
        }

        [Command("list")]
        [Summary
            ("Prints a list of all availables cooking recipes.")]
        [Alias("l", "ls")]
        public async Task ListRecipes(
            [Summary("Optional page if multiple pages are returned.")]
            int page = 1,
            [Summary("Optional word to filter the search.")]
            string cmdName = null
            )
        {
            if (page < 1)
                page = 1;

            List<Recipe> recipes;
            int totalRecipeCount;
            const int descMaxLength = 10;

            using (var context = new RecipeContext())
            {
                recipes = await context.GetRecipes(cmdName, page - 1);
                totalRecipeCount = recipes.Count;
            }
            var embedBuilder = new EmbedBuilder();
            foreach (var recipe in recipes)
            {
                var recipeText = recipe.Text;
                var recipeTextLength = recipeText.Length > descMaxLength ? descMaxLength : recipeText.Length;
                embedBuilder.AddField($"{recipe.Name}", $"{recipeText.Substring(0, recipeTextLength)}");
            }

            if (recipes.Count > 0)
            {
                var totalPage = totalRecipeCount / RecipeContext.NumberPerPage +
                                (totalRecipeCount % RecipeContext.NumberPerPage == 0 ? 0 : 1);
                if (totalPage == 0)
                    totalPage += 1;

                var pageStr = $" *(Page:{page}/{totalPage})*";
                var isFiltered = cmdName != null ? $" that contains `{cmdName}` in their name" : "";
                await ReplyAsync($"Here's a list of recipes{isFiltered}{pageStr} : ", false, embedBuilder.Build());
            }
            else
            {
                string noMatch;
                if (cmdName == null)
                {
                    noMatch = "Oh no ! My recipe book is empty... :(";
                }
                else
                {
                    noMatch = $"No recipes with {cmdName} in their name ";
                    if (page != 1)
                    {
                        noMatch += $"on page {page}";
                    }
                }

                await ReplyAsync(noMatch);
            }
        }

        [Command("new")]
        [Summary
            ("Creates a new command and add it to the cook book.")]
        [Alias("n", "mk", "create")]
        [RequireRole(Roles.ModDeveloper)]
        public async Task NewRecipe(
            [Summary("The name to give to the command")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            Recipe existingRecipe;
            using (var context = new RecipeContext())
            {
                existingRecipe = await context.GetRecipe(cmdName);
            }
            if (existingRecipe != null)
            {
                var ownerName = existingRecipe.RealOwnerName(Context.Guild);

                botAnswer.Clear();
                botAnswer.AppendLine($"A recipe called `{cmdName}` already exists in the cook book, the owner is {ownerName}.");
                var canEdit = existingRecipe.CanEdit((SocketGuildUser)Context.User)
                    ? $"You can modify it by using `!c e {cmdName} <new_text>`"
                    : $"You can't modify that cooking recipe. You aren't {ownerName} !";
                botAnswer.AppendLine(canEdit);

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            var allStaticCmdAliases = CommandHandler.Service.Commands.SelectMany(cmdInfo => cmdInfo.Aliases).ToList();
            allStaticCmdAliases.AddRange(MultBotCommands);
            if (allStaticCmdAliases.Any(alias => alias.Equals(cmdName, StringComparison.InvariantCultureIgnoreCase)))
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

            using (var context = new RecipeContext())
            {
                await context.Recipes.AddAsync(new Recipe
                {
                    Name = cmdName,
                    Text = text,
                    OwnerId = Context.User.Id,
                    OwnerName = Context.User.ToString()
                });
                await context.SaveChangesAsync();
            }

            botAnswer.Clear();
            botAnswer.AppendLine($"Successfully added a new cooking recipe called `{cmdName}`");
            await ReplyAsync(botAnswer.ToString());
        }

        [Command("edit")]
        [Summary
            ("Edits a command that already exists in the cook book.")]
        [Alias("e")]
        [RequireRole(Roles.ModDeveloper)]
        public async Task EditRecipe(
            [Summary("The name of the command to edit")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            using (var context = new RecipeContext())
            {
                var existingRecipe = await context.GetRecipe(cmdName);

                if (existingRecipe == null)
                {
                    botAnswer.Clear();
                    botAnswer.AppendLine($"No recipe called `{cmdName}` exists in my cook book.");

                    await ReplyAsync(botAnswer.ToString());
                    return;
                }

                var gUser = (SocketGuildUser)Context.User;
                if (!existingRecipe.CanEdit(gUser))
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

                existingRecipe.Text = text;
                await context.SaveChangesAsync();
            }
            

            botAnswer.Clear();
            botAnswer.AppendLine($"Successfully modified the cooking recipe called `{cmdName}`");
            await ReplyAsync(botAnswer.ToString());
        }

        [Command("delete")]
        [Summary
            ("Deletes an exisiting command from the cook book.")]
        [Alias("d", "del", "rm", "remove")]
        [RequireRole(Roles.ModDeveloper)]
        public async Task DeleteRecipe(
            [Summary("The name of the command to delete")]
            string cmdName)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            using (var context = new RecipeContext())
            {
                var existingRecipe = await context.GetRecipe(cmdName);

                if (existingRecipe == null)
                {
                    botAnswer.Clear();
                    botAnswer.AppendLine($"No recipe called `{cmdName}` exists in my cooking recipe database.");

                    await ReplyAsync(botAnswer.ToString());
                    return;
                }

                var gUser = (SocketGuildUser)Context.User;
                if (!existingRecipe.CanEdit(gUser))
                {
                    botAnswer.Clear();
                    botAnswer.AppendLine("You don't own that recipe / are not atleast a core dev, you can't delete it.");

                    await ReplyAsync(botAnswer.ToString());
                    return;
                }

                context.Remove(existingRecipe);
                await context.SaveChangesAsync();
            }
            

            botAnswer.Clear();
            botAnswer.AppendLine($"Successfully deleted the cooking recipe called `{cmdName}`");
            await ReplyAsync(botAnswer.ToString());
        }
    }
}
