using System;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using ThePathBot.Models;

namespace ThePathBot.Commands.ACNHCommands
{
    public class Villager : BaseCommandModule
    {
        [Command ("villager")]
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
                ImageUrl = "http://williamspires.co.uk:9876/villagers/" + responseVillager.Filename + ".png"
            };
            villagerEmbed.AddField("Species", responseVillager.Species, true);
            villagerEmbed.AddField("Gender", responseVillager.Gender, true);
            villagerEmbed.AddField("Birthday", responseVillager.Birthday, true);
            villagerEmbed.AddField("Catchphrase", responseVillager.Catchphrase, true);
            villagerEmbed.AddField("Personality", responseVillager.Personality, true);

            await ctx.Channel.SendMessageAsync(embed: villagerEmbed).ConfigureAwait(false);
        }
    }
}
