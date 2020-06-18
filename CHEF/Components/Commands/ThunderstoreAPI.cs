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

        [JsonProperty("versions")]
        public VersionV1[] Versions { get; set; }

        public VersionV1 LatestPackage() => Versions[0];
        public long TotalDownloads() => Versions.Sum(version => version.Downloads);
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
        public string[] Dependencies { get; set; }

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

    public class PackageList
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("next")]
        public Uri Next { get; set; }

        [JsonProperty("previous")]
        public Uri Previous { get; set; }

        [JsonProperty("results")]
        public PackageV2[] Results { get; set; }
    }

    public class PackageV2
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

        [JsonProperty("rating_score")]
        public long RatingScore { get; set; }

        [JsonProperty("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("is_deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("total_downloads")]
        public long TotalDownloads { get; set; }

        [JsonProperty("latest")]
        public LatestPackage LatestPackage { get; set; }
    }

    public class LatestPackage
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
        public string[] Dependencies { get; set; }

        [JsonProperty("download_url")]
        public Uri DownloadUrl { get; set; }

        [JsonProperty("downloads")]
        public long Downloads { get; set; }

        [JsonProperty("date_created")]
        public DateTimeOffset DateCreated { get; set; }

        [JsonProperty("website_url")]
        public string WebsiteUrl { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }
    }

    public static class Thunderstore
    {
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

        public static async Task<PackageV2> GetModInfoV2(string modName)
        {
            using (var httpClient = new HttpClient())
            {
                var page = 1;
                while (true)
                {
                    const string apiUrl = "https://thunderstore.io/api/v2/package/?page=";
                    var apiPage = $"{apiUrl}{page++}";
                    var apiResult =
                        JsonConvert.DeserializeObject<PackageList>(await httpClient.GetStringAsync(apiPage));

                    foreach (var package in apiResult.Results)
                    {
                        if (package.Name.Contains(modName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return package;
                        }
                    }

                    if (apiResult.Next == null)
                        break;
                }

                return null;
            }
        }
    }
}
