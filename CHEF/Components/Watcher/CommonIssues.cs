using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

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
        /// Check if the <paramref name="text"/> contains a version of R2API, if so, check if its the latest.<para/>
        /// Returns the latest version in the first string if <paramref name="text"/> doesn't already contains the latest version.<para/>
        /// Second string is the version that is in the text.<para/>
        /// Both strings are null if <paramref name="text"/> doesn't contains a R2API version.
        /// </summary>
        /// <param name="text">Text that may or may not contains a R2API version number.</param>
        /// <returns></returns>
        public static async Task<(string, string)> CheckR2APIVersion(string text)
        {
            if (text != null)
            {
                text = text.ToLower();
                if (text.Contains("loading [r2ap"))
                {
                    const string regexFindVer = "loading \\[r2ap[a-z] (.*?)]";
                    var rx = new Regex(regexFindVer,
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var match = rx.Match(text);
                    if (match.Groups.Count > 1)
                    {
                        var apiVer = rx.Match(text).Groups[1].ToString();

                        return (await IsLatestR2APIVersion(apiVer), apiVer);
                    }
                }
            }

            return (null, null);
        }

        /// <summary>
        /// Check if <paramref name="otherVer"/> is the latest version of R2API.<para/>
        /// Returns the latest version as a string, null if <paramref name="otherVer"/> is the latest.
        /// </summary>
        /// <param name="otherVer">Text that should be equal to a R2API version, outdated or not.</param>
        /// <returns></returns>
        private static async Task<string> IsLatestR2APIVersion(string otherVer)
        {
            using (var httpClient = new HttpClient())
            {
                var apiResult = JObject.Parse(await httpClient.GetStringAsync("https://thunderstore.io/api/v2/package/?page=1"));
                var latestVersion = apiResult["results"][0]["latest"]["version_number"].ToString();
                Logger.Log("comparing : " + latestVersion);
                Logger.Log("with : " + otherVer);
                if (!otherVer.Equals("0.0.1") && latestVersion.Equals(otherVer))
                {
                    return null;
                }
                else
                {
                    return latestVersion;
                }
            }
        }
    }
}
