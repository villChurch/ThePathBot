using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ThePathBot.Handlers.Dialogue.Steps;

namespace ThePathBot.Commands.Admin
{
    public class Emoji : BaseCommandModule
    {
        const string baseURL = "https://cdn.discordapp.com/emojis/";
        private Dictionary<string, string> foundEmojis;
        private List<string> foundEmojisKeys;
        private Timer timer;
        private bool timerRunning = false;

        [Command("steal")]
        [Hidden]
        [RequirePermissions(DSharpPlus.Permissions.ManageEmojis)]
        public async Task stealEmoji(CommandContext ctx)
        {
            foundEmojis = new Dictionary<string, string>();
            IReadOnlyList<DiscordMessage> messageList = await ctx.Channel.GetMessagesAsync(50).ConfigureAwait(false);

            foreach (DiscordMessage message in messageList)
            {
                if (message.Content.ToLower() != ctx.Message.Content.ToLower())
                {
                    parseIDs(message.Content.ToString());
                }
            }
            List<Page> pages = new List<Page>();

            foreach (KeyValuePair<ulong, DiscordEmoji> emojiItem in ctx.Guild.Emojis)
            {
                if (foundEmojis.ContainsKey(emojiItem.Value.Name))
                {
                    foundEmojis.Remove(emojiItem.Value.Name);
                }
            }

            foundEmojisKeys = new List<string>(foundEmojis.Keys);

            int counter = 1;

            foreach (var item in foundEmojis)
            {
                try
                {
                    HttpClient client = new HttpClient
                    {
                        BaseAddress = new Uri(baseURL)
                    };

                    HttpResponseMessage response = await client.GetAsync(item.Value.Replace(baseURL, ""));

                    byte[] res = await response.Content.ReadAsByteArrayAsync();
                    Stream emojiImage = new MemoryStream(res);
                    Page page = new Page
                    {
                        Embed = new DiscordEmbedBuilder
                        {
                            Title = item.Key.Replace(":", ""),
                            ImageUrl = item.Value
                        }
                    };
                    pages.Add(page);
                    counter++;
                }
                catch (Exception)
                {
                    await ctx.Channel.SendMessageAsync("Error has occured, its likely an emoji with this name already existed").ConfigureAwait(false);
                }
            }
            int pageCounter = 0;
            var interactivity = ctx.Client.GetInteractivity();

            SetTimer();
            DiscordMessage msg = await ctx.Channel.SendMessageAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
            while (timerRunning)
            {
                await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":arrow_backward:")).ConfigureAwait(false);
                await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":arrow_forward:")).ConfigureAwait(false);
                await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":detective:")).ConfigureAwait(false);

                List<string> options = new List<string>
                {
                    ":arrow_backward:",
                    ":arrow_forwad:",
                    ":detective:"
                };

                DiscordEmoji backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                DiscordEmoji forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                DiscordEmoji detective = DiscordEmoji.FromName(ctx.Client, ":detective:");

                InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> reactionResult = await interactivity.WaitForReactionAsync(
                    xe => xe.Emoji == backward || xe.Emoji == forward || xe.Emoji == detective, msg, ctx.User,
                    TimeSpan.FromSeconds(120)).ConfigureAwait(false);

                if (reactionResult.Result.Emoji == backward)
                {
                    if ((pageCounter - 1) < 0)
                    {
                        pageCounter = pages.Count - 1;
                    }
                    else
                    {
                        pageCounter--;
                    }
                    await msg.DeleteReactionAsync(backward, ctx.User).ConfigureAwait(false);
                    await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                }
                else if (reactionResult.Result.Emoji == forward)
                {
                    if ((pageCounter + 1) >= pages.Count)
                    {
                        pageCounter = 0;
                    }
                    else
                    {
                        pageCounter++;
                    }
                    await msg.DeleteReactionAsync(forward, ctx.User).ConfigureAwait(false);
                    await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                }
                else if (reactionResult.Result.Emoji == detective)
                {
                    try
                    {
                        HttpClient client = new HttpClient
                        {
                            BaseAddress = new Uri(baseURL)
                        };

                        HttpResponseMessage response = await client.GetAsync(foundEmojis[pages[pageCounter].Embed.Title].Replace(baseURL, ""));

                        byte[] res = await response.Content.ReadAsByteArrayAsync();
                        Stream emojiImage = new MemoryStream(res);

                        await ctx.Guild.CreateEmojiAsync(pages[pageCounter].Embed.Title, emojiImage).ConfigureAwait(false);
                        await ctx.Channel.SendMessageAsync(
                            $"Added emoji called {pages[pageCounter].Embed.Title}")
                            .ConfigureAwait(false);

                        pages.RemoveAt(pageCounter);

                        if (pages.Count >= 1)
                        {
                            await msg.DeleteReactionAsync(detective, ctx.User).ConfigureAwait(false);
                            await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        }
                        else
                        {
                            await msg.DeleteAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                }
            }
            await msg.DeleteAsync().ConfigureAwait(false);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
        }

        private void SetTimer()
        {
            timer = new Timer(120000); // set timer for two minutes to match interactivity time span
            timer.Elapsed += FinishTimer;
            timer.Enabled = true;
            timerRunning = true;
        }

        private void FinishTimer(Object source, ElapsedEventArgs e)
        {
            timerRunning = false;
            timer.Stop();
            timer.Dispose();
        }

        private void parseIDs(string text)
        {
            MatchCollection parsedEmojis;
            if (text.Contains("<a:"))
            {
                parsedEmojis = Regex.Matches(text, @"<a:.+?:\d+>");
            }
            else
            {
                parsedEmojis = Regex.Matches(text, @"<:.+?:\d+>");
            }
            foreach (var thing in parsedEmojis)
            {
                var emoji = thing.ToString();
                string name = Regex.Match(emoji, @":[A-Za-z0-9_]+:").ToString().Replace(":", "");
                //emoji.match(/:[a-z0 - 9_]+:/ gi)[0].substr(1).slice(0, -1); //match(/:[a-z0 - 9_]+:/ gi)

                var id = Regex.Matches(emoji, @":[0-9]+>")[0].ToString().Replace(":", "").Replace(">","");
                //emoji.match(/:[0 - 9]+>/ gi)[0].substr(1).slice(0, -1);

                var gif = Regex.IsMatch(emoji, "<a:");
                //(emoji.match(/< a:/ gi)?true:false);

                var url = baseURL + id + (gif ? ".gif" : ".png");

                //baseURL + id + (gif ? ".gif" : ".png");
                if (!foundEmojis.ContainsKey(name))
                {
                    foundEmojis.Add(name, url);
                }
            }
        }
    }
}
