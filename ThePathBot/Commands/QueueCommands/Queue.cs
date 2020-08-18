using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace ThePathBot.Commands.QueueCommands
{
    public class Queue : BaseCommandModule
    {
        private bool timerRunning = false;
        private ulong msgId;

        [Command("create")]
        [Description("Create queue")]
        public async Task CreateQueue(CommandContext ctx)
        {
            if (ctx.Guild.Id == 694013861560320081)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-oh!",
                    Description = "Sorry... This command cannot be run in this server.",
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }
            //first we must create a private channel
            Guid guid = Guid.NewGuid();
            var newChannel = await ctx.Guild.CreateChannelAsync(guid.ToString(), DSharpPlus.ChannelType.Text, ctx.Guild.GetChannel(744273831602028645));
            //Then give the user access to the channel
            await newChannel.AddOverwriteAsync(ctx.Member, DSharpPlus.Permissions.AccessChannels);

            // Now we can get some information
            bool ready = false;
            var interactivity = ctx.Client.GetInteractivity();
            DiscordEmoji yes = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            DiscordEmoji no = DiscordEmoji.FromName(ctx.Client, ":x:");
            string turnipPrice = "0";
            while (!ready)
            {
                await newChannel.SendMessageAsync("Enter your Dodo Code").ConfigureAwait(false);

                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                string dodoCode = msg.Result.Content;

                await newChannel.SendMessageAsync("Enter your turnip price").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                turnipPrice = msg.Result.Content;

                await newChannel.SendMessageAsync("Enter session message for your guests").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                string message = msg.Result.Content;

                await newChannel.SendMessageAsync("Enter how many people you want per group (max of 7)").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                string maxGroupSize = msg.Result.Content;

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Turnip Session"
                };

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Dodo Code: {dodoCode}");
                sb.AppendLine($"Turnip Price: {turnipPrice}");
                sb.AppendLine($"Session Message: {message}");
                sb.AppendLine($"Group Size: {maxGroupSize}");
                sb.AppendLine($"If this information is correct press the :white_check_mark: otherwise press :x: to start again.");


                embed.Description = sb.ToString();

                var sentMessage = await newChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);

                await sentMessage.CreateReactionAsync(yes).ConfigureAwait(false);
                await sentMessage.CreateReactionAsync(no).ConfigureAwait(false);

                var response = await interactivity.WaitForReactionAsync(xe => xe.Emoji == yes || xe.Emoji == no,
                    sentMessage, ctx.User, TimeSpan.FromMinutes(1)).ConfigureAwait(false);

                if (response.Result.Emoji == yes)
                {
                    ready = true;
                }
            }
            var sessionEmbed = new DiscordEmbedBuilder
            {
                Title = "Your Queue",
                Description = "Some stuff about your queue"
            };

            sessionEmbed.AddField("On Island", "Some people");
            var dodoMsg = await newChannel.SendMessageAsync(embed: sessionEmbed).ConfigureAwait(false);

            CreateQueueEmbed(turnipPrice, ctx, newChannel);

            timerRunning = true;
            while (timerRunning)
            {
                await dodoMsg.CreateReactionAsync(yes).ConfigureAwait(false);
                await dodoMsg.CreateReactionAsync(no).ConfigureAwait(false);
                var reactionResult = await interactivity.WaitForReactionAsync(xe => xe.Emoji == yes || xe.Emoji == no,
                    dodoMsg, ctx.User, TimeSpan.FromHours(1)).ConfigureAwait(false);
                if (reactionResult.Result.Emoji == yes)
                {
                    await dodoMsg.DeleteReactionAsync(yes, ctx.User).ConfigureAwait(false);
                    await newChannel.SendMessageAsync($"{DateTime.Now}").ConfigureAwait(false);
                }
                else if (reactionResult.Result.Emoji == no)
                {
                    await dodoMsg.DeleteReactionAsync(no, ctx.User).ConfigureAwait(false);
                    timerRunning = false;
                    await newChannel.SendMessageAsync($"Stopped polling at {DateTime.Now}").ConfigureAwait(false);
                    var postChannel = ctx.Guild.GetChannel(744644693479915591);
                    await postChannel.DeleteMessageAsync(postChannel.GetMessageAsync(msgId).Result).ConfigureAwait(false);
                    await newChannel.DeleteAsync().ConfigureAwait(false);
                }
            }
        }

        private async void CreateQueueEmbed(string turnipPrice, CommandContext ctx, DiscordChannel channel)
        {
            var queueEmbed = new DiscordEmbedBuilder
            {
                Title = $"Nooks buying turnips for {turnipPrice}",
                Description = $"photo goes here"
            };
            var postChannel = ctx.Guild.GetChannel(744644693479915591);
            var queueMsg = await postChannel.SendMessageAsync(embed: queueEmbed).ConfigureAwait(false);
            msgId = queueMsg.Id;

            var interactivity = ctx.Client.GetInteractivity();
            DiscordEmoji reaction = DiscordEmoji.FromName(ctx.Client, ":ankha:");
            while (timerRunning)
            {
                await queueMsg.CreateReactionAsync(reaction).ConfigureAwait(false);
                var result = await interactivity.WaitForReactionAsync(xe => xe.Emoji == reaction && xe.Message == queueMsg
                && !xe.User.IsBot, TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                if (result.Result != null && result.Result.Emoji != null)
                {
                    if (result.Result.Emoji == reaction)
                    {
                        if (ctx.Guild.Channels.ContainsKey(channel.Id))
                        {
                            await channel.SendMessageAsync(
                                $"{result.Result.User.Username} has joined the queue").ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}