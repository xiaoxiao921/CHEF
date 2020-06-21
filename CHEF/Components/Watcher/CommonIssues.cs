using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CHEF.Components.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Watcher
{
    public class CommonIssues : Component
    {
        public const string CrashLogLocation =
            "You also mentioned the word `crash` in your message.\n" +
            "Here is the file path for a log file that could be more useful to us :" +
            @"`C:\Users\<UserName>\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`\n" +
            "or\n" +
            @"`C:\Users\< UserName >\AppData\Local\Temp\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`";

        public const string VersionMismatch = 
            "If you are struggling playing with people in private lobbies:\n" +
            "If you are using Steam Build ID mod or UnmoddedClients and that everyone have the mods, remove them.\n" +
            "You don't need any kind of id build spoofing if everyone have the same modding setup.\n" +
            "If you want to play with people that have the mods but also with people who doesnt. Proceed as follow :\n" +
            "Open the in-game console and type this : `build_id_steam`, then invite people who are unmodded.\n" +
            "Then, do the same with modded people, by typing : `build_id_mod`";

        public CommonIssues(DiscordSocketClient client) : base(client)
        {
        }

        public override Task SetupAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Check if the <paramref name="text"/> contains version numbers of mods, if so,<para/>
        /// check if their version number in the text is the latest by querying the thunderstore api.<para/>
        /// Returns the latest version in the first string if <paramref name="text"/> doesn't already contains the latest version.<para/>
        /// Second string is the version that is in the text.<para/>
        /// Both strings are null if <paramref name="text"/> doesn't contains any mod version.
        /// </summary>
        /// <param name="text">Text that may or may not contains mod version numbers.</param>
        /// <returns></returns>
        public static async Task<string> CheckModsVersion(string text)
        {
            if (text != null)
            {
                if (text.Contains("loading [", StringComparison.InvariantCultureIgnoreCase))
                {
                    var outdatedMods = new StringBuilder();

                    const string regexFindVer = "Loading \\[(.*?) ([0-9].*?)]";
                    var rx = new Regex(regexFindVer,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var matches = rx.Matches(text);

                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            var modName = match.Groups[1].ToString();
                            var verFromText = match.Groups[2].ToString();
                            Logger.Log("modName : " + modName);
                            Logger.Log("verFromText : " + verFromText);
                            var latestVer = await IsThisLatestModVersion(modName, verFromText);
                            if (latestVer != null)
                            {
                                outdatedMods.AppendLine($"{modName} v{verFromText} instead of {latestVer}");
                            }
                        }
                    }

                    return outdatedMods.Length > 0 ? outdatedMods.ToString() : null;
                }
            }

            return null;
        }

        /// <summary>
        /// Check for the <paramref name="modName"/> if <paramref name="otherVer"/> is the latest version.<para/>
        /// Returns the latest version as a string, null if <paramref name="otherVer"/> is the latest.
        /// </summary>
        /// <param name="modName">Name of the mod to check</param>
        /// <param name="otherVer">Text that should be equal to the mod version, outdated or not.</param>
        /// <returns></returns>
        private static async Task<string> IsThisLatestModVersion(string modName, string otherVer)
        {
            var modInfo = await Thunderstore.GetModInfoV1(modName);
            if (modInfo != null)
            {
                var latestVer = modInfo.LatestPackage().VersionNumber;
                return !latestVer.Equals(otherVer) ? latestVer : null;
            }

            return null;
        }
    }
}
