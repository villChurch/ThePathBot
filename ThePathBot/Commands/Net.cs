using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ThePathBot.Commands
{
    public class Net : BaseCommandModule
    {
        private string netGif = "https://nxcache.nexon.net/spotlight/26/00J0k-0e47f12b-5d28-4831-9946-01c2abf9ae16.gif";

        [Command("net")]
        [Hidden]
        public async Task netWhack(CommandContext ctx, string victim)
        {
            IReadOnlyList<DiscordUser> mentions = ctx.Message.MentionedUsers;

            DiscordUser author = ctx.Message.Author;

            foreach(DiscordUser user in mentions)
            {
                var victimUser = ctx.Guild.GetMemberAsync(user.Id).Result.Nickname;
                if (string.Empty == victimUser || victimUser == null)
                {
                    victimUser = ctx.Guild.GetMemberAsync(user.Id).Result.DisplayName;
                }
                var assulter = ctx.Guild.GetMemberAsync(author.Id).Result.Nickname;
                if (string.Empty == assulter || assulter == null)
                {
                    assulter = ctx.Guild.GetMemberAsync(author.Id).Result.DisplayName;
                }
                var whackEmbed = new DiscordEmbedBuilder
                {
                    Title = assulter + " whacked " +
                    victimUser + " with a net",
                    ImageUrl = netGif,
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: whackEmbed).ConfigureAwait(false);
            }

        }
    }
}
