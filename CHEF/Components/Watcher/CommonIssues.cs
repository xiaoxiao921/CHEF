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
        public const string DuplicateAssemblyError =
            "You seems to have `Tried to load duplicate assembly...` error message." +
            "When that happens, you should delete your " + 
            @"Risk of Rain 2\Risk of Rain 2_Data\Managed folder " + 
            "and verify the integrity of the game files\n" +
            "http://steamcdn-a.akamaihd.net/steam/support/faq/verifygcf2.gif";

        public const string R2APIMonoModPatchError =
            "You seems to have `The Monomod patch of R2API seems to be missing` error message." +
            "When that happens, it either means that :\n" +
            "You are missing the .dll file like the message is saying,\n" +
            "or\n" +
            "You are missing the monomod loader that is normally located in the " + 
            @"`Risk of Rain 2\BepInEx\patchers\BepInEx.MonoMod.Loader` folder." + "\n" + 
            "If you don't have this folder, please download BepInEx again from " + 
            "the thunderstore and make sure to follow the installation instructions.";

        public const string CrashLogLocation =
            "You also mentioned the word `crash` in your message.\n" +
            "Here is the file path for a log file that could be more useful to us :" +
            @"`C:\Users\<UserName>\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`" + "\n" + 
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

                if (text.Contains("The Monomod patch of", StringComparison.InvariantCultureIgnoreCase))
                {
                    answer.AppendLine(R2APIMonoModPatchError);
                    hasCommonError = true;
                }
            }

            return hasCommonError;
        }

        /// <summary>
        /// Check if the <paramref name="text"/> contains version numbers of mods<para/>
        /// Returns true if the text contains any outdated mods
        /// </summary>
        /// /// <param name="text">Text that may or may not contains mod version numbers</param>
        /// <param name="answer">StringBuilder that will contains the bot answer ready to be sent</param>
        /// /// <param name="author">User that we are answering to</param>
        /// <returns></returns>
        public static async Task<bool> CheckModsVersion(string text, StringBuilder answer, SocketUser author)
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
                            var verFromText = match.Groups[2].ToString().Replace(" ", "");
                            //Logger.Log("modName : " + modName);
                            //Logger.Log("verFromText : " + verFromText);

                            // todo : re-enable me when we have consistency
                            // across manifest version and the version from the plugin / log

                            if (modName.ToLower().Contains("r2api") ||
                                modName.ToLower().Contains("bepin"))
                            {
                                var latestVer = await IsThisLatestModVersion(modName, verFromText);
                                if (latestVer != null)
                                {
                                    outdatedMods.AppendLine($"{modName} v{verFromText} instead of v{latestVer}");
                                }
                            }
                        }
                    }

                    if (outdatedMods.Length > 0)
                    {
                        var outdatedModsS = outdatedMods.ToString();
                        answer.AppendLine(
                            $"{author.Mention}, looks like you don't have the latest version installed of " +
                            $"the following mod{(outdatedModsS.Contains('\n') ? "s" : "")} :" + Environment.NewLine +
                            outdatedModsS);

                        return true;
                    }

                    return false;
                }
            }

            return false;
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
