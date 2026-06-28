using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CHEF.Components
{
    public class ListingResponse
    {
        [JsonProperty("results")]
        public Package[] Results { get; set; }
    }

    public class Package
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("datetime_created")]
        public DateTimeOffset DateCreated { get; set; }

        [JsonProperty("last_updated")]
        public DateTimeOffset DateUpdated { get; set; }

        [JsonProperty("rating_count")]
        public long RatingScore { get; set; }

        [JsonProperty("is_pinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("is_deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("is_nsfw")]
        public bool HasNsfwContent { get; set; }

        [JsonProperty("categories")]
        public Category[] Categories { get; set; }

        [JsonProperty("download_count")]
        public long DownloadCount { get; set; }

        public string PackageUrl() => $"https://thunderstore.io/c/riskofrain2/p/{Namespace}/{Name}/";
        public bool IsNsfw() => HasNsfwContent ||
                                Categories.Any(category => category.Name.Contains("nsfw", StringComparison.OrdinalIgnoreCase));
    }

    public class Category
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public static class Thunderstore
    {
        private const string RoR2Community = "riskofrain2";
        private const string ModsSectionId = "017857cd-10d0-5372-c2de-7f75f6ca3e95";
        private const string ApiUrl = $"https://thunderstore.io/api/cyberstorm/listing/{RoR2Community}/?section={ModsSectionId}";

        public const string IsDownMessage = "Couldn't retrieve mod information, Thunderstore API is down. (Try again in 5-10 minutes)";
        public const string IsUpMessage = "The Thunderstore API is up.";

        private static readonly HttpClientHandler _httpClientHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        private static readonly HttpClient _httpClient = new(_httpClientHandler);

        public static async Task<Package> GetModInfoV1(string modName)
        {
            var url = $"{ApiUrl}&q={Uri.EscapeDataString(modName)}";
            var response = JsonConvert.DeserializeObject<ListingResponse>(await _httpClient.GetStringAsync(url));

            return response.Results?.FirstOrDefault();
        }
    }
}
