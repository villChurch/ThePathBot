using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace ThePathBot.Handlers.Dialogue.Steps
{
    public interface IDialogueStep
    {
        Action<DiscordMessage> onMessageAdded { get; set; }
        IDialogueStep nextStep { get; }
        Task<bool> processStep(DiscordClient client, DiscordChannel channel, DiscordUser user);
    }
}
