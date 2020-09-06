using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using ThePathBot.Attributes;

namespace ThePathBot.Commands.Admin
{
    [Group("info")]
    public class GeneralInfoCommands : BaseCommandModule
    {

        [Command("server")]
        [Description("Returns information about the current server")]
        [OwnerOrPermission(DSharpPlus.Permissions.Administrator)]
        public async Task ServerInfo(CommandContext ctx)
        {
            var serverInfoEmbed = new DiscordEmbedBuilder
            {
                Title = ctx.Guild.Name,
                Color = DiscordColor.Aquamarine,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = ctx.Message.CreationTimestamp.ToString()
                }
            };
            var memberList = await ctx.Guild.GetAllMembersAsync();
            serverInfoEmbed.AddField("Server Owner", ctx.Guild.Owner.DisplayName, false);
            serverInfoEmbed.AddField("Members", memberList.Count.ToString(), false);
            serverInfoEmbed.AddField("Boosters", ctx.Guild.PremiumSubscriptionCount.ToString(), false);
            serverInfoEmbed.AddField("Created On", ctx.Guild.CreationTimestamp.ToString(), false);

            await ctx.Channel.SendMessageAsync(embed: serverInfoEmbed).ConfigureAwait(false);
        }

        [Command("user"), Description("Returns info about a user"), OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task UserInfo(CommandContext ctx, DiscordMember member)
        {
            if (member.IsBot)
            {
                await ctx.Channel.SendMessageAsync("Cannot run this against a bot").ConfigureAwait(false);
            }
            var memberInfoEmbed = new DiscordEmbedBuilder
            {
                Title = member.DisplayName,
                Color = DiscordColor.Aquamarine,
                ImageUrl = member.AvatarUrl,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = ctx.Message.CreationTimestamp.ToString()
                }
            };
            string boosting = member?.PremiumSince.HasValue == true ? "Yes" : "No";
            memberInfoEmbed.AddField("Username", member.Username, false);
            memberInfoEmbed.AddField("Nickname", member.Nickname, false);
            memberInfoEmbed.AddField("Joined on", member.JoinedAt.ToString(), false);
            memberInfoEmbed.AddField("Boosting Server", boosting, false);

            await ctx.Channel.SendMessageAsync(embed: memberInfoEmbed).ConfigureAwait(false);
        }
    }
}
