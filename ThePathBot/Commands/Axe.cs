using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ThePathBot.Commands
{
    public class Axe : BaseCommandModule
    {
        string axeGif = "https://media.giphy.com/media/VJTqRrNcLXnSMrLXhE/giphy.gif";
        [Command("axe")]
        [Hidden]
        public async Task axeSomeone(CommandContext ctx, params string[] mentions)
        {
            if (ctx.Guild.Id != 742472837901582486)
            {
                return;
            }
            DiscordUser victim = ctx.Message.MentionedUsers[0];
            if (victim.IsBot)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "The Path cannot be axed",
                    ImageUrl = "https://media.giphy.com/media/K55exy0toWjQc/giphy.gif"
                };

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }
            DiscordEmbedBuilder axeEmbed = new DiscordEmbedBuilder
            {
                Title = ctx.Channel.Guild.GetMemberAsync(ctx.Message.Author.Id).Result.DisplayName
                + " axed " + ctx.Channel.Guild.GetMemberAsync(victim.Id).Result.DisplayName,
                ImageUrl = axeGif
            };

            await ctx.Channel.SendMessageAsync(embed: axeEmbed).ConfigureAwait(false);
        }
    }
}
