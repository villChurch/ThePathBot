using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace ThePathBot.Handlers.Dialogue.Steps
{
    public class ReactionStep : DialogueStepBase
    {

        private readonly Dictionary<DiscordEmoji, ReactionStepData> options;
        private DiscordEmoji selectedEmoji;
        private string title { get; set; }

        public ReactionStep(string title, string content, Dictionary<DiscordEmoji, ReactionStepData> options) : base(content)
        {
            this.options = options;
            this.title = title;
        }

        public override IDialogueStep nextStep => options[selectedEmoji].nextStep;

        public Action<DiscordEmoji> onValidResult { get; set; } = delegate { };

        public override async Task<bool> processStep(DiscordClient client, DiscordChannel channel, DiscordUser user)
        {
            var cancelEmoji = DiscordEmoji.FromName(client, ":x:");

            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = $"{title}",
                Description = $"{_content}",
            };

            embedBuilder.AddField("To Stop the Dialogue", "React with the :x: emoji");

            var interactivity = client.GetInteractivity();

            while (true)
            {
                var embed = await channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

                onMessageAdded(embed);
                foreach (var emoji in options.Keys)
                {
                    await embed.CreateReactionAsync(emoji).ConfigureAwait(false);
                }

                await embed.CreateReactionAsync(cancelEmoji).ConfigureAwait(false);

                var reactionResult = await interactivity.WaitForReactionAsync(
                    x => options.ContainsKey(x.Emoji) || x.Emoji == cancelEmoji,
                    embed, user).ConfigureAwait(false);

                if (reactionResult.Result.Emoji == cancelEmoji)
                {
                    return true;
                }

                selectedEmoji = reactionResult.Result.Emoji;

                onValidResult(selectedEmoji);
                return false;
            }
        }
    }

    public class ReactionStepData
    {
        public IDialogueStep nextStep { get; set; }
        public string Content { get; set; }
        public string Title { get; set; }
    }
}
