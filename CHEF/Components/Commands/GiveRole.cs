using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace CHEF.Components.Commands
{
    public class GiveRoleModule : ModuleBase<SocketCommandContext>
    {
        [Command("role")]
        [Summary
            ("Give / Remove the role given as first parameter to the user who called this command.")]
        public async Task GiveRole(
            [Summary("The role you want to give / remove access to.")]
            string roleName)
        {
            if (roleName == "nsfw")
            {
                var user = Context.User;
                var nsfwRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
                var nsfwRoleId = nsfwRole?.Id;

                if (user is IGuildUser gUser && nsfwRoleId != null)
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
            else
            {
                await Context.Channel.SendMessageAsync("Can only give/remove nsfw role for now.");
            }
        }
    }
}
