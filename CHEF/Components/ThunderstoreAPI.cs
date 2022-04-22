using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CHEF.Components
{
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
        private const string ApiUrl = "https://thunderstore.io/api/v1/package/";

        public const string IsDownMessage = "Couldn't retrieve mod information, Thunderstore API is down. (Try again in 5-10 minutes)";
        public const string IsUpMessage = "The Thunderstore API is up.";

        private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        private static readonly HttpClient _httpClient = new HttpClient(_httpClientHandler);

        private static TimeSpan _cacheRefreshInterval = TimeSpan.FromMinutes(5);
        private static DateTime _lastCacheTime;
        private static PackageV1[] _packageCache = null;

        public static async Task<PackageV1> GetModInfoV1(string modName)
        {
            var timeNow = DateTime.Now;
            if (_packageCache == null || timeNow - _lastCacheTime >= _cacheRefreshInterval)
            {
                _packageCache = JsonConvert.DeserializeObject<PackageV1[]>(await _httpClient.GetStringAsync(ApiUrl));
                _lastCacheTime = timeNow;
            }

            var mod = _packageCache.FirstOrDefault(package => package.Name.Contains(modName, StringComparison.InvariantCultureIgnoreCase));
            if (mod == null)
            {
                mod = _packageCache.FirstOrDefault(package => package.FullName.Contains(modName, StringComparison.InvariantCultureIgnoreCase));
            }

            return mod;
        }
    }
}
