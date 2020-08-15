using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using ThePathBot.Models;

namespace ThePathBot.Commands.ACNHCommands
{
    public class Fish : BaseCommandModule
    {
        [Command("fishy")]
        public async Task getFish(CommandContext ctx, params string[] args)
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

            Console.Out.WriteLine("http://williamspires.co.uk:9876/Fish/" + responseFish.CritterpediaFilename + ".png");
            var fishEmbed = new DiscordEmbedBuilder
            {
                Title = responseFish.Name,
                Color = DiscordColor.Blurple,
                ImageUrl = "http://williamspires.co.uk:9876/Fish/" + responseFish.CritterpediaFilename + ".png"
            };
            fishEmbed.AddField("Where To Find", responseFish.WhereOrHow, true);
            fishEmbed.AddField("Shadow Size", responseFish.Shadow, true);
            fishEmbed.AddField("Size", responseFish.Size, true);
            fishEmbed.AddField("Nook Sell Price", responseFish.Sell.ToString(), true);
            fishEmbed.AddField("Flick Sell Price", (responseFish.Sell * 1.5).ToString(CultureInfo.CurrentCulture), true);

            await ctx.Channel.SendMessageAsync(embed: fishEmbed).ConfigureAwait(false);
        }
    }
}
