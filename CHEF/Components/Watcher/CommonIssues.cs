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
