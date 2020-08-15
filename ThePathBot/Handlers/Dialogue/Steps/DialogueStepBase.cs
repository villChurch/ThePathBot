using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace ThePathBot.Handlers.Dialogue.Steps
{
    public abstract class DialogueStepBase : IDialogueStep
    {
        protected readonly string _content;

        public DialogueStepBase(string content)
        {
            _content = content;
        }
        public Action<DiscordMessage> onMessageAdded { get; set; } = delegate { };
        public abstract IDialogueStep nextStep { get; }
        public abstract Task<bool> processStep(DiscordClient client, DiscordChannel channel, DiscordUser user);

        protected async Task tryAgain(DiscordChannel channel, string problem)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Please try again",
                Color = DiscordColor.Red
            };

            embedBuilder.AddField("There was a problem with your previous input", problem);

            var embed = await channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
            onMessageAdded(embed);
        }
    }
}
