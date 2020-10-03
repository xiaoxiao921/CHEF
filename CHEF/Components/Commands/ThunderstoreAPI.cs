using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CHEF.Components.Commands
{
    using System;
    using Newtonsoft.Json;

    public class PackageV1
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("package_url")]
        public Uri PackageUrl { get; set; }

        [JsonProperty("date_created")]
        public DateTimeOffset DateCreated { get; set; }

        [JsonProperty("date_updated")]
        public DateTimeOffset DateUpdated { get; set; }

        [JsonProperty("uuid4")]
        public Guid Uuid4 { get; set; }

        [JsonProperty("rating_score")]
        public long RatingScore { get; set; }

        [JsonProperty("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("is_deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("has_nsfw_content")]
        public bool HasNsfwContent { get; set; }

        [JsonProperty("categories")]
        public string[] Categories { get; set; }

        [JsonProperty("versions")]
        public VersionV1[] Versions { get; set; }

        public VersionV1 LatestPackage() => Versions[0];
        public long TotalDownloads() => Versions.Sum(version => version.Downloads);
        public bool IsNsfw() => HasNsfwContent || 
                                Categories.Any(category => category.ToLowerInvariant().Contains("nsfw"));
    }

    public class VersionV1
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon")]
        public Uri Icon { get; set; }

        [JsonProperty("version_number")]
        public string VersionNumber { get; set; }

        [JsonProperty("dependencies")]
        public object[] Dependencies { get; set; }

        [JsonProperty("download_url")]
        public Uri DownloadUrl { get; set; }

        [JsonProperty("downloads")]
        public long Downloads { get; set; }

        [JsonProperty("date_created")]
        public DateTimeOffset DateCreated { get; set; }

        [JsonProperty("website_url")]
        public Uri WebsiteUrl { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("uuid4")]
        public Guid Uuid4 { get; set; }
    }

    public static class Thunderstore
    {
        public const string IsDownMessage = "Couldn't retrieve mod information, Thunderstore API is down. (Try again in 5-10 minutes)";
        public const string IsUpMessage = "The Thunderstore API is up.";

        public static async Task<PackageV1> GetModInfoV1(string modName)
        {
            using (var httpClient = new HttpClient())
            {
                const string apiUrl = "https://thunderstore.io/api/v1/package/";
                var apiResult =
                    JsonConvert.DeserializeObject<PackageV1[]>(await httpClient.GetStringAsync(apiUrl));

                return apiResult.FirstOrDefault(package => package.Name.Contains(modName, StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}
