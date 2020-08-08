using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
namespace ThePathBot.Commands
{
    public class UtilityCommands : BaseCommandModule
    {
        private string botLink =
            "https://discord.com/oauth2/authorize?client_id=741036620912001045&permissions=523328&scope=bot";
        [Command("invite")]
        [Description("Give an invite link for this bot")]
        public async Task getInviteLink(CommandContext ctx)
        {
            var owner = await ctx.Guild.GetMemberAsync(272151652344266762);
            var inviteEmbed = new DiscordEmbedBuilder
            {
                Title = "Inivte The Path to your server",
                Color = DiscordColor.CornflowerBlue,
                Url = botLink,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Made by " + owner.Nickname
                }
            };
            await ctx.Channel.SendMessageAsync(embed: inviteEmbed).ConfigureAwait(false);
        }

        [Command("testphoto")]
        [Hidden]
        public async Task testPhotoUpload(CommandContext ctx)
        {
            var uploads = ctx.Message.Attachments;

            foreach (var upload in uploads)
            {
                await ctx.Channel.SendMessageAsync(upload.Url).ConfigureAwait(false);
            }
        }
    }
}
