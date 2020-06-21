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

        private async Task ListRecipesInternal(SocketUser owner = null, int page = 1, string cmdName = null)
        {
            if (page < 1)
                page = 1;

            List<Recipe> recipes;
            int totalRecipeCount;
            const int descMaxLength = 10;

            using (var context = new RecipeContext())
            {
                (recipes, totalRecipeCount) = await context.GetRecipes(cmdName, page - 1, owner);
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
                    if (page == 1)
                    {
                        noMatch = owner == null ? 
                            "Oh no ! My recipe book is empty... :(" : 
                            $"No recipes found from an author named {owner}.";
                    }
                    else
                    {
                        noMatch = "Page number is too high.";
                    }
                }
                else
                {
                    noMatch = $"No recipes with {cmdName} in their name ";
                    if (page != 1)
                    {
                        noMatch += $"on page {page}";
                    }
                    if (owner != null)
                    {
                        noMatch += $" from an author named {owner}";
                    }
                }

                await ReplyAsync(noMatch);
            }
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
            await ListRecipesInternal(null, page, cmdName);
        }

        [Command("listuser")]
        [Summary
            ("Prints a list of all availables cooking recipes from a specified user.")]
        [Alias("lu", "lsu")]
        public async Task ListUserRecipes(
            [Summary("Name of the recipe owner.")]
            SocketUser ownerName,
            [Summary("Optional page if multiple pages are returned.")]
            int page = 1,
            [Summary("Optional word to filter the search.")]
            string cmdName = null
            )
        {
            await ListRecipesInternal(ownerName, page, cmdName);
        }

        [Command("new")]
        [Summary
            ("Creates a new command and add it to the cook book.")]
        [Alias("n", "mk", "create")]
        [RequireRole(PermissionLevel.ModDev)]
        public async Task NewRecipe(
            [Summary("The name to give to the command")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            if (text.Contains(':'))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has `:` in its name.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

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
#if DEBUG
                // Testing duplicates
                await context.Recipes.AddAsync(new Recipe
                {
                    Name = cmdName,
                    Text = text,
                    OwnerId = Context.User.Id,
                    OwnerName = Context.User.ToString()
                });
#endif
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
        [RequireRole(PermissionLevel.ModDev)]
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
                    botAnswer.AppendLine("You don't own that recipe / don't have the required permission, you can't change it.");

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
        [RequireRole(PermissionLevel.ModDev)]
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
                    botAnswer.AppendLine("You don't own that recipe / don't have the required permission, you can't delete it.");

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

        [Command("rmd")]
        [Summary
            ("Debug: Removes duplicate / forbidden recipes from the book.")]
        [RequireRole(PermissionLevel.Elevated)]
        public async Task DeleteDuplicateRecipes()
        {
            int nbDuplicate;
            var nameOfRecipesToRemove = new List<string>();
            using (var context = new RecipeContext())
            {
                var duplicates = context.Recipes.AsQueryable().
                    GroupBy(r => new { r.Name }).
                    Where(g => g.Count() > 1).
                    Select(r => r.Key);

                nbDuplicate = duplicates.Count();

                foreach (var duplicate in duplicates)
                {
                    Logger.Log("duplicate recipe : " + duplicate.Name);
                    nameOfRecipesToRemove.Add(duplicate.Name);
                }

                await context.SaveChangesAsync();
            }

            using (var context = new RecipeContext())
            {
                foreach (var rName in nameOfRecipesToRemove)
                {
                    var r = context.GetRecipe(rName).Result;
                    context.Remove(r);
                }

                await context.SaveChangesAsync();
            }
            
            await ReplyAsync($"Successfully deleted `{nbDuplicate}` recipes that were duplicate.");

            int nbForbidden;
            var recipesToRemove = new List<Recipe>();
            using (var context = new RecipeContext())
            {
                var forbiddens = context.Recipes.AsQueryable().Where(r => r.Name.Contains(":"));

                nbForbidden = forbiddens.Count();

                foreach (var forbidden in forbiddens)
                {
                    Logger.Log("forbidden recipe : " + forbidden.Name);
                    recipesToRemove.Add(forbidden);
                }

                await context.SaveChangesAsync();
            }

            using (var context = new RecipeContext())
            {
                context.RemoveRange(recipesToRemove);

                await context.SaveChangesAsync();
            }
            
            await ReplyAsync($"Successfully deleted `{nbForbidden}` recipes that had forbidden characters in them.");
        }
    }
}
