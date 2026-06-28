using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CHEF.Components.Watcher
{
    public class CommonIssues : Component
    {
        public CommonIssues(DiscordSocketClient client) : base(client)
        {
        }

        public override Task SetupAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Build a string discord bot answer that output outdated / deprecated mods from the <paramref name="text"/> by checking for TS Manifest lines in it and the TS API<para/>
        /// </summary>
        /// /// <param name="text">Text that may or may not contains mod version numbers (TS Manifest lines)</param>
        /// <param name="answer">StringBuilder that will contains the bot answer ready to be sent</param>
        /// /// <param name="author">the user who posted the <paramref name="text"/></param>
        /// <returns>true if the <paramref name="text"/> contains any outdated / deprecated mods</returns>
        public static async Task<bool> CheckForOutdatedAndDeprecatedMods(string text, StringBuilder answer, SocketUser author)
        {
            var textContainsAnyBadMod = false;

            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return textContainsAnyBadMod;
                }

                const string ThunderstoreManifestPrefix = "TS Manifest: ";

                if (!text.Contains(ThunderstoreManifestPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    return textContainsAnyBadMod;
                }

                const string regexFindVer = ThunderstoreManifestPrefix + "(.*)-([0-9]*.[0-9]*.[0-9]*)";
                var rx = new Regex(regexFindVer, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var matches = rx.Matches(text);

                var outdatedModsBag = new ConcurrentBag<string>();
                var deprecatedModsBag = new ConcurrentBag<string>();

                var tasks = matches.Cast<Match>().Select(async match =>
                {
                    if (match.Groups.Count <= 2)
                        return;

                    var modTeamAndName = match.Groups[1].ToString().Split('-');
                    var modTeam = string.Join('-', modTeamAndName.Take(modTeamAndName.Length - 1));
                    var modName = modTeamAndName[^1];
                    var modVersion = match.Groups[2].ToString();

                    var res = await GetIsOutdatedAndIsDeprecated(modTeam, modName, modVersion);

                    if (res.IsModDeprecated)
                    {
                        deprecatedModsBag.Add($"[{modTeam}-{modName}](<{res.PackageUrl}>)");
                    }
                    else if (res.IsOutdated)
                    {
                        outdatedModsBag.Add($"[{modTeam}-{modName}](<{res.PackageUrl}>) (v{modVersion} → v{res.LastVersionNumber})");
                    }
                });

                await Task.WhenAll(tasks);

                var outdatedMods = outdatedModsBag.ToList();
                var deprecatedMods = deprecatedModsBag.ToList();

                if (outdatedMods.Count > 0)
                {
                    answer.AppendLine($"Outdated mods detected:");

                    for (int i = 0; i < outdatedMods.Count; i++)
                        answer.AppendLine($"• {outdatedMods[i]}");

                    answer.AppendLine();
                    textContainsAnyBadMod = true;
                }

                if (deprecatedMods.Count > 0)
                {
                    answer.AppendLine($"Deprecated mods detected:");

                    for (int i = 0; i < deprecatedMods.Count; i++)
                        answer.AppendLine($"• {deprecatedMods[i]}");

                    answer.AppendLine();
                    textContainsAnyBadMod = true;
                }

                return textContainsAnyBadMod;
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return textContainsAnyBadMod;
        }

        struct GetIsOutdatedAndIsDeprecatedResult
        {
            public string PackageUrl;
            public string LastVersionNumber;
            public bool IsModDeprecated;
            public bool IsOutdated;
        }

        private static async Task<GetIsOutdatedAndIsDeprecatedResult> GetIsOutdatedAndIsDeprecated(string modTeam, string modName, string verFromText)
        {
            try
            {
                var modInfo = await Thunderstore.GetModInfoCyberstormWithTeam(modTeam, modName);
                if (modInfo == null)
                {
                    return default;
                }

                var latestVer = modInfo.VersionNumber();
                return new GetIsOutdatedAndIsDeprecatedResult()
                {
                    PackageUrl = modInfo.PackageUrl(),
                    LastVersionNumber = latestVer,
                    IsModDeprecated = modInfo.IsDeprecated,
                    IsOutdated = !string.IsNullOrWhiteSpace(latestVer) && !latestVer.Equals(verFromText)
                };
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return default;
        }
    }
}
