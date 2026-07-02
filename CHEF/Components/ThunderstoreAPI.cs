using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CHEF.Components;

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

    public string VersionNumber()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IconUrl))
                return string.Empty;

            var fileName = IconUrl.Split('/').Last();
            var version = fileName.Split('-').Last();

            return version[..version.LastIndexOf('.')];
        }
        catch (Exception e)
        {
            Logger.Log(e);
        }

        return "";
    }
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
    public static string ApiUrlWithAuthor(string modAuthor) =>
        $"https://thunderstore.io/api/cyberstorm/listing/{RoR2Community}/{modAuthor}/?section={ModsSectionId}";

    public const string IsDownMessage = "Couldn't retrieve mod information, Thunderstore API is down. (Try again in 5-10 minutes)";
    public const string IsUpMessage = "The Thunderstore API is up.";

    private static readonly HttpClientHandler _httpClientHandler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    };
    private static readonly HttpClient _httpClient = new(_httpClientHandler);

    private static async Task<Package> GetModInfoCyberstormInternal(string url, string modName)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonConvert.DeserializeObject<ListingResponse>(json);

            var exactModNameMatch = response.Results.FirstOrDefault(p => p.Name.Equals(modName, StringComparison.InvariantCultureIgnoreCase));
            if (exactModNameMatch != null)
            {
                return exactModNameMatch;
            }
            return response.Results.FirstOrDefault();
        }
        catch (Exception e)
        {
            // The api call can fail.
            Logger.Log(e);
            return null;
        }
    }

    public static async Task<Package> GetModInfoCyberstorm(string modName)
    {
        return await GetModInfoCyberstormInternal($"{ApiUrl}&q={Uri.EscapeDataString(modName)}&nsfw=true&deprecated=true", modName);
    }

    public static async Task<Package> GetModInfoCyberstormWithTeam(string modTeam, string modName)
    {
        return await GetModInfoCyberstormInternal($"{ApiUrlWithAuthor(modTeam)}&q={Uri.EscapeDataString(modName)}&nsfw=true&deprecated=true", modName);
    }
}
