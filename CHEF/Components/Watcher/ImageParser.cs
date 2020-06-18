﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using HtmlAgilityPack;

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
                msg.Channel.Name.Equals("tech-support") &&
                msg.Attachments.Count == 1)
            {
                var attachment = msg.Attachments.First();
                {
                    var fileType = System.IO.Path.GetExtension(attachment.Url);
                    if (fileType == ".png")
                    {
                        Logger.Log("Preparing a query for Yandex.");

                        var botAnswer = new StringBuilder();
                        var queryResult = await QueryYandex(attachment.Url);

                        Logger.Log("Checking for outdated mods.");
                        var outdatedMods = await CommonIssues.CheckModsVersion(queryResult.ImageText);
                        if (outdatedMods != null)
                        {
                            botAnswer.AppendLine(
                                $"{msg.Author.Mention}, looks like you don't have the latest version installed of " +
                                $"the following mod{(outdatedMods.Contains('\n') ? "s" : "")} :" + Environment.NewLine +
                                outdatedMods);
                        }

                        Logger.Log("Checking if its a console image");
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

                        Logger.Log("Checking if its a window explorer image");
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

            var web = new HtmlWeb();
            var document = (await web.LoadFromWebAsync($"{_postUrl}{imageUrl}&rpt=imageview")).DocumentNode;
            var tags = document.SelectNodes("//*[@class=\"CbirItem CbirTags\"]//*[@class=\"Button2-Text\"]");
            res.AddTags(tags);
            Logger.Log("Got the image tags.");

            // Catch any error in case the Cloud Vision Service is
            // not correctly setup : An exception will be thrown
            // If the project doesnt have billing info correctly filled
            try
            {
                var img = Image.FromUri(imageUrl);
                res.AddText(CloudVisionOcr.AnnotatorClient.DetectText(img));
                Logger.Log("Got the image text : " + res.ImageText);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
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

            public void AddTags(HtmlNodeCollection tags)
            {
                foreach (var tag in tags)
                {
                    _imageTags.Add(tag.InnerText);
                }
            }

            /// <summary>
            /// For some reason the first entity contains all the entity with line break inbetween them.
            /// If you want to iterate on each line, start at 1, not 0
            /// </summary>
            /// <param name="entities"></param>
            public void AddText(IReadOnlyList<EntityAnnotation> entities)
            {
                ImageText = entities[0].Description;
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
