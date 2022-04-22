using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CHEF.Components.Commands.Ignore;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CHEF.Components.Commands
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task Help(
            [Summary("The (optional) command name to get more detailed info from")]
            string cmdName = null)
        {
            if (Context.Guild != null)
            {
                var currentChannel = Context.Channel.Id;
                const ulong botChannel = 723014139060027462;
                if (currentChannel != botChannel)
                {
                    await Context.Channel.SendMessageAsync($"Use `help` in {MentionUtils.MentionChannel(botChannel)}");
                    return;
                }
            }

            var commands = CommandHandler.Service.Commands.ToList();
            var embedBuilder = new EmbedBuilder();

            commands.Remove(commands.Single(command => command.Name.Equals("help")));
            if (cmdName == null)
            {
                foreach (var command in commands)
                {
                    string embedFieldText;
                    if (command.Summary == null)
                    {
                        embedFieldText = "No description available\n";
                    }
                    else
                    {
                        embedFieldText = command.Summary;
                        var parameters = command.Parameters;
                        if (parameters != null)
                        {
                            embedFieldText += $"\n{parameters.Count} parameter{(parameters.Count > 1 ? "s" : "")}:\n";
                            foreach (var param in parameters)
                            {
                                embedFieldText += $"{param.Type}: {param.Summary}\n";
                            }
                        }
                    }
                    var sb = new StringBuilder();
                    foreach (var alias in command.Aliases)
                    {
                        sb.Append(alias);
                        sb.Append(" / ");
                    }
                    embedBuilder.AddField("\u200B", $"{sb}\n{embedFieldText}");
                }

                await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
            }
            else
            {
                var command = commands.FirstOrDefault(info =>
                    info.Name.Equals(cmdName, StringComparison.InvariantCultureIgnoreCase));

                string res;
                if (command == null)
                {
                    res = $"Could not find any static command called `{cmdName}`.";
                }
                else
                {
                    res = command.Summary ?? "No description available\n";
                }

                await ReplyAsync(res);
            }
        }

        [Command("optout")]
        [Summary("Opts the user out of automatic message scanning.")]
        public async Task OptOut()
        {
            var user = Context.User;
            using (var context = new IgnoreContext())
            {
                var alreadyIgnored = await context.IsIgnored(user);
                if (alreadyIgnored)
                {
                    await ReplyAsync("I'm already ignoring you.");
                }
                else
                {
                    var ignore = new Ignore.Ignore { DiscordId = user.Id };
                    context.Add(ignore);
                    await context.SaveChangesAsync();
                    await ReplyAsync("No longer scanning your messages.");
                }
            }
        }

        [Command("optin")]
        [Summary("Opts the user in of automatic message scanning.")]
        public async Task OptIn()
        {
            var user = Context.User;
            using (var context = new IgnoreContext())
            {
                var ignored = await context.GetIgnore(user.Id);
                if (ignored == null)
                {
                    await ReplyAsync("I wasn't ignoring you.");
                }
                else
                {
                    context.Remove(ignored);
                    await context.SaveChangesAsync();
                    await ReplyAsync("Scanning your messages again.");
                }
            }
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

            embedBuilder.AddField("\u200B", $"{userInfo.Mention}");

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
                {
                    roles.Append(role.Mention);
                }
            }
            if (roles.Length > 1)
            {
                embedBuilder.AddField("Roles", roles);
            }

            await ReplyAsync("", false, embedBuilder.Build());
        }

        [Command("modinfo")]
        [Summary
            ("Returns info about a mod that is uploaded on thunderstore.io")]
        [Alias("mod")]
        public async Task ModInfo(
            [Summary("The mod to get info from")]
            [Remainder] string modName)
        {
            modName = modName.Replace(' ', '_');
            PackageV1 modInfo;
            try
            {
                modInfo = await Thunderstore.GetModInfoV1(modName);
            }
            catch (JsonSerializationException)
            {
                await ReplyAsync(Thunderstore.IsDownMessage);
                return;
            }

            if (modInfo == null)
            {
                await ReplyAsync($"Could not find any mod that contains `{modName}` in its name.");
                return;
            }

            var embedBuilder = new EmbedBuilder();

            var pinkColor = new Color(255, 20, 147);
            embedBuilder.WithColor(modInfo.IsDeprecated ? Color.Red : modInfo.IsNsfw() ? pinkColor : Color.Green);

            embedBuilder.WithAuthor(modInfo.Owner);
            embedBuilder.WithTitle($"{modInfo.Name} v{modInfo.LatestPackage().VersionNumber}");
            embedBuilder.WithUrl(modInfo.PackageUrl.AbsoluteUri);
            embedBuilder.WithDescription(modInfo.LatestPackage().Description);
            embedBuilder.WithThumbnailUrl(modInfo.LatestPackage().Icon.AbsoluteUri);

            if (modInfo.IsDeprecated)
            {
                embedBuilder.AddField("DEPRECATED", "This mod is deprecated, it may not work correctly.");
            }

            if (modInfo.Categories != null)
            {
                var categoriesString = string.Join(", ", modInfo.Categories);
                if (!string.IsNullOrEmpty(categoriesString))
                {
                    embedBuilder.AddField("Categories", categoriesString);
                }
            }

            embedBuilder.AddField("Rating Score", modInfo.RatingScore, true);
            embedBuilder.AddField("Total downloads", modInfo.TotalDownloads(), true);

            embedBuilder.WithFooter(new EmbedFooterBuilder { Text = "Last updated:" });
            embedBuilder.WithTimestamp(modInfo.LatestPackage().DateCreated);

            await ReplyAsync("", false, embedBuilder.Build());
        }

        [Command("api")]
        [Summary
            ("Returns whether or not the thunderstore api is down.")]
        [Alias("apidown")]
        public async Task ThunderstoreAPIInfo()
        {
            PackageV1 modInfo;
            try
            {
                modInfo = await Thunderstore.GetModInfoV1("bepin");
            }
            catch (JsonSerializationException)
            {
                await ReplyAsync(Thunderstore.IsDownMessage);
                return;
            }

            if (modInfo != null)
            {
                await ReplyAsync(Thunderstore.IsUpMessage);
            }
        }

        [Command("wiki")]
        [Summary
            ("Returns info about wiki pages from the R2Wiki")]
        public async Task WikiSearch(
            [Remainder] string search = "Home")
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
