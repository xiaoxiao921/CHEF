using CHEF.Components.Watcher.Spam;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHEF.Components.Commands.Spam
{
    [Group("spamconfig")]
    public class SpamConfig : ModuleBase<SocketCommandContext>
    {
        [Command("list")]
        [Alias("l", "ls")]
        [Summary("Shows current configuration of a spam filter")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SpamConfigList()
        {
            using (var context = new SpamFilterContext())
            {
                var config = await context.GetFilterConfig();
                var embedBuilder = new EmbedBuilder();
                embedBuilder.AddField(nameof(SpamFilterConfig.ActionOnSpam), config.ActionOnSpam.ToString());
                var muteRole = Context.Guild.GetRole(config.MuteRoleId);
                embedBuilder.AddField("MuteRole", muteRole?.Name ?? "<unset>");
                embedBuilder.AddField(nameof(SpamFilterConfig.MessagesForAction), config.MessagesForAction.ToString());
                embedBuilder.AddField(nameof(SpamFilterConfig.MessageSecondsGap), config.MessageSecondsGap.ToString());
                embedBuilder.AddField(nameof(SpamFilterConfig.IncludeMessageContentInLog), config.IncludeMessageContentInLog.ToString());
                await ReplyAsync("", false, embedBuilder.Build());
            }
        }

        [Command("Update")]
        [Alias("u")]
        [Summary("Update a field of spam filter config")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SpamConfigUpdate(
            [Summary("Config field name")] string fieldName,
            [Summary("New value")] string value)
        {
            using (var context = new SpamFilterContext())
            {
                var config = await context.GetFilterConfig();
                switch (fieldName.ToLower())
                {
                    case "actiononspam":
                        if (!Enum.TryParse<ActionOnSpam>(value, true, out var action))
                        {
                            await ReplyAsync($"Can be only one of these values:\n{string.Join("\n", Enum.GetNames(typeof(ActionOnSpam)))}");
                            return;
                        }
                        config.ActionOnSpam = action;
                        break;
                    case "muterole":
                        var role = Context.Guild.Roles.FirstOrDefault(el => el.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));
                        if (role == null)
                        {
                            await ReplyAsync($"There is no role with such name");
                            return;
                        }
                        config.MuteRoleId = role.Id;
                        break;
                    case "messagesforaction":
                        if (!int.TryParse(value, out var messagesForAction) || messagesForAction < 1)
                        {
                            await ReplyAsync($"Value should be an integer and more than 0");
                            return;
                        }
                        config.MessagesForAction = messagesForAction;
                        break;
                    case "messagesecondsgap":
                        if (!int.TryParse(value, out var messageSecondsGap) || messageSecondsGap < 1)
                        {
                            await ReplyAsync($"Value should be an integer and more than 0");
                            return;
                        }
                        config.MessageSecondsGap = messageSecondsGap;
                        break;
                    case "includemessagecontentinlog":
                        if (!bool.TryParse(value, out var includeMessageContentInLog))
                        {
                            if (!int.TryParse(value, out var includeMessageContentInLogInt) || (includeMessageContentInLogInt != 0 && includeMessageContentInLogInt != 1))
                            {
                                await ReplyAsync($"Value should be one of these values:\ntrue\nfalse\n1\n0");
                                return;
                            }
                            includeMessageContentInLog = includeMessageContentInLogInt == 1;
                        }
                        config.IncludeMessageContentInLog = includeMessageContentInLog;
                        break;
                    default:
                        await ReplyAsync($"**{fieldName}** is not a valid field");
                        return;
                }
                await context.SaveConfig(config);
                await ReplyAsync($"Successfully updated");
            }
        }
    }
}
