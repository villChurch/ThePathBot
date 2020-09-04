using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ThePathBot.Commands.SupportCommands
{
    public class TicketSystem : BaseCommandModule
    {
        [RequireOwner]
        [Command("ticket")]
        [Aliases("support")]
        [Description("Create a ticket to the mod team")]
        public async Task CreateSupportTicket(CommandContext ctx, [Description("Support type (server, queue, general)")] string supportType,
            [RemainingText, Description("Ticket Content")] string content)
        {
            supportType = supportType.ToLower();
            if (supportType != "server" || supportType != "queue" || supportType != "general")
            {
                await ctx.Channel.SendMessageAsync("This is not a valid support type. Valid types are server, queue or general").ConfigureAwait(false);
                return;
            }

        }
    }
}
