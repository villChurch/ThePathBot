using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using ThePathBot.Handlers.Dialogue.Steps;

namespace ThePathBot.Handlers.Dialogue
{
    public class DialogueHandler
    {
        private readonly DiscordClient _client;
        private readonly DiscordUser _user;
        private readonly DiscordChannel _channel;
        private IDialogueStep _currentStep;

        public DialogueHandler(
            DiscordClient client, DiscordChannel channel, DiscordUser user, IDialogueStep startingStep)
        {
            _client = client;
            _user = user;
            _channel = channel;
            _currentStep = startingStep;
        }

        private readonly List<DiscordMessage> messages = new List<DiscordMessage>();

        public async Task<bool> processDialogue()
        {
            while (_currentStep != null)
            {
                _currentStep.onMessageAdded += (message) => messages.Add(message);

                bool cancelled = await _currentStep.processStep(_client, _channel, _user);

                if (cancelled)
                {
                    await deleteMessages().ConfigureAwait(false);
                    var cancelEmbed = new DiscordEmbedBuilder
                    {
                        Title = "The Dialogue has successfully been cancelled",
                        Description = _user.Mention,
                        Color = DiscordColor.Green
                    };
                    await _channel.SendMessageAsync(embed: cancelEmbed).ConfigureAwait(false);
                    return false;
                }

                _currentStep = _currentStep.nextStep;
            }

            await deleteMessages().ConfigureAwait(false);
            return true;
        }

        private async Task deleteMessages()
        {
            if (_channel.IsPrivate)
            {
                return;
            }

            foreach (var message in messages)
            {
                await message.DeleteAsync().ConfigureAwait(false);
            }
        }
    }
}
