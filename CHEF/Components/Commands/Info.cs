using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HtmlAgilityPack;

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

            var web = new HtmlWeb();
            var document = (await web.LoadFromWebAsync(url)).DocumentNode;
            
            var wikiResultsParent = document.SelectSingleNode("//div[@id=\"wiki_search_results\"]");
            if (wikiResultsParent != null)
            {
                var nbOfResultQuery =
                    document.SelectSingleNode(
                        "//div[@class=\"d-flex flex-column flex-md-row flex-justify-between border-bottom pb-3 position-relative\"]/h3");
                var nbOfResult = Regex.Replace(
                    nbOfResultQuery.FirstChild.GetDirectInnerText(), @"[^\d]", "", 
                    RegexOptions.Compiled);

                var wikiResult =
                    document.SelectSingleNode(
                        "//div[@class=\"hx_hit-wiki py-4 border-top\"]");

                const string githubUrl = "https://github.com";

                var aElement = wikiResult.SelectSingleNode("./div[1]/a");
                var resultTitle = HttpUtility.HtmlDecode(aElement.GetAttributeValue("title", ""));
                var resultLink =
                    githubUrl + HttpUtility.HtmlDecode(aElement.GetAttributeValue("href", ""));

                var resultDescQuery = wikiResult.SelectSingleNode("./p[1]");
                var resultDesc = HttpUtility.HtmlDecode(resultDescQuery.GetDirectInnerText());

                var lastUpdatedQuery = wikiResult.SelectSingleNode("./div[2]/relative-time");
                var lastUpdated = lastUpdatedQuery.GetDirectInnerText();

                var embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(Color.Orange);

                embedBuilder.WithAuthor("R2Wiki", "https://avatars1.githubusercontent.com/u/49210367", resultLink);
                embedBuilder.WithTitle(resultTitle);
                embedBuilder.WithUrl(resultLink);
                embedBuilder.WithDescription($"{resultDesc}\n*This page was last updated {lastUpdated}*");

                embedBuilder.AddField($"Other results ({int.Parse(nbOfResult) - 1})", $"[Click here to see the other results]({url})");

                await ReplyAsync("", false, embedBuilder.Build());
            }
            else
            {
                await ReplyAsync($"No result in the wiki for {search}.");
            }
        }
    }
}
