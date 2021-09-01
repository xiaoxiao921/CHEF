using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public class GiveRoleModule : ModuleBase<SocketCommandContext>
    {
        private const string PossibleRolesMessage = "Can only give/remove nsfw or the mod tester role for now.";

        [Command("role")]
        [Summary
            ("Give / Remove the role given as first parameter to the user who called this command. " +
            PossibleRolesMessage)]
        public async Task GiveRole(
            [Summary("The role you want to give / remove access to.")]
            [Remainder] string roleName = null)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                await Context.Channel.SendMessageAsync(PossibleRolesMessage);
                return;
            }

            var user = Context.User;
            roleName = roleName.ToLowerInvariant();

            if (user is IGuildUser gUser)
            {
                if (roleName == "nsfw")
                {
                    var nsfwRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
                    var nsfwRoleId = nsfwRole?.Id;

                    if (nsfwRoleId != null)
                    {
                        if (gUser.RoleIds.Any(id => id == nsfwRoleId))
                        {
                            await gUser.RemoveRoleAsync(nsfwRole);
                        }
                        else
                        {
                            await gUser.AddRoleAsync(nsfwRole);
                        }

                        await Context.Message.AddReactionAsync(Emote.Parse("<:KappaPride:570231271645511692>"));
                    }
                }
                else if (roleName == "guinea pig" || roleName == "mod tester")
                {
                    var modTesterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "guinea pig / mod tester");
                    var modTesterRoleId = modTesterRole?.Id;

                    if (modTesterRoleId != null)
                    {
                        if (gUser.RoleIds.Any(id => id == modTesterRoleId))
                        {
                            await gUser.RemoveRoleAsync(modTesterRole);
                        }
                        else
                        {
                            await gUser.AddRoleAsync(modTesterRole);
                        }

                        await Context.Message.AddReactionAsync(new Emoji("✅"));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(PossibleRolesMessage);
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Just tried to give a role to someone who is not in the server.");
            }
        }
    }

    public class SlashGiveRole : ISlashCommand
    {
        public bool IsGlobal => true;

        private const string Description = "Give / remove the listed roles.";

        private SlashCommandBuilder _builder;
        public SlashCommandBuilder Builder
        {
            get
            {
                if (_builder == null)
                {
                    _builder = new SlashCommandBuilder()
                        .WithName("role")
                        .WithDescription(Description)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("roleName")
                            .WithDescription("The role you want to give / remove.")
                            .WithRequired(true)
                            .AddChoice("nsfw", "nsfw")
                            .AddChoice("mod tester", "guinea pig / mod tester")
                            .WithType(ApplicationCommandOptionType.Role));
                        //.AddOption("guinea pig / mod tester", ApplicationCommandOptionType.Role, "mod tester role", required: true);
                }

                return _builder;
            }
        }

        public async Task Handle(SocketSlashCommand interaction)
        {
            var role = (SocketRole)interaction.Data.Options.First().Value;

            await interaction.RespondAsync("ROLE : " + role.Name + " | " + role.ToString());
        }
    }
}
