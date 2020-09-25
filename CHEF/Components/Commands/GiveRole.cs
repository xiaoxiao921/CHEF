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
                    await Context.Channel.SendMessageAsync("Can only give/remove nsfw or the mod tester role for now.");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Just tried to give a role to someone who is not in the server.");
            }
        }
    }
}
