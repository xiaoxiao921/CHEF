using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CHEF.Components.Commands.Ignore;
using Discord.WebSocket;

namespace CHEF.Components.Watcher;

public class LogScan
{
    private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    };

    private static readonly HttpClient _httpClient = new(_httpClientHandler);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    internal async Task<string> Try(SocketMessage msg)
    {
        if (msg is SocketUserMessage &&
            msg.Channel is SocketTextChannel &&
            !msg.Author.IsBot &&
            !msg.Author.IsWebhook &&
            msg.Attachments.Count >= 1)
        {
            foreach (var attachment in msg.Attachments)
            {
                try
                {
                    var fileType = Path.GetExtension(new Uri(attachment.Url).AbsolutePath);

                    static bool IsValidLogFileExtension(string extension)
                    {
                        return extension == ".txt" || extension == ".log" || extension == ".zip";
                    }

                    const int FiftyMiB = 50 * 1024 * 1024;
                    const int AttachmentMaxFileSizeInBytes = FiftyMiB;
                    if (IsValidLogFileExtension(fileType))
                    {
                        if (attachment.Size <= AttachmentMaxFileSizeInBytes)
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
                                        if (zipArchiveEntry.Length > AttachmentMaxFileSizeInBytes)
                                        {
                                            Logger.Log($"Skipping log scanning for file {zipArchiveEntry.Name} because file size is {zipArchiveEntry.Length}, max is {AttachmentMaxFileSizeInBytes}");
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
                                    using var context = new IgnoreContext();
                                    if (!await context.IsIgnored(msg.Author))
                                    {
                                        Logger.Log("Scanning log content.");
                                        await CommonIssues.CheckForOutdatedAndDeprecatedMods(fileContent, botAnswer, msg.Author);
                                    }
                                    else
                                    {
                                        Logger.Log($"Message author {msg.Author} is ignored, not scanning attachment");
                                    }
                                }
                            }

                            return botAnswer.ToString();
                        }
                        else
                        {
                            return
                                "Log file too large for automatic analysis\n\n" +
                                $"• Size: {FormatBytes(attachment.Size)}\n" +
                                $"• Limit: {FormatBytes(AttachmentMaxFileSizeInBytes)}\n\n" +
                                "What you can do:\n" +
                                "- Relaunch the game and exit quickly (main menu is enough)\n" +
                                "- Wait for help! Someone may still help you.";
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString());
                }
            }
        }

        return string.Empty;
    }
}