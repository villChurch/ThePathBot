using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ThePathBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class FromChannel : CheckBaseAttribute
    {
        public ulong Channel { get; private set; }
        public FromChannel(ulong channel)
        {
            this.Channel = channel;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var currentChannelId = ctx.Channel.Id;

            return Task.FromResult(currentChannelId == Channel);
        }
    }
}
