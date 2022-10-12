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
using Discord;
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

                const int FiftyMiB = 52428800;
                const int attachmentMaxFileSizeInBytes = FiftyMiB;
                if (IsAttachmentValidExtension(fileType))
                {
                    if (attachment.Size <= attachmentMaxFileSizeInBytes)
                    {
                        var fileContentStream = await _httpClient.GetStreamAsync(attachment.Url);
                        var botAnswer = new StringBuilder();

                        List<string> fileContents = new();
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
                                            continue;
                                        }

                                        Logger.Log("Reading zip entry into a string");
                                        using var entryStream = zipArchiveEntry.Open();
                                        using var streamReader = new StreamReader(entryStream);
                                        fileContents.Add(streamReader.ReadToEnd());
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Log($"Putting file attachment content into a string");
                            using var streamReader = new StreamReader(fileContentStream);
                            fileContents.Add(streamReader.ReadToEnd());
                        }

                        foreach (var fileContent in fileContents)
                        {
                            if (!string.IsNullOrWhiteSpace(fileContent))
                            {
                                var alreadyPostedBin = false;

                                using (var context = new IgnoreContext())
                                {
                                    if (!await context.IsIgnored(msg.Author))
                                    {
                                        Logger.Log("Scanning log content for common issues");
                                        CommonIssues.CheckCommonLogError(fileContent, botAnswer, msg.Author);
                                        await CommonIssues.CheckForOutdatedAndDeprecatedMods(fileContent, botAnswer, msg.Author);

                                        var noDuplicateFileContent = CommonIssues.RemoveDuplicateExceptionsFromText(fileContent);

                                        _ = Task.Run(() => PostBin(msg, attachment, noDuplicateFileContent));
                                        alreadyPostedBin = true;
                                    }
                                    else
                                    {
                                        Logger.Log($"Message author {msg.Author} is ignored, not scanning attachment");
                                    }
                                }

                                if (!alreadyPostedBin)
                                    _ = Task.Run(() => PostBin(msg, attachment, fileContent));
                            }
                        }

                        return botAnswer.ToString();
                    }
                    else
                    {
                        return "That file is quite large, if you want it to be scanned by CHEF for common log errors, please relaunch the game and exit as soon as you hit the main menu screen, it will prevent the file from getting a lot bigger than needed.";
                    }
                }
            }

            return string.Empty;
        }

        private async Task PostBin(SocketMessage msg, Attachment attachment, string fileContent)
        {
            Logger.Log("Trying to post file content to pastebin endpoints");
            var postTimeout = TimeSpan.FromSeconds(15);
            try
            {
                var pasteResult = await PostBinInternal(fileContent).WaitAsync(postTimeout);
                if (pasteResult.IsSuccess)
                {
                    var answer = $"Automatic pastebin for {msg.Author.Username} {attachment.Filename} file: <{pasteResult.FullUrl}>";
                    Logger.Log(answer);
                    await msg.Channel.SendMessageAsync(answer);
                }
                else
                {
                    Logger.Log("Failed posting log to any hastebin endpoints");
                }
            }
            catch (Exception e)
            {
                if (e is HttpRequestException)
                {
                    Logger.Log($"Failed posting log to any hastebin endpoints within a reasonable amount of time ({postTimeout})");
                }
                else
                {
                    Logger.Log(e.ToString());
                }
            }
        }

        private async Task<HasteBinResult> PostBinInternal(string content)
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
