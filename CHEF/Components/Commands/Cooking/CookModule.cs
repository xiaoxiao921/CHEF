using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private async Task ListRecipesInternal(SocketUser owner = null, int page = 1, string cmdName = null, IUserMessage existingBotMsg = null)
        {
            if (page < 1)
            {
                page = 1;
            }

            List<Recipe> recipes;
            int totalRecipeCount;
            const int descMaxLength = 10;

            using (var context = new RecipeContext())
            {
                (recipes, totalRecipeCount) = await context.GetRecipes(cmdName, page - 1, owner);
            }

            var badRecipes = new List<Recipe>();
            var embedBuilder = new EmbedBuilder();
            foreach (var recipe in recipes)
            {
                var recipeText = recipe.Text;
                var recipeTextLength = recipeText.Length > descMaxLength ? descMaxLength : recipeText.Length;

                try
                {
                    embedBuilder.AddField($"{recipe.Name}", $"{recipeText[..recipeTextLength]}");
                }
                catch (Exception)
                {
                    badRecipes.Add(recipe);
                }
            }

            if (badRecipes.Count > 0)
            {
                using (var context = new RecipeContext())
                {
                    context.RemoveRange(badRecipes);

                    await context.SaveChangesAsync();

                    Logger.Log($"Removed {badRecipes.Count} bad recipes.");
                }
            }

            if (recipes.Count > 0)
            {
                var totalPage = totalRecipeCount / RecipeContext.NumberPerPage +
                                (totalRecipeCount % RecipeContext.NumberPerPage == 0 ? 0 : 1);
                if (totalPage == 0)
                {
                    totalPage += 1;
                }

                var pageStr = $"{page} / {totalPage}";

                // reference https://github.com/djthegr8/RoleX/blob/fb1cd476562df7d0a98d9da1baa31082f904843e/Hermes/Modules/Channel%20Permission/Categorydelete.cs#L41
                var componentBuilder = new ComponentBuilder();
                var buttonBuilders = new List<ButtonBuilder>();

                var guid = Guid.NewGuid();
                var currentPageButton = new ButtonBuilder(pageStr, $"{guid}", ButtonStyle.Secondary, isDisabled: true);

                if (totalPage > 1)
                {
                    var noPreviousPages = page == 1;
                    var noNextPages = page >= totalPage;

                    buttonBuilders.Add(new ButtonBuilder("First", $"{guid}first", isDisabled: noPreviousPages));
                    buttonBuilders.Add(new ButtonBuilder("Previous", $"{guid}previous", isDisabled: noPreviousPages));
                    buttonBuilders.Add(currentPageButton);
                    buttonBuilders.Add(new ButtonBuilder("Next", $"{guid}next", isDisabled: noNextPages));
                    buttonBuilders.Add(new ButtonBuilder("Last", $"{guid}last", isDisabled: noNextPages));
                }
                else
                {
                    buttonBuilders.Add(currentPageButton);
                }

                foreach (var buttonBuilder in buttonBuilders)
                {
                    componentBuilder.WithButton(buttonBuilder);
                }

                var isFiltered = cmdName != null ? $" that contains `{cmdName}` in their name" : "";
                var msgContent = $"Here's a list of recipes{isFiltered}: ";
                if (existingBotMsg == null)
                {
                    existingBotMsg = await ReplyAsync(msgContent, false, embedBuilder.Build(), components: componentBuilder.Build());
                }
                else
                {
                    await existingBotMsg.ModifyAsync(msg =>
                    {
                        msg.Content = msgContent;
                        msg.Embed = embedBuilder.Build();
                        msg.Components = componentBuilder.Build();
                    });
                }

                if (totalPage > 1)
                {
                    var cancelSource = new CancellationTokenSource();
                    const int timeoutInMs = 15000;
                    cancelSource.CancelAfter(timeoutInMs);

                    var interaction = await InteractionHandler.GetNextSocketMessageComponent(Context.Client,
                        m => m.Data.CustomId.Contains(guid.ToString()) && m.User.Id == Context.User.Id, cancelSource.Token);

                    var noInteractionWithAnyButtonsAfterTimeout = interaction == null;
                    if (noInteractionWithAnyButtonsAfterTimeout)
                    {
                        Logger.Log("Recipe listing interaction timed out.");

                        // Disable buttons
                        componentBuilder = new();
                        foreach (var buttonBuilder in buttonBuilders)
                        {
                            buttonBuilder.IsDisabled = true;
                            componentBuilder.WithButton(buttonBuilder);
                        }

                        await existingBotMsg.ModifyAsync(msg =>
                        {
                            msg.Components = componentBuilder.Build();
                        });

                        return;
                    }

                    await interaction.DeferAsync();

                    if (interaction.Data.CustomId.Contains("first"))
                    {
                        page = 1;
                    }
                    else if (interaction.Data.CustomId.Contains("previous"))
                    {
                        page -= 1;
                    }
                    else if (interaction.Data.CustomId.Contains("next"))
                    {
                        page += 1;
                    }
                    else if (interaction.Data.CustomId.Contains("last"))
                    {
                        page = totalPage;
                    }

                    await ListRecipesInternal(owner, page, cmdName, existingBotMsg);
                }
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
            ) => await ListRecipesInternal(null, page, cmdName);

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
            ) => await ListRecipesInternal(ownerName, page, cmdName);

        [Command("new")]
        [Summary
            ("Creates a new command and add it to the cook book.")]
        [Alias("n", "mk", "create")]
        [RequireRole(PermissionLevel.ModCreator)]
        public async Task NewRecipe(
            [Summary("The name to give to the command")]
            string cmdName,
            [Summary("The text that will be printed when the command is called.")]
            [Remainder]
            string text)
        {
            var botAnswer = new StringBuilder("I can't cook ???");

            if (cmdName.Contains(':'))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has `:` in its name.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            if (cmdName.Contains('\n'))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has new line in its name.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            if (cmdName.Contains(' '))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has spaces in its name.");

                await ReplyAsync(botAnswer.ToString());
                return;
            }

            if (string.IsNullOrWhiteSpace(cmdName))
            {
                botAnswer.Clear();
                botAnswer.AppendLine("You can't have a recipe that has no name.");

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
            if (allStaticCmdAliases.Any(alias => cmdName.StartsWith(alias, StringComparison.InvariantCultureIgnoreCase)))
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

            var realOwnerName = Context.User.ToString();
            var isUnderLimit = await CheckRecipeLengthUnderLimitAsync(cmdName, realOwnerName, text);
            if (!isUnderLimit)
            {
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
        [RequireRole(PermissionLevel.ModCreator)]
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

                if (string.IsNullOrWhiteSpace(text))
                {
                    botAnswer.Clear();
                    botAnswer.AppendLine("You can't have a recipe that has no text.");

                    await ReplyAsync(botAnswer.ToString());
                    return;
                }

                var realOwnerName = existingRecipe.RealOwnerName(Context.Guild);
                var isUnderLimit = await CheckRecipeLengthUnderLimitAsync(existingRecipe.Name, realOwnerName, text);
                if (isUnderLimit)
                {
                    existingRecipe.Text = text;
                    await context.SaveChangesAsync();

                    botAnswer.Clear();
                    botAnswer.AppendLine($"Successfully modified the cooking recipe called `{cmdName}`");
                    await ReplyAsync(botAnswer.ToString());
                }
            }
        }

        private async Task<bool> CheckRecipeLengthUnderLimitAsync(string recipeName, string realOwnerName, string newRecipeText)
        {
            var textPreview = $"**Recipe: {recipeName} (Owner: {realOwnerName})**{Environment.NewLine}{newRecipeText}";
            const int textLengthLimit = 2000;
            if (textPreview.Length >= textLengthLimit)
            {
                await ReplyAsync($"The text length when the recipe will be shown will exceed {textLengthLimit} characters. " +
                           $"Currently at {textPreview.Length}. " +
                           "Please reduce the text length.");

                return false;
            }

            return true;
        }

        [Command("delete")]
        [Summary
            ("Deletes an exisiting command from the cook book.")]
        [Alias("d", "del", "rm", "remove")]
        [RequireRole(PermissionLevel.ModCreator)]
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
                    recipesToRemove.Add(forbidden);
                }

                forbiddens = context.Recipes.AsQueryable().Where(r => r.Name.Contains("\n"));

                nbForbidden += forbiddens.Count();

                foreach (var forbidden in forbiddens)
                {
                    recipesToRemove.Add(forbidden);
                }

                forbiddens = context.Recipes.AsQueryable().Where(r => r.Name.Contains(" "));

                nbForbidden += forbiddens.Count();

                foreach (var forbidden in forbiddens)
                {
                    recipesToRemove.Add(forbidden);
                }

                forbiddens = context.Recipes.AsQueryable().Where(r => r.Name.Length == 0);

                nbForbidden += forbiddens.Count();

                foreach (var forbidden in forbiddens)
                {
                    recipesToRemove.Add(forbidden);
                }

                forbiddens = context.Recipes.AsQueryable().Where(r => string.IsNullOrWhiteSpace(r.Name));

                nbForbidden += forbiddens.Count();

                foreach (var forbidden in forbiddens)
                {
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
