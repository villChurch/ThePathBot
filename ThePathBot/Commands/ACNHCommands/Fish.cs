using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public class Fish : BaseCommandModule
    {
        [Command("fishy")]
        [Description("Gets certian informaiton about a fishy")]
        public async Task getFish(CommandContext ctx, [Description("fish name")] params string[] args)
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
            var fish = String.Join(" ", args).ToLower();
            if (fish.ToLower() == "pointy bois".ToLower())
            {
                fish = "great white shark";
            }
            else if (fish.ToLower() == "great white shark".ToLower())
            {
                await ctx.Channel.SendMessageAsync("Think you mean pointy bois please try again").ConfigureAwait(false);
                return;
            }
            var template = "https://nooksinfo.com/fish/{0}";
            var url = string.Format(template, fish);
            FishModel responseFish = new FishModel();
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
                        await ctx.Channel.SendMessageAsync("Could not locate this fish, sorry.").ConfigureAwait(false);
                    }
                    responseFish = JsonConvert.DeserializeObject<FishModel>(responseJsonString);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    await ctx.Channel.SendMessageAsync("Could not locate this fish, sorry.").ConfigureAwait(false);
                }
            }

            Console.Out.WriteLine("http://williamspires.com/Fish/" + responseFish.CritterpediaFilename + ".png");
            var fishEmbed = new DiscordEmbedBuilder
            {
                Title = responseFish.Name,
                Color = DiscordColor.Blurple,
                ImageUrl = "http://williamspires.com/Fish/" + responseFish.CritterpediaFilename + ".png"
            };
            fishEmbed.AddField("Where To Find", responseFish.WhereOrHow, true);
            fishEmbed.AddField("Shadow Size", responseFish.Shadow, true);
            fishEmbed.AddField("Size", responseFish.Size, true);
            fishEmbed.AddField("Nook Sell Price", responseFish.Sell.ToString(), true);
            fishEmbed.AddField("Flick Sell Price", (responseFish.Sell * 1.5).ToString(CultureInfo.CurrentCulture), true);

            await ctx.Channel.SendMessageAsync(embed: fishEmbed).ConfigureAwait(false);
        }

        [Command("fish")]
        [Aliases("fa")]
        [Description("Get information on what fish are available that month in nh or sh")]
        public async Task FishInfoByMonth(CommandContext ctx, [Description("full month name, eg April")] string month, [RemainingText, Description("nh for north or sh for south")] string hemi)
        {
            try
            {
                month = month.First().ToString().ToUpper() + String.Join("", month.Skip(1));
                hemi = hemi.ToUpper().Trim();

                var template = "https://nooksinfo.com/fish/available/{0}/{1}";
                var url = string.Format(template, month, hemi);
                List<FishModel> responseFish = new List<FishModel>();
                using (var httpClient = new HttpClient())
                {

                    Task<HttpResponseMessage> getResponse = httpClient.GetAsync(url);
                    HttpResponseMessage response = await getResponse;
                    var responseJsonString = await response.Content.ReadAsStringAsync();
                    Console.Out.WriteLine(responseJsonString);
                    if (responseJsonString.Length < 1)
                    {
                        await ctx.Channel.SendMessageAsync("Could not find anything, sorry.").ConfigureAwait(false);
                    }
                    responseFish = JsonConvert.DeserializeObject<List<FishModel>>(responseJsonString);
                }

                List<Page> pages = new List<Page>();
                int count = 1;
                foreach (var fish in responseFish)
                {
                    Page page = new Page();
                    var fishEmbed = new DiscordEmbedBuilder
                    {
                        Title = fish.Name,
                        Color = DiscordColor.Blurple,
                        ImageUrl = "http://williamspires.com/Fish/" + fish.CritterpediaFilename + ".png",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Fish {count}/{responseFish.Count}"
                        }
                    };
                    fishEmbed.AddField("Where To Find", fish.WhereOrHow, true);
                    fishEmbed.AddField("Shadow Size", fish.Shadow, true);
                    fishEmbed.AddField("Size", fish.Size, true);
                    fishEmbed.AddField("Nook Sell Price", fish.Sell.ToString(), true);
                    fishEmbed.AddField("Flick Sell Price", (fish.Sell * 1.5).ToString(CultureInfo.CurrentCulture), true);
                    string monthTime = GetMonthParamName(month, fish, hemi);
                    if (monthTime.Equals("whoops"))
                    {
                        fishEmbed.AddField("Time", "Could not fetch time info", true);
                    }
                    else
                    {
                        fishEmbed.AddField("Time", monthTime, true);
                    }
                    page.Embed = fishEmbed;
                    page.Content = $"Fish Available in {month} in {hemi}";
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

        private string GetMonthParamName(string inputMonth, FishModel fishy, string hemi)
        {
            if (inputMonth.ToLower() == "january")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhJan;
                }
                else
                {
                    return fishy.ShJan;
                }
            }
            else if (inputMonth.ToLower() == "february")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhFeb;
                }
                else
                {
                    return fishy.ShFeb;
                }
            }
            else if (inputMonth.ToLower() == "march")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhMar;
                }
                else
                {
                    return fishy.ShMar;
                }
            }
            else if (inputMonth.ToLower() == "april")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhApr;
                }
                else
                {
                    return fishy.ShApr;
                }
            }
            else if (inputMonth.ToLower() == "may")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhMay;
                }
                else
                {
                    return fishy.ShMay;
                }
            }
            else if (inputMonth.ToLower() == "june")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhJun;
                }
                else
                {
                    return fishy.ShJun;
                }
            }
            else if (inputMonth.ToLower() == "july")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhJul;
                }
                else
                {
                    return fishy.ShJul;
                }
            }
            else if (inputMonth.ToLower() == "august")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhAug;
                }
                else
                {
                    return fishy.ShAug;
                }
            }
            else if (inputMonth.ToLower() == "september")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhSep;
                }
                else
                {
                    return fishy.ShSep;
                }
            }
            else if (inputMonth.ToLower() == "october")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhOct;
                }
                else
                {
                    return fishy.ShOct;
                }
            }
            else if (inputMonth.ToLower() == "november")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhNov;
                }
                else
                {
                    return fishy.ShNov;
                }
            }
            else if (inputMonth.ToLower() == "december")
            {
                if (hemi.ToLower() == "nh")
                {
                    return fishy.NhDec;
                }
                else
                {
                    return fishy.ShDec;
                }
            }
            else
            {
                return "whoops";
            }
        }
    }
}
