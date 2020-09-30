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

namespace ThePathBot.Commands.ACNHCommands
{
    public class Villager : BaseCommandModule
    {
        [Command("villager")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task getVillager(CommandContext ctx, params string[] villagerInput)
        {
            //if (ctx.Guild.Id == 694013861560320081)
            //{
            //    var embed = new DiscordEmbedBuilder
            //    {
            //        Title = "Wuh-oh!",
            //        Description = "Sorry... This command cannot be run in this server.",
            //        Color = DiscordColor.Blurple
            //    };
            //    await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            //    return;
            //}
            string villager = string.Join(" ", villagerInput);
            if (villager.ToLower() == "Lil J with the eyes".ToLower())
            {
                villager = "Judy";
            }
            villager = villager.ToUpperInvariant();
            var template = "https://nooksinfo.com/villager/{0}";
            var url = string.Format(template, villager);
            VillagerModel responseVillager = new VillagerModel();
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
                        await ctx.Channel.SendMessageAsync("Could not locate this villager, sorry.").ConfigureAwait(false);
                    }
                    responseVillager = JsonConvert.DeserializeObject<VillagerModel>(responseJsonString);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    await ctx.Channel.SendMessageAsync("Could not locate this villager, sorry.").ConfigureAwait(false);
                }
            }

            var villagerEmbed = new DiscordEmbedBuilder
            {
                Title = responseVillager.Name,
                Color = DiscordColor.Blurple,
                ImageUrl = "http://williamspires.com/villagers/" + responseVillager.Filename + ".png"
            };
            villagerEmbed.AddField("Species", responseVillager.Species, true);
            villagerEmbed.AddField("Gender", responseVillager.Gender, true);
            villagerEmbed.AddField("Birthday", responseVillager.Birthday, true);
            villagerEmbed.AddField("Catchphrase", responseVillager.Catchphrase, true);
            villagerEmbed.AddField("Personality", responseVillager.Personality, true);

            await ctx.Channel.SendMessageAsync(embed: villagerEmbed).ConfigureAwait(false);
        }

        [Command("personality")]
        [Description("Gets a list of villagers with that personaltiy")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task Personality(CommandContext ctx, [RemainingText, Description("personality to search for")] string personality)
        {
            try
            {
                List<VillagerModel> villagers = new List<VillagerModel>();
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://nooksinfo.com/villager/personality/{personality}";
                    Task<HttpResponseMessage> getResponse = httpClient.GetAsync(url);
                    HttpResponseMessage response = await getResponse;
                    var responseJsonString = await response.Content.ReadAsStringAsync();
                    Console.Out.WriteLine(responseJsonString);
                    if (responseJsonString.Length < 1)
                    {
                        await ctx.Channel.SendMessageAsync($"Could not find any villagers with personality of {personality}").ConfigureAwait(false);
                        return;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        await ctx.Channel.SendMessageAsync($"{response.Content.ReadAsStringAsync().Result}");
                        return;
                    }
                    villagers = JsonConvert.DeserializeObject<List<VillagerModel>>(responseJsonString);
                }
                List<Page> pages = new List<Page>();
                int count = 1;
                foreach (var villager in villagers)
                {
                    Page page = new Page();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = villager.Name,
                        Color = DiscordColor.Blurple,
                        ImageUrl = "http://williamspires.com/villagers/" + villager.Filename + ".png",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Villager {count}/{villagers.Count}"
                        }
                    };
                    embed.AddField("Species", villager.Species, true);
                    embed.AddField("Gender", villager.Gender, true);
                    embed.AddField("Birthday", villager.Birthday, true);
                    embed.AddField("Catchphrase", villager.Catchphrase, true);
                    page.Embed = embed;
                    page.Content = $"Villagers with {personality} personality";
                    pages.Add(page);
                    count++;
                }
                InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("birthday")]
        [Aliases("bday")]
        [Description("Gets todays birthdays using UTC time zone")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task Birthday(CommandContext ctx, [Description("optional: add a date in format {day-month} eg 23-01, if no date included uses todays date")] params string[] date)
        {
            string joinedDate = string.Join("", date).Trim();
            if (String.IsNullOrEmpty(joinedDate))
            {
                try
                {
                    List<VillagerModel> villagers = new List<VillagerModel>();
                    using (var httpClient = new HttpClient())
                    {
                        Task<HttpResponseMessage> getResponse = httpClient.GetAsync("https://nooksinfo.com/birthday/current");
                        HttpResponseMessage response = await getResponse;
                        var responseJsonString = await response.Content.ReadAsStringAsync();
                        Console.Out.WriteLine(responseJsonString);
                        if (responseJsonString.Length < 1)
                        {
                            await ctx.Channel.SendMessageAsync("Could not locate this villager, sorry.").ConfigureAwait(false);
                        }
                        villagers = JsonConvert.DeserializeObject<List<VillagerModel>>(responseJsonString);
                    }
                    List<Page> pages = new List<Page>();
                    int count = 1;
                    foreach (var villager in villagers)
                    {
                        Page page = new Page();
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = villager.Name,
                            Color = DiscordColor.Blurple,
                            ImageUrl = "http://williamspires.com/villagers/" + villager.Filename + ".png",
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = $"Birthday {count}/{villagers.Count} for {villager.Birthday} UTC"
                            }
                        };
                        embed.AddField("Species", villager.Species, true);
                        embed.AddField("Gender", villager.Gender, true);
                        embed.AddField("Birthday", villager.Birthday, true);
                        embed.AddField("Catchphrase", villager.Catchphrase, true);
                        page.Embed = embed;
                        page.Content = "Today's birthdays";
                        pages.Add(page);
                        count++;
                    }
                    InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                if (joinedDate.Contains("-"))
                {
                    try
                    {
                        List<VillagerModel> villagers = new List<VillagerModel>();
                        using (var httpClient = new HttpClient())
                        {
                            var url = $"https://nooksinfo.com/birthday/date/{joinedDate}";
                            Task<HttpResponseMessage> getResponse = httpClient.GetAsync(url);
                            HttpResponseMessage response = await getResponse;
                            var responseJsonString = await response.Content.ReadAsStringAsync();
                            Console.Out.WriteLine(responseJsonString);
                            if (responseJsonString.Length < 1)
                            {
                                await ctx.Channel.SendMessageAsync("Could not locate this villager, sorry.").ConfigureAwait(false);
                                return;
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                            {
                                await ctx.Channel.SendMessageAsync($"{response.Content.ReadAsStringAsync().Result}");
                                return;
                            }
                            villagers = JsonConvert.DeserializeObject<List<VillagerModel>>(responseJsonString);
                        }
                        List<Page> pages = new List<Page>();
                        int count = 1;
                        foreach (var villager in villagers)
                        {
                            Page page = new Page();
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = villager.Name,
                                Color = DiscordColor.Blurple,
                                ImageUrl = "http://williamspires.com/villagers/" + villager.Filename + ".png",
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    Text = $"Birthday {count}/{villagers.Count} for {villager.Birthday} UTC"
                                }
                            };
                            embed.AddField("Species", villager.Species, true);
                            embed.AddField("Gender", villager.Gender, true);
                            embed.AddField("Birthday", villager.Birthday, true);
                            embed.AddField("Catchphrase", villager.Catchphrase, true);
                            page.Embed = embed;
                            page.Content = $"{joinedDate} birthdays";
                            pages.Add(page);
                            count++;
                        }
                        InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("Looks like date was in an incorrect format").ConfigureAwait(false);
                }
            }
        }

        [Command("birthdayTomorrow")]
        [Aliases("bt", "tomorrow")]
        [Description("tomorrows birthdays utc")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task TomorrowsBirthdays(CommandContext ctx)
        {
            try
            {
                List<VillagerModel> villagers = new List<VillagerModel>();
                using (var httpClient = new HttpClient())
                {
                    Task<HttpResponseMessage> getResponse = httpClient.GetAsync("https://nooksinfo.com/birthday/tomorrow");
                    HttpResponseMessage response = await getResponse;
                    var responseJsonString = await response.Content.ReadAsStringAsync();
                    Console.Out.WriteLine(responseJsonString);
                    if (responseJsonString.Length < 1)
                    {
                        await ctx.Channel.SendMessageAsync("Could not locate this villager, sorry.").ConfigureAwait(false);
                    }
                    villagers = JsonConvert.DeserializeObject<List<VillagerModel>>(responseJsonString);
                }
                List<Page> pages = new List<Page>();
                int count = 1;
                foreach (var villager in villagers)
                {
                    Page page = new Page();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = villager.Name,
                        Color = DiscordColor.Blurple,
                        ImageUrl = "http://williamspires.com/villagers/" + villager.Filename + ".png",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Birthday {count}/{villagers.Count} for {villager.Birthday} UTC"
                        }
                    };
                    embed.AddField("Species", villager.Species, true);
                    embed.AddField("Gender", villager.Gender, true);
                    embed.AddField("Birthday", villager.Birthday, true);
                    embed.AddField("Catchphrase", villager.Catchphrase, true);
                    page.Embed = embed;
                    page.Content = "Tomorrows Birthdays";
                    pages.Add(page);
                    count++;
                }
                InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
