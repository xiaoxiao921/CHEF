using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        private static readonly HttpClient _httpClient = new(_httpClientHandler);
        private readonly List<string> _siteUrls = new();
        private readonly List<string> _postUrls = new();

        public AutoPastebin(params string[] hastebinUrls)
        {
            foreach (var hastebinUrl in hastebinUrls)
            {
                var siteUrl = hastebinUrl;
                if (!hastebinUrl.EndsWith("/"))
                {
                    siteUrl += "/";
                }

                _siteUrls.Add(siteUrl);
                _postUrls.Add(siteUrl + "documents/");
            }
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
                var fileType = Path.GetExtension(attachment.Url);

                static bool IsPasteBinValidExtension(string extension)
                {
                    return extension == ".txt" || extension == ".log" || extension == ".cs";
                }

                static bool IsAttachmentValidExtension(string extension)
                {
                    return IsPasteBinValidExtension(extension) || extension == ".zip";
                }

                const int TenMiB = 10485760;
                const int attachmentMaxFileSizeInBytes = TenMiB;
                if (IsAttachmentValidExtension(fileType) && attachment.Size <= attachmentMaxFileSizeInBytes)
                {
                    var fileContentStream = await _httpClient.GetStreamAsync(attachment.Url);
                    var botAnswer = new StringBuilder();

                    string fileContent = null;
                    if (fileType == ".zip")
                    {
                        Logger.Log("Scanning zip attachment");
                        using (var zipArchive = new ZipArchive(fileContentStream))
                        {
                            foreach (var zipArchiveEntry in zipArchive.Entries)
                            {
                                Logger.Log($"Scanning zip entry: {zipArchiveEntry.FullName}");

                                var extension = Path.GetExtension(zipArchiveEntry.Name);
                                if (IsPasteBinValidExtension(extension))
                                {
                                    if (zipArchiveEntry.Length > attachmentMaxFileSizeInBytes)
                                    {
                                        Logger.Log($"Skipping log scanning for file {zipArchiveEntry.Name} because file size is {zipArchiveEntry.Length}, max is {attachmentMaxFileSizeInBytes}");
                                    }

                                    using var streamReader = new StreamReader(zipArchiveEntry.Open());
                                    fileContent = streamReader.ReadToEnd();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Log($"Putting file attachment content into a string");
                        using var streamReader = new StreamReader(fileContentStream);
                        fileContent = streamReader.ReadToEnd();
                    }

                    if (!string.IsNullOrWhiteSpace(fileContent))
                    {
                        using (var context = new IgnoreContext())
                        {
                            if (!await context.IsIgnored(msg.Author))
                            {
                                CommonIssues.CheckCommonLogError(fileContent, botAnswer, msg.Author);
                                await CommonIssues.CheckForOutdatedAndDeprecatedMods(fileContent, botAnswer, msg.Author);
                            }
                            else
                            {
                                Logger.Log($"Message author {msg.Author} is ignored, not scanning attachment");
                            }
                        }

                        var pasteResult = await PostBin(fileContent);

                        if (pasteResult.IsSuccess)
                        {
                            botAnswer.AppendLine(
                                $"Automatic pastebin for {msg.Author.Username} {attachment.Filename} file: <{pasteResult.FullUrl}>");
                        }
                        else
                        {
                            Logger.Log("Failed posting log to any hastebin endpoints");
                        }
                    }

                    return botAnswer.ToString();
                }
            }

            return string.Empty;
        }

        private async Task<HasteBinResult> PostBin(string content)
        {
            string siteUrl = "";
            HttpResponseMessage result = null;

            for (int i = 0; i < _siteUrls.Count; i++)
            {
                siteUrl = _siteUrls[i];
                var postUrl = _postUrls[i];

                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(postUrl))
                {
                    Content = new StringContent(content)
                };
                result = await _httpClient.SendAsync(request);

                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    var hasteBinResult = JsonConvert.DeserializeObject<HasteBinResult>(json);

                    if (hasteBinResult?.Key != null)
                    {
                        hasteBinResult.FullUrl = $"{siteUrl}{hasteBinResult.Key}";
                        hasteBinResult.IsSuccess = true;
                        hasteBinResult.StatusCode = HttpStatusCode.OK;
                        return hasteBinResult;
                    }
                }
            }

            return new HasteBinResult
            {
                FullUrl = siteUrl,
                IsSuccess = false,
                StatusCode = result != null ? result.StatusCode : HttpStatusCode.NotFound
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
