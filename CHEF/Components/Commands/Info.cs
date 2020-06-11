using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

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
            var modInfo = await GetModInfo(modName);

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

        public static async Task<Package> GetModInfo(string modName)
        {
            using (var httpClient = new HttpClient())
            {
                var page = 1;
                while (true)
                {
                    const string apiUrl = "https://thunderstore.io/api/v2/package/?page=";
                    var apiPage = $"{apiUrl}{page++}";
                    var apiResult =
                        JsonConvert.DeserializeObject<PackageList>(await httpClient.GetStringAsync(apiPage));

                    foreach (var package in apiResult.Results)
                    {
                        if (package.Name.ToLower().Contains(modName.ToLower()))
                        {
                            return package;
                        }
                    }

                    if (apiResult.Next == null)
                        break;
                }

                return null;
            }
        }
    }
}
