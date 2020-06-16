using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using PuppeteerSharp;

namespace CHEF.Components.Watcher
{
    public class ImageParser
    {
        private readonly string _postUrl;

        public ImageParser(string siteUrl = "https://yandex.com/")
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }

            _postUrl = siteUrl + "images/search?url=";
        }

        internal async Task<string> Try(SocketMessage msg)
        {
            if (msg is SocketUserMessage &&
                msg.Channel is SocketTextChannel &&
                !msg.Author.IsBot &&
                !msg.Author.IsWebhook &&
                //msg.Channel.Name.Equals("tech-support") &&
                msg.Attachments.Count == 1)
            {
                var attachment = msg.Attachments.First();
                {
                    var fileType = System.IO.Path.GetExtension(attachment.Url);
                    if (fileType == ".png")
                    {
                        var botAnswer = new StringBuilder();
                        var queryResult = await QueryYandex(attachment.Url);

                        var outdatedMods = await CommonIssues.CheckModsVersion(queryResult.ImageText);
                        if (outdatedMods != null)
                        {
                            botAnswer.AppendLine(
                                $"{msg.Author.Mention}, looks like you don't have the latest version installed of " +
                                $"the following mod{(outdatedMods.Contains('\n') ? "s" : "")} :" + Environment.NewLine +
                                outdatedMods);
                        }

                        if (queryResult.IsAConsole())
                        {
                            botAnswer.AppendLine($"{msg.Author.Mention}, looks like you just uploaded a screenshot of a BepinEx console / log file." +
                                                 Environment.NewLine + 
                                                 "Know that most of the time, a full log is way more useful for finding what your problem is.");
                            
                            if (msg.Content.Contains("crash", StringComparison.InvariantCultureIgnoreCase))
                            {
                                botAnswer.AppendLine("You also mentioned the word `crash` in your message." + Environment.NewLine + 
                                                     "Here is the file path for a log file that could be more useful to us :" + Environment.NewLine +
                                                     @"`C:\Users\<UserName>\AppData\LocalLow\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`" + Environment.NewLine +
                                                     "or" + Environment.NewLine + 
                                                     @"`C:\Users\< UserName >\AppData\Local\Temp\Hopoo Games, LLC\Risk of Rain 2\output_log.txt`");
                            }
                            else
                            {
                                botAnswer.AppendLine("You can find such log file in your `Risk of Rain 2/BepInEx/` folder, " +
                                                     "the file is called `LogOutput.log`.");
                            }

                            botAnswer.AppendLine("Drag the file in this channel so that other users can help you !");
                            return botAnswer.ToString();
                        }

                        if (queryResult.IsWindowExplorer() && 
                            (queryResult.ImageText.Contains("bepin", StringComparison.InvariantCultureIgnoreCase) || 
                             queryResult.ImageText.Contains("risk of rain", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            var channel = msg.Channel as SocketGuildChannel;
                            var faqChannel = channel.Guild.Channels.FirstOrDefault(guildChannel =>
                                guildChannel.Name.Equals("faq"));

                            botAnswer.AppendLine($"{msg.Author.Mention}, looks like you just uploaded a screenshot of a Windows Explorer in your game folder.");
                            if (faqChannel != null)
                            {
                                botAnswer.AppendLine(
                                    "If you are struggling installing BepInEx / R2API: " + Environment.NewLine + 
                                    $"There is a video and an image at the bottom of the <#{faqChannel}> that explains how to install them properly.");
                            }
                            botAnswer.AppendLine("If the issue is something else, just wait for help.");

                            return botAnswer.ToString();
                        }
                    }
                }
            }

            return string.Empty;
        }

        private async Task<YandexImageQuery> QueryYandex(string imageUrl)
        {
            var res = new YandexImageQuery();
            
            using (var page = await HeadlessBrowser.Chromium.NewPageAsync())
            {
                await page.GoToAsync($"{_postUrl}{imageUrl}&rpt=imageview");
                var tagsParent = await page.QuerySelectorAsync(".CbirItem.CbirTags");
                if (tagsParent != null)
                {
                    var tags = await tagsParent.QuerySelectorAllAsync(".Button2-Text");
                    await res.AddTags(tags);
                }

                var ocrParent = await page.QuerySelectorAsync(".CbirItem.CbirOcr");
                if (ocrParent != null)
                {
                    await page.WaitForSelectorAsync(".CbirOcr-TextBlock.CbirOcr-TextBlock_level_text");
                    var ocr = await page.QuerySelectorAllAsync(".CbirOcr-TextBlock.CbirOcr-TextBlock_level_text");
                    await res.AddText(ocr);
                }
            }

            return res;
        }

        public class YandexImageQuery
        {
            private readonly List<string> _imageTags;
            public string ImageText { get; private set; }

            public YandexImageQuery()
            {
                _imageTags = new List<string>();
            }

            public async Task AddTags(IEnumerable<ElementHandle> tags)
            {
                foreach (var tag in tags)
                {
                    _imageTags.Add(await tag.EvaluateFunctionAsync<string>("el => el.innerText"));
                }
            }

            public async Task AddText(IEnumerable<ElementHandle> elements)
            {
                var sb = new StringBuilder();
                foreach (var el in elements)
                {
                    sb.Append(await el.EvaluateFunctionAsync<string>("el => el.innerText"));
                    sb.Append(" ");
                }

                ImageText = sb.ToString();
            }

            public bool ContainTag(string tag) => _imageTags.Any(t => t.Contains(tag));

            public bool IsAConsole()
            {
                // yandex tags for console window screenshot:
                // screenshot with text: Экран с текстом , Скриншот с текстом
                // dark image: Темное изображение
                // black screen: черный экран

                return ContainTag("Экран с текстом") && !IsWindowExplorer();
            }

            public bool IsWindowExplorer()
            {
                // yandex tags for window explorer
                // computer files: файлы на компьютере
                // folders: папки
                // Screen with text: Экран с текстом
                // Windows 10
                // file: файл

                return ContainTag("файл") || 
                       ContainTag("папки") ||
                       ContainTag("файлы на компьютере");
            }
        }
    }
}
