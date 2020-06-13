using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task Help()
        {
            var commands = CommandHandler.Service.Commands.ToList();
            var embedBuilder = new EmbedBuilder();

            commands.Remove(commands.Single(command => command.Name.Equals("help")));

            foreach (CommandInfo command in commands)
            {
                var embedFieldText = command.Summary ?? "No description available\n";
                var sb = new StringBuilder();
                foreach (var alias in command.Aliases)
                {
                    sb.Append(alias);
                    sb.Append(" / ");
                }
                embedBuilder.AddField(sb.ToString(), embedFieldText);
            }

            await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
        }

        [Command("userinfo")]
        [Summary
            ("Returns info about the user who called this command, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfo(
            [Summary("The (optional) user to get info from")]
            SocketUser user = null)
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Orange);

            var socketUser = user ?? Context.User;
            var userInfo = (SocketGuildUser)socketUser;

            embedBuilder.WithAuthor(author => author.Build());
            embedBuilder.Author.WithName($"{userInfo.Username}#{userInfo.Discriminator}");
            embedBuilder.Author.WithIconUrl(userInfo.GetAvatarUrl());

            embedBuilder.AddField($"{"" + '\u200B'}", $"{userInfo.Mention}");

            embedBuilder.AddField("Joined",
                // ReSharper disable once PossibleInvalidOperationException
                userInfo.JoinedAt.Value.Date.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture), true);
            embedBuilder.AddField("Registered",
                userInfo.CreatedAt.Date.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture), true);
            embedBuilder.WithFooter($"ID: {userInfo.Id}");

            var roles = new StringBuilder();
            foreach (var role in userInfo.Roles)
            {
                if (!role.IsEveryone)
                    roles.Append(role.Mention);
            }
            if (roles.Length > 1)
                embedBuilder.AddField("Roles", roles);

            await ReplyAsync("", false, embedBuilder.Build());
        }

        [Command("modinfo")]
        [Summary
            ("Returns info about a mod that is uploaded on thunderstore.io")]
        [Alias("mod")]
        public async Task ModInfo(
            [Summary("The mod to get info from")]
            string modName)
        {
            var modInfo = await Thunderstore.GetModInfo(modName);

            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(modInfo.IsDeprecated ? Color.Red : Color.Green);

            embedBuilder.WithAuthor(modInfo.Owner);
            embedBuilder.WithTitle($"{modInfo.Name} v{modInfo.LatestPackage.VersionNumber}");
            embedBuilder.WithUrl(modInfo.PackageUrl.AbsoluteUri);
            embedBuilder.WithDescription(modInfo.LatestPackage.Description);
            embedBuilder.WithThumbnailUrl(modInfo.LatestPackage.Icon.AbsoluteUri);

            if (modInfo.IsDeprecated)
            {
                embedBuilder.AddField("DEPRECATED", "This mod is deprecated, it may not work correctly.");
            }

            embedBuilder.AddField("Rating Score", modInfo.RatingScore, true);
            embedBuilder.AddField("Total downloads", modInfo.TotalDownloads, true);

            await ReplyAsync("", false, embedBuilder.Build());
        }

        [Command("wiki")]
        [Summary
            ("Returns info about a mod that is uploaded on thunderstore.io")]
        public async Task WikiSearch(
            [Remainder] string search)
        {
            search = search.Trim();
            var encoded = WebUtility.UrlEncode(search);
            var url = $"https://github.com/risk-of-thunder/R2Wiki/search?q={encoded}&type=Wikis";

            using (var page = await HeadlessBrowser.Chromium.NewPageAsync())
            {
                await page.GoToAsync(url);

                var wikiResultsParent = await page.QuerySelectorAsync("#wiki_search_results");
                if (wikiResultsParent != null)
                {
                    var nbOfResultQuery = await page.QuerySelectorAsync(
                        "div.d-flex.flex-column.flex-md-row.flex-justify-between.border-bottom.pb-3.position-relative h3");
                    var nbOfResult =
                        await nbOfResultQuery.EvaluateFunctionAsync<int>(
                            "el => el.childNodes[0].nodeValue = el.childNodes[0].nodeValue.replace(/\\D/g, '')");

                    var wikiResult = await wikiResultsParent.QuerySelectorAsync(".hx_hit-wiki.py-4.border-top");

                    const string githubUrl = "https://github.com";

                    var aElement = await wikiResult.QuerySelectorAsync("a");
                    var resultTitle = await aElement.EvaluateFunctionAsync<string>("el => el.getAttribute('title')");
                    var resultLink =
                        githubUrl + await aElement.EvaluateFunctionAsync<string>("el => el.getAttribute('href')");

                    var resultDescQuery = await wikiResult.QuerySelectorAsync("p.mb-1.width-full");
                    var resultDesc = await resultDescQuery.EvaluateFunctionAsync<string>("el => el.innerText");

                    var lastUpdatedQuery = await wikiResult.QuerySelectorAsync("relative-time");
                    var lastUpdated = await lastUpdatedQuery.EvaluateFunctionAsync<string>("el => el.innerText");

                    var embedBuilder = new EmbedBuilder();
                    embedBuilder.WithColor(Color.Orange);

                    embedBuilder.WithAuthor("R2Wiki", "https://avatars1.githubusercontent.com/u/49210367", resultLink);
                    embedBuilder.WithTitle(resultTitle);
                    embedBuilder.WithUrl(resultLink);
                    embedBuilder.WithDescription($"{resultDesc}\n\n*This page was last updated {lastUpdated}*");

                    embedBuilder.AddField($"Other results ({nbOfResult})", $"[Click here to see the other results]({url})");

                    await ReplyAsync("", false, embedBuilder.Build());
                }
                else
                {
                    await ReplyAsync($"No result in the wiki for {search}.");
                }
            }
        }
    }
}
