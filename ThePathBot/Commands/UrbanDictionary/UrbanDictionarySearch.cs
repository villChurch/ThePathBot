using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using ThePathBot.Models;

namespace ThePathBot.Commands.UrbanDictionary
{
    public class UrbanDictionarySearch : BaseCommandModule
    {
        [Command("ud")]
        [Description("search urban dictionary")]
        public async Task searchUd(CommandContext ctx, params string[] searchTerm)
        {
            if (ctx.Guild.Id == 694013861560320081)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-oh!",
                    Description = "Sorry... This command cannot be run in this server.",
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }

            if (!ctx.Channel.IsNSFW && ctx.Guild.Id != 742472837901582486)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-Oh!",
                    Description = "Please be in a NSFW channel to run this command.",
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }

            var apiUrl = "https://api.urbandictionary.com/v0/define?term={0}";
            string udSearchTerm = string.Join(" ", searchTerm);
            var url = string.Format(apiUrl, udSearchTerm);
            var udTest = new Temperatures();

            using (var httpClient = new HttpClient())
            {
                try
                {
                    Task<HttpResponseMessage> getResponse = httpClient.GetAsync(url);
                    HttpResponseMessage response = await getResponse;
                    var responseJsonString = await response.Content.ReadAsStringAsync();
                    Console.Out.WriteLine(responseJsonString);
                    if (responseJsonString.Length < 1)
                    {
                        await ctx.Channel.SendMessageAsync("Could not locate this word, sorry.").ConfigureAwait(false);
                        return;
                    }
                    else if (responseJsonString.Contains("An error occurred"))
                    {
                        responseJsonString = await response.Content.ReadAsStringAsync();
                        if (responseJsonString.Contains("An error occurred"))
                        {
                            await ctx.Channel.SendMessageAsync("An error occured on UD api please try again").ConfigureAwait(false);
                        }
                    }
                    udTest = JsonConvert.DeserializeObject<Temperatures>(responseJsonString);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    await ctx.Channel.SendMessageAsync("Could not locate this word, sorry.").ConfigureAwait(false);
                    return;
                }
            }
            List<Page> udPages = new List<Page>();
            Page page = new Page();
            int counter = 1;
            foreach (List item in udTest.List)
            {
                page.Embed = new DiscordEmbedBuilder
                {
                    Title = udSearchTerm,
                    Color = DiscordColor.Blurple,
                    Description = item.Definition,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Page {counter}/{udTest.List.Length}"
                    }
                };
                udPages.Add(page);
                counter++;
                page = new Page();
            }

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, udPages)
                .ConfigureAwait(false);
        }
    }
}
