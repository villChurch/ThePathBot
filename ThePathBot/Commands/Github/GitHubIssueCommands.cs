using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using ThePathBot.Models;

namespace ThePathBot.Commands.Github
{
    [Group("issue")]
    public class GitHubIssueCommands : BaseCommandModule
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        ConfigJson configJson = new ConfigJson();

        private void SetConfigInfo()
        {
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
        }

        [Command("create")]
        [Aliases("add")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        [Description("Creates a DM session to Submit an issue with path bot")]
        public async Task IssueCommand(CommandContext ctx)
        {
            try
            {
                SetConfigInfo();
                DiscordMember member = ctx.Member;
                var interactivity = ctx.Client.GetInteractivity();
                var dmChannel = await member.CreateDmChannelAsync();
                await dmChannel.SendMessageAsync("Enter a title for your issue");
                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == dmChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await dmChannel.SendMessageAsync("Dialogue has timed out please rerun the command").ConfigureAwait(false);
                    return;
                }

                if (msg.Result.Content == null)
                {
                    await dmChannel.SendMessageAsync("You did not enter a title please rerun the command").ConfigureAwait(false);
                    return;
                }

                string title = $"{msg.Result.Content} from {ctx.Member.Username} with id {ctx.Member.Id}";
                string description;

                await dmChannel.SendMessageAsync("Please describe the issue");
                msg = await interactivity.WaitForMessageAsync(x => x.Channel == dmChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await dmChannel.SendMessageAsync("Dialogue has timed out please rerun the command").ConfigureAwait(false);
                    return;
                }

                if (msg.Result.Content == null)
                {
                    description = $"User {ctx.Member.Username} with id {ctx.Member.Id} did not enter a description";
                }
                else
                {
                    description = msg.Result.Content;
                }

                GithubIssue issue = new GithubIssue();
                issue.title = title;
                issue.body = description;
                string content = "";
                JsonSerializer serializer = new JsonSerializer();
                try
                {
                    content = JsonConvert.SerializeObject(issue);
                }
                catch (JsonReaderException)
                {
                    Console.WriteLine("Invalid JSON.");
                    return;
                }
                Console.Out.WriteLine(content);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", configJson.gitHubToken);
                client.DefaultRequestHeaders.Add("User-Agent", "Path Bot Discord bot");
                var postRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/repos/villChurch/ThePathBot/issues")
                {
                    Content = new StringContent(content)
                };
                var postResponse = await client.SendAsync(postRequest);

                var responseString = await postResponse.Content.ReadAsStringAsync();
                Console.Out.WriteLine(responseString);
                postResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("Error occured while running this command").ConfigureAwait(false);
            }
        }

        [Command("show")]
        [Aliases("list")]
        [Cooldown(10, 60, CooldownBucketType.Global)]
        [Description("Shows the current issues/enhancements for Path Bot")]
        public async Task ShowIssues(CommandContext ctx)
        {
            try
            {
                SetConfigInfo();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", configJson.gitHubToken);
                client.DefaultRequestHeaders.Add("User-Agent", "Path Bot Discord bot");
                var httpResponse = await client.GetAsync("https://api.github.com/repos/villChurch/ThePathBot/issues", HttpCompletionOption.ResponseHeadersRead);
                httpResponse.EnsureSuccessStatusCode();
                if (httpResponse.Content is object && httpResponse.Content.Headers.ContentType.MediaType == "application/json")
                {
                    var contentStream = await httpResponse.Content.ReadAsStreamAsync();

                    var streamReader = new StreamReader(contentStream);
                    var jsonReader = new JsonTextReader(streamReader);

                    JsonSerializer serializer = new JsonSerializer();
                    List<GithubIssueResponse> issues;

                    try
                    {
                        issues = serializer.Deserialize<List<GithubIssueResponse>>(jsonReader);

                        List<Page> issuePages = new List<Page>();
                        Page page = new Page();
                        int counter = 1;
                        foreach (var issue in issues)
                        {
                            page.Embed = new DiscordEmbedBuilder
                            {
                                Title = issue.Title,
                                Description = issue.Body,
                                Color = DiscordColor.Aquamarine,
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"Issue {counter}/{issues.Count} is currently {issue.State}"
                                }
                            };
                            counter++;
                            issuePages.Add(page);
                            page = new Page();
                        }
                        var interactivity = ctx.Client.GetInteractivity();
                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, issuePages).ConfigureAwait(false);
                    }
                    catch (JsonReaderException)
                    {
                        Console.WriteLine("Invalid JSON.");
                        await ctx.Channel.SendMessageAsync("Github has returned invalid JSON").ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("HTTP Response was invalid and cannot be deserialised.");
                    await ctx.Channel.SendMessageAsync("Error receiving data from Github").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("Error occured while running this command").ConfigureAwait(false);
            }
        }
    }
}
