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
        private static readonly byte messagesForBan = 4;
        private static readonly byte hashLifetimeInSeconds = 15;

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
        private void CleanUpTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.UtcNow;
            var usersToRemove = new List<ulong>();
            var hashesToRemove = new List<HashResult>();

            try
            {
                foreach (var guild in hashes)
                {
                    foreach (var user in guild.Value)
                    {
                        var removeUser = false;
                        foreach (var hash in user.Value)
                        {
                            if (hash.Value.count >= messagesForBan)
                            {
                                removeUser = true;
                                break;
                            }
                            if (hash.Value.lastUpdate.AddSeconds(hashLifetimeInSeconds) < now)
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
                using (var context = new SpamIgnoreRolesContext())
                {
                    if (await context.IsIgnored(guildUser.Roles))
                    {
                        return false;
                    }
                }

                using (var md5 = new HMACMD5(md5Key))
                {
                    var builder = new StringBuilder(msg.Content);
                    foreach (var attachment in msg.Attachments)
                    {
                        builder.Append(attachment.Url);
                    }
                    foreach (var embed in msg.Embeds)
                    {
                        builder.Append(embed.Title).Append(embed.Description).Append(embed.Footer).Append(embed.Url);
                    }

                    var hash = new HashResult(md5.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
                    var guildDict = hashes.GetOrAdd(guild.Id, CreateGuildDict);
                    var userDict = guildDict.GetOrAdd(author.Id, CreateUserDict);
                    var info = userDict.GetOrAdd(hash, CreateHashInfo);

                    if (info.lastUpdate.AddSeconds(hashLifetimeInSeconds) < DateTime.UtcNow)
                    {
                        info.count = 0;
                    }

                    info.count++;
                    info.lastUpdate = DateTime.UtcNow;

                    if (info.count >= messagesForBan)
                    {
                        await guild.AddBanAsync(author, 1, $"Sending the same message {messagesForBan} times with less than {hashLifetimeInSeconds} seconds gap");
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
