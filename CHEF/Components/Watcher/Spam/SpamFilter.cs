using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CHEF.Components.Watcher.Spam
{
    public class SpamFilter
    {
        //For the purpose of this class doesn't matter what the key as long as it's the same one
        private static readonly byte[] md5Key = new byte[] { 1 };

        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, ConcurrentDictionary<HashResult, HashInfo>>> hashes = new();
        private readonly Timer cleanUpTimer = new()
        {
            AutoReset = false,
            Interval = 30000
        };

        public SpamFilter()
        {
            cleanUpTimer.Elapsed += CleanUpTimerElapsed;
            cleanUpTimer.Start();
        }

        /// <summary>
        /// Removes hashes that expired, removes users that have no active hashes or were banned
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CleanUpTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.UtcNow;
            var usersToRemove = new List<ulong>();
            var hashesToRemove = new List<HashResult>();

            try
            {
                SpamFilterConfig config;
                using (var context = new SpamFilterContext())
                {
                    config = await context.GetFilterConfig();
                }

                foreach (var guild in hashes)
                {
                    foreach (var user in guild.Value)
                    {
                        var removeUser = false;
                        foreach (var hash in user.Value)
                        {
                            if (hash.Value.count >= config.MessagesForAction)
                            {
                                removeUser = true;
                                break;
                            }
                            if (hash.Value.lastUpdate.AddSeconds(config.MessageSecondsGap) < now)
                            {
                                hashesToRemove.Add(hash.Key);
                            }
                        }

                        if (!removeUser)
                        {
                            foreach (var hash in hashesToRemove)
                            {
                                user.Value.TryRemove(hash, out _);
                            }
                        }
                        hashesToRemove.Clear();
                        if (removeUser || user.Value.IsEmpty)
                        {
                            usersToRemove.Add(user.Key);
                        }
                    }
                    foreach (var user in usersToRemove)
                    {
                        guild.Value.TryRemove(user, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            finally
            {
                //Not using AutoRestart because it doesn't matter if the time between cleanups is not exactly the same
                //and in case it would take too long there won't be a second cleanup method running 
                cleanUpTimer.Start();
            }
        }

        /// <summary>
        /// Check if a message is a spam and ban its author if it is
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True if successful and further message processing can be skipped, otherwise false</returns>
        internal async Task<bool> Try(SocketMessage msg)
        {
            try
            {
                var author = msg.Author;
                if (author.IsBot || author.IsWebhook)
                {
                    return false;
                }

                var channel = msg.Channel as SocketGuildChannel;
                var guild = channel?.Guild;
                if (guild == null)
                {
                    return false;
                }

                var guildUser = author as SocketGuildUser;
                SpamFilterConfig config;
                using (var context = new SpamFilterContext())
                {
                    if (await context.IsIgnored(guildUser.Roles))
                    {
                        return false;
                    }
                    config = await context.GetFilterConfig();
                }

                using (var md5 = new HMACMD5(md5Key))
                {
                    var builder = new StringBuilder(msg.Content);
                    if (msg.Attachments.Count >= config.AttachmentCountForSpam)
                    {
                        builder.Append($" {msg.Attachments.Count} attachments");
                    }
                    if (msg.Embeds.Count >= config.EmbedCountForSpam)
                    {
                        builder.Append($" {msg.Attachments.Count} embeds");
                    }

                    var hash = new HashResult(md5.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
                    var guildDict = hashes.GetOrAdd(guild.Id, CreateGuildDict);
                    var userDict = guildDict.GetOrAdd(author.Id, CreateUserDict);
                    var info = userDict.GetOrAdd(hash, CreateHashInfo);

                    if (info.lastUpdate.AddSeconds(config.MessageSecondsGap) < DateTime.UtcNow)
                    {
                        info.count = 0;
                    }

                    info.count++;
                    info.lastUpdate = DateTime.UtcNow;

                    if (info.count >= config.MessagesForAction)
                    {
                        string auditReason = GetAuditActionReason(config, msg);
                        switch (config.ActionOnSpam)
                        {
                            case ActionOnSpam.Mute:
                                var muteRole = guild.GetRole(config.MuteRoleId);
                                if (muteRole == null)
                                {
                                    var logMsg = $"Mute role '{config.MuteRoleId}' was not found. Kicking instead. " + auditReason;
                                    Logger.Log(logMsg);
                                    await guildUser.KickAsync(logMsg);
                                }
                                else
                                {
                                    var logMsg = $"Adding mute role to {guildUser.Nickname} : {auditReason}";
                                    Logger.Log(logMsg);
                                    await guildUser.AddRoleAsync(config.MuteRoleId, new RequestOptions { AuditLogReason = auditReason });
                                }
                                await DeleteUserMessages(guild, guildUser, config);
                                break;
                            case ActionOnSpam.Kick:
                                Logger.Log($"Spam kick user {guildUser.Nickname} : {auditReason}");

                                await guildUser.KickAsync(auditReason);
                                await DeleteUserMessages(guild, guildUser, config);
                                break;
                            case ActionOnSpam.Ban:
                                Logger.Log($"Spam Ban user {guildUser.Nickname} : {auditReason}");

                                await guild.AddBanAsync(author, 1, auditReason);
                                break;
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
            return false;
        }

        /// <summary>
        /// Deletes all user messages that currently in message cache
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="user"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task DeleteUserMessages(SocketGuild guild, SocketGuildUser user, SpamFilterConfig config)
        {
            foreach (var channel in guild.Channels)
            {
                if (channel is not SocketTextChannel socketChannel)
                {
                    continue;
                }

                var messages = new List<ulong>();
                foreach (var message in socketChannel.GetCachedMessages())
                {
                    if (message.Author.Id == user.Id)
                    {
                        messages.Add(message.Id);
                    }
                }

                try
                {
                    await socketChannel.DeleteMessagesAsync(messages);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString());
                }
            }
        }

        private string GetAuditActionReason(SpamFilterConfig config, SocketMessage message)
        {
            var builder = new StringBuilder();
            builder
                .Append("Sending the same message ")
                .Append(config.MessagesForAction)
                .Append(" times with less than ")
                .Append(config.MessageSecondsGap)
                .AppendLine(" seconds gap.");

            if (config.IncludeMessageContentInLog)
            {
                builder.AppendLine("User's message:");
                builder.AppendLine(message.Content);
            }
            return builder.ToString();
        }

        private ConcurrentDictionary<ulong, ConcurrentDictionary<HashResult, HashInfo>> CreateGuildDict(ulong key)
        {
            return new ConcurrentDictionary<ulong, ConcurrentDictionary<HashResult, HashInfo>>();
        }

        private ConcurrentDictionary<HashResult, HashInfo> CreateUserDict(ulong key)
        {
            return new ConcurrentDictionary<HashResult, HashInfo>();
        }

        private HashInfo CreateHashInfo(HashResult key)
        {
            return new HashInfo();
        }

        private class HashInfo
        {
            public byte count = 0;
            public DateTime lastUpdate = DateTime.UtcNow;
        }

        private struct HashResult
        {
            public readonly ulong first;
            public readonly ulong second;

            public HashResult(byte[] bytes)
            {
                first = 0;
                second = 0;

                for (int i = 0; i < 8; i++)
                {
                    first = (first << 8) | bytes[i];
                    second = (second << 8) | bytes[i + 8];
                }
            }

            public override bool Equals(object obj)
            {
                return obj is HashResult result &&
                       first == result.first &&
                       second == result.second;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(first, second);
            }
        }
    }
}
