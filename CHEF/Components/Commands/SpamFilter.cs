using CHEF.Components.Watcher.Spam;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHEF.Components.Commands
{
    public class SpamFilter : ModuleBase<SocketCommandContext>
    {
        [Command("spamignoreadd")]
        [Summary("Adds a role to ignore list of a spam filter.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SpamIgnoreAdd([Summary("Name of a role")] string roleName)
        {
            var role = Context.Guild.Roles.FirstOrDefault(role => role.Name == roleName);
            if (role == null)
            {
                await ReplyAsync($"Couldn't find role **{roleName}**.");
                return;
            }
            using (var context = new SpamIgnoreRolesContext())
            {
                var ignore = await context.GetIgnore(role);
                if (ignore != null)
                {
                    await ReplyAsync($"Role **{roleName}** is already ignored.");
                }
                else
                {
                    ignore = new SpamIgnoreRole { DiscordId = role.Id };
                    context.Add(ignore);
                    await context.SaveChangesAsync();
                    await ReplyAsync($"Role **{roleName}** was added to ignore list.");
                }
            }
        }

        [Command("spamignoreremove")]
        [Summary("Removes a role from ignore list of a spam filter.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SpamIgnoreRemove([Summary("Name of a role")] string roleName)
        {
            var role = Context.Guild.Roles.FirstOrDefault(role => role.Name == roleName);
            if (role == null)
            {
                await ReplyAsync($"Couldn't find role **{roleName}**.");
                return;
            }
            using (var context = new SpamIgnoreRolesContext())
            {
                var ignore = await context.GetIgnore(role);
                if (ignore == null)
                {
                    await ReplyAsync($"Role **{roleName}** was not ignored.");
                }
                else
                {
                    context.Remove(ignore);
                    await context.SaveChangesAsync();
                    await ReplyAsync($"Role **{roleName}** was removed from ignore list.");
                }
            }
        }

        [Command("spamignorelist")]
        [Summary("Removes a role from ignore list of a spam filter.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SpamIgnoreList()
        {
            using (var context = new SpamIgnoreRolesContext())
            {
                var embedBuilder = new EmbedBuilder();
                var stringBuilder = new StringBuilder();
                await foreach (var ignoreRole in context.SpamIgnoreRoles.AsAsyncEnumerable())
                {
                    var role = Context.Guild.Roles.FirstOrDefault(role => role.Id == ignoreRole.DiscordId);
                    if (role != null)
                    {
                        stringBuilder.AppendLine(role.Name);
                    }
                }
                if (stringBuilder.Length == 0)
                {
                    await ReplyAsync("There is no roles ignored");
                }
                else
                {
                    embedBuilder.AddField("Here's a list of all ignored roles:", stringBuilder.ToString(), true);
                    await ReplyAsync("", false, embedBuilder.Build());
                }
            }
        }
    }
}
