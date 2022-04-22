using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CHEF.Components.Commands.Ignore;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CHEF.Components.Watcher
{
    public class AutoPastebin
    {
        private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        private static readonly HttpClient _httpClient = new HttpClient(_httpClientHandler);
        private readonly string _siteUrl;
        private readonly string _postUrl;

        public AutoPastebin(string siteUrl = "https://www.toptal.com/developers/hastebin/")
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }
            _siteUrl = siteUrl;
            _postUrl = siteUrl + "documents/";
        }

        internal async Task<string> Try(SocketMessage msg)
        {
            if (msg is SocketUserMessage &&
                msg.Channel is SocketTextChannel &&
                !msg.Author.IsBot &&
                !msg.Author.IsWebhook &&
                msg.Attachments.Count == 1)
            {
                var attachment = msg.Attachments.First();
                {
                    var fileType = System.IO.Path.GetExtension(attachment.Url);
                    if ((fileType == ".txt" || fileType == ".log" || fileType == ".cs") && attachment.Size < 1000000)
                    {
                        var fileContent = await _httpClient.GetStringAsync(attachment.Url);
                        var botAnswer = new StringBuilder();

                        using (var context = new IgnoreContext())
                        {
                            if (!await context.IsIgnored(msg.Author))
                            {
                                CommonIssues.CheckCommonLogError(fileContent, botAnswer, msg.Author);
                                await CommonIssues.CheckModsVersion(fileContent, botAnswer, msg.Author);
                            }
                        }

                        var pasteResult = await PostBin(fileContent);

                        if (pasteResult.IsSuccess)
                        {
                            botAnswer.AppendLine(
                                $"Automatic pastebin for {msg.Author.Username} {attachment.Filename} file: {pasteResult.FullUrl}");
                            return botAnswer.ToString();
                        }
                    }
                }
            }

            return string.Empty;
        }

        private async Task<HasteBinResult> PostBin(string content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_postUrl))
            {
                Content = new StringContent(content)
            };
            var result = await _httpClient.SendAsync(request);

            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                var hasteBinResult = JsonConvert.DeserializeObject<HasteBinResult>(json);

                if (hasteBinResult?.Key != null)
                {
                    hasteBinResult.FullUrl = $"{_siteUrl}{hasteBinResult.Key}";
                    hasteBinResult.IsSuccess = true;
                    hasteBinResult.StatusCode = HttpStatusCode.OK;
                    return hasteBinResult;
                }
            }

            return new HasteBinResult
            {
                FullUrl = _siteUrl,
                IsSuccess = false,
                StatusCode = result.StatusCode
            };
        }
    }

    public class HasteBinResult
    {
        public string Key { get; set; }
        public string FullUrl { get; set; }
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
