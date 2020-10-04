using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ThePathBot.Commands.SupportCommands
{
    public class CodeShare : BaseCommandModule
    {
        [Command("CodeShare")]
        [Description("Show information on code sharing")]
        [Cooldown(5, 60, CooldownBucketType.Channel)]
        public async Task ShowCodeShare(CommandContext ctx)
        {
            DiscordEmoji starFrag = DiscordEmoji.FromName(ctx.Client, ":70starfrag:");
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Code Sharing",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Add your information", $"{starFrag} Have a dream address you want to share? Want to share your friend code or Nook Exchange wishlist? " +
                $"Fill out the following Google Form to share this info with fellow server members: https://forms.gle/jVihwvjYmrGsRXT8A {starFrag}", false);
            embed.AddField("View others information", $"{starFrag} Use the following link to view other's codes: " +
                $"https://docs.google.com/spreadsheets/d/1bqmcEvqzvV2UlaYDcwexexfwGQ9VSx2HVDMUng6bgsg/edit?usp=sharing {starFrag}", false);
            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }
    }
}
