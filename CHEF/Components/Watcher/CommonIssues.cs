using System;
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
        public const string DuplicateAssemblyError =
            "You seems to have `Tried to load duplicate assembly...` error message." +
            "When that happens, you should delete your " +
            @"Risk of Rain 2\Risk of Rain 2_Data\Managed folder " +
            "and verify the integrity of the game files\n" +
            "http://steamcdn-a.akamaihd.net/steam/support/faq/verifygcf2.gif";

        public const string CrashLogLocation =
            "You also mentioned the word `crash` in your message.\n" +
            "Here is the file path for a log file that could be more useful to us :" +
            @"`C:\Users\<UserName>\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`" + "\n" +
            "or\n" +
            @"`C:\Users\< UserName >\AppData\Local\Temp\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`";

        public const string VersionMismatch =
            "If you are struggling playing with people in private lobbies:\n" +
            "If you are using Steam Build ID mod or UnmoddedClients and that everyone have the mods, remove them.\n" +
            "You don't need any kind of id build spoofing if everyone have the same modding setup.\n";

        public CommonIssues(DiscordSocketClient client) : base(client)
        {
        }

        public override Task SetupAsync()
        {
            return Task.CompletedTask;
        }

        public static bool CheckCommonLogError(string text, StringBuilder answer, SocketUser author)
        {
            var hasCommonError = false;

            if (text != null)
            {
                if (text.Contains("Tried to load duplicate", StringComparison.InvariantCultureIgnoreCase))
                {
                    answer.AppendLine(DuplicateAssemblyError);
                    hasCommonError = true;
                }
            }

            return hasCommonError;
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

            if (!string.IsNullOrWhiteSpace(text))
            {
                const string ThunderstoreManifestPrefix = "TS Manifest: ";
                if (text.Contains(ThunderstoreManifestPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    const string regexFindVer = ThunderstoreManifestPrefix + "(.*)-([0-9]*.[0-9]*.[0-9]*)";
                    var rx = new Regex(regexFindVer,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var matches = rx.Matches(text);

                    var outdatedMods = new StringBuilder();
                    var deprecatedMods = new StringBuilder();

                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            // A log line is like this :
                            // TS Manifest: Random-Team-Name-ModName-1.0.0

                            var modTeamAndName = match.Groups[1].ToString().Split('-');
                            var modTeam = string.Join('-', modTeamAndName.Take(modTeamAndName.Length - 1));
                            var modName = modTeamAndName[^1];
                            var modVersion = match.Groups[2].ToString();

                            var (latestVer, isDeprecated) = await IsThisLatestModVersion(modName, modVersion);
                            if (latestVer != null && !isDeprecated)
                            {
                                outdatedMods.AppendLine($"{modTeam}-{modName} v{modVersion} instead of v{latestVer}");
                            }
                            else if (isDeprecated)
                            {
                                deprecatedMods.AppendLine($"{modTeam}-{modName}");
                            }
                        }

                    }

                    var notMentionedYet = true;
                    if (outdatedMods.Length > 0)
                    {
                        var outdatedModsS = outdatedMods.ToString();
                        var plural = outdatedModsS.Count(c => c == '\n') > 1;
                        answer.AppendLine(
                            $"{(notMentionedYet ? author.Mention + " y" : "Y")}ou don't have the latest version installed of " +
                            $"the following mod{(plural ? "s" : "")}:" + Environment.NewLine +
                            outdatedModsS);

                        textContainsAnyBadMod = true;
                        notMentionedYet = false;
                    }

                    if (deprecatedMods.Length > 0)
                    {
                        var deprecatedModsS = deprecatedMods.ToString();
                        var plural = deprecatedModsS.Count(c => c == '\n') > 1;
                        answer.AppendLine(
                            $"{(notMentionedYet ? author.Mention + " y" : "Y")}ou have {(plural ? "" : "a")} deprecated " +
                            $"mod{(plural ? "s" : "")} installed. Deprecated mods usually don't work:" + Environment.NewLine +
                            deprecatedModsS);

                        textContainsAnyBadMod = true;
                    }
                }
            }

            return textContainsAnyBadMod;
        }

        public static string RemoveDuplicateExceptionsFromText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                using (var stringReader = new StringReader(text))
                {
                    var isLinePartOfAnException = false;

                    HashSet<string> exceptions = new();
                    StringBuilder linesSb = new();

                    string line;
                    while ((line = stringReader.ReadLine()) != null)
                    {
                        if (line.Contains("Exception:", StringComparison.InvariantCulture))
                        {
                            isLinePartOfAnException = true;
                        }
                        else if (line.StartsWith('[') &&
                                line.Contains(':', StringComparison.InvariantCulture) &&
                                line.Contains(']', StringComparison.InvariantCulture))
                        {
                            isLinePartOfAnException = false;
                        }

                        if (!isLinePartOfAnException || exceptions.Add(line))
                        {
                            linesSb.AppendLine(line);
                        }
                    }

                    return linesSb.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Check for the <paramref name="modName"/> if <paramref name="verFromText"/> is the latest version.<para/>
        /// Returns the latest version as a string, null if <paramref name="verFromText"/> is the latest.
        /// Second Tuple value is true if mod is deprecated.
        /// </summary>
        /// <param name="modName">Name of the mod to check</param>
        /// <param name="verFromText">Text that should be equal to the mod version, outdated or not.</param>
        /// <returns></returns>
        private static async Task<(string, bool)> IsThisLatestModVersion(string modName, string verFromText)
        {
            var modInfo = await Thunderstore.GetModInfoV1(modName);
            if (modInfo != null)
            {
                if (modInfo.IsDeprecated)
                {
                    return (null, true);
                }

                var latestVer = modInfo.LatestPackage().VersionNumber;
                return !latestVer.Equals(verFromText) ? (latestVer, false) : (null, false);
            }

            return (null, false);
        }
    }
}
