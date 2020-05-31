using System.Globalization;
using System.Linq;
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

                embedBuilder.AddField(command.Name, embedFieldText);
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
            embedBuilder.WithColor(Color.DarkOrange);

            var _userInfo = user ?? Context.User;
            var userInfo = (SocketGuildUser)_userInfo;

            embedBuilder.WithAuthor(author => author.Build());
            embedBuilder.Author.WithName($"{userInfo.Username}#{userInfo.Discriminator}");
            embedBuilder.Author.WithIconUrl(userInfo.GetAvatarUrl());

            embedBuilder.AddField($"{"" + '\u200B'}", $"{userInfo.Mention}");

            embedBuilder.AddField("Joined",
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
    }
}
