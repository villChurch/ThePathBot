﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using ThePathBot.Models;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.QueueCommands
{
    public class Queue : BaseCommandModule
    {
        private readonly ulong privateChannelGroup = 745024494464270448; //test 761508425888038923
        private readonly ulong turnipPostChannel = 744733259748999270; //test 761508382108418098
        private readonly ulong daisyMaeChannel = 744733207148232845; //test 761508362097131571
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();
        private Timer msgDestructTimer;
        string turnipPrice = "0";
        string attachment = "";
        string maxGroupSize = "0";
        string dodoCode = "";
        string message = "Welcome";
        bool isDaisy = false;
        bool timedOut = false;
        private readonly DiscordEmbedBuilder sessionEmbed = new DiscordEmbedBuilder
        {
            Title = "Your Queue has been created",
            Description = "To see whos in the queue run ```?showqueue ``` To send the next group of people run ```?sendcode```" +
                "To change your dodocode run ```?updatedodo``` To kick someone from your queue run " +
            "```?kick groupNumber positionIngroup``` To end your session run ```?endqueue``` To pause your queue run ```?pause```" +
            "To resume your queue run ```?resume``` To reshow this help message run ```?queuehelp```",
            Color = DiscordColor.Blurple
        };

        private void StartTimer(DiscordClient client, ulong guildId, ulong channelId, ulong messageId)
        {
            msgDestructTimer = new Timer
            {
                Interval = 5000 // 5 seconds
            };
            msgDestructTimer.Elapsed += (sender, e) => DestructMessage(sender, e, client, guildId, channelId, messageId);
            msgDestructTimer.AutoReset = true;
            msgDestructTimer.Enabled = true;
        }

        private async void DestructMessage(object source, ElapsedEventArgs e, DiscordClient client, ulong guildId, ulong channelId, ulong messageId)
        {
            msgDestructTimer.Stop();
            msgDestructTimer.Dispose();
            msgDestructTimer.Enabled = false;
            try
            {
                DiscordGuild guild = await client.GetGuildAsync(guildId);
                DiscordChannel channel = guild.GetChannel(channelId);
                var msg = await channel.GetMessageAsync(messageId);
                await msg.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("place")]
        [Aliases("check")]
        [Description("Check your place in a queue")]
        public async Task CheckPlaceInQueue(CommandContext ctx, [Description("code of queue")] string queueCode)
        {
            try
            {
                var channelId = GetPrivateChannelFromCode(queueCode);
                var timeJoined = "";
                var groupSize = GetMaxVisitorsAtOnceFromCode(queueCode);
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select TimeJoined from pathQueuers Where DiscordID = ?discordID AND queueChannelID = ?channelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        timeJoined = reader.GetString("TimeJoined");
                    }
                }

                if (string.IsNullOrEmpty(timeJoined))
                {
                    await ctx.Channel.SendMessageAsync("Looks like you are not part of a queue");
                }
                else
                {
                    //02/10/2020 12:36:54
                    Console.Out.WriteLine($"time joined ===============> {timeJoined}");
                    DateTime time = DateTime.Parse(timeJoined);
                    int numberInfront = 0;
                    using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Select * from pathQueuers Where queueChannelID = ?channelId AND TimeJoined <= ?timeJoined " +
                            "and visited = 0 and DiscordID <> ?discordId AND onIsland = 0";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                        command.Parameters.Add("?timeJoined", MySqlDbType.VarChar).Value = time.ToString("yyyy-MM-dd HH:mm:ss");
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                        connection.Open();
                        var reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                numberInfront++;
                            }
                        }
                    }

                    if (numberInfront == 0)
                    {
                        await ctx.Channel.SendMessageAsync("Looks like you are next up in the queue!");
                    }
                    else
                    {
                        int yourQueue = ((numberInfront - 1) / groupSize) + 1;
                        string queue = yourQueue == 1 ? "queue" : "queues";
                        await ctx.Channel.SendMessageAsync(
                            $"There is currently {yourQueue} {queue} ahead of you and the queue size is {groupSize} at a time").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

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
            else
            {
                try
                {
                    //first we must create a private channel
                    Guid guid = Guid.NewGuid();
                    var newChannel = await ctx.Guild.CreateChannelAsync(guid.ToString(),
                        ChannelType.Text, ctx.Guild.GetChannel(privateChannelGroup));
                    //Then give the user access to the channel
                    await newChannel.AddOverwriteAsync(ctx.Member, Permissions.AccessChannels);
                    await QueueDialogue(ctx, newChannel, false);
                    if (!timedOut)
                    {
                        var dodoMsg = await newChannel.SendMessageAsync(embed: sessionEmbed).ConfigureAwait(false);

                        CreateQueueEmbed(turnipPrice, ctx, newChannel, attachment, maxGroupSize, dodoCode, message, isDaisy);
                    }
                    else
                    {
                        var message = await newChannel.SendMessageAsync("Your queue creation timed out. React with :thumbsup: to restart or :thumbsdown: to end." +
                            " This message will time out in 4 minutes and end automatically if not.").ConfigureAwait(false);
                        DiscordEmoji thumbsup = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                        DiscordEmoji thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
                        await message.CreateReactionAsync(thumbsup);
                        await message.CreateReactionAsync(thumbsDown);
                        var interactivity = ctx.Client.GetInteractivity();
                        var result = await interactivity.WaitForReactionAsync(react => (react.Emoji == thumbsup ||
                        react.Emoji == thumbsDown) && react.User == ctx.User, TimeSpan.FromMinutes(4)).ConfigureAwait(false);

                        if (result.TimedOut || result.Result.Emoji == thumbsDown)
                        {
                            //close channel
                            await newChannel.DeleteAsync();
                        }
                        else
                        {
                            await QueueDialogue(ctx, newChannel, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }
        }

        private async Task QueueDialogue(CommandContext ctx, DiscordChannel newChannel, bool secondTime)
        {
            bool ready = false;
            var interactivity = ctx.Client.GetInteractivity();
            DiscordEmoji yes = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            DiscordEmoji no = DiscordEmoji.FromName(ctx.Client, ":x:");
            // nooks :70xTimmy:
            // Daisy :70zdaisymae:
            DiscordEmoji daisy = DiscordEmoji.FromName(ctx.Client, ":70zdaisymae:");
            DiscordEmoji nooks = DiscordEmoji.FromName(ctx.Client, ":70xTimmy:");
            while (!ready)
            {
                var daisyNooksMessage = await newChannel.SendMessageAsync($"React {daisy} for daisy session or {nooks} for turnip session").ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(daisy).ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(nooks).ConfigureAwait(false);

                var daisyOrNooks = await interactivity.WaitForReactionAsync(react => (react.Emoji == daisy ||
                react.Emoji == nooks) && react.User == ctx.User, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (daisyOrNooks.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }
                isDaisy = daisyOrNooks.Result.Emoji == daisy;

                bool responseCorrect = true;
                await newChannel.SendMessageAsync("Enter your Dodo Code").ConfigureAwait(false);

                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }

                dodoCode = msg.Result.Content;
                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                if (dodoCode.Length != 5)
                {
                    await newChannel.SendMessageAsync("This is not a valid dodo code").ConfigureAwait(false);
                    responseCorrect = false;
                }

                await newChannel.SendMessageAsync("Enter your price").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }

                turnipPrice = msg.Result.Content;

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                if (!int.TryParse(turnipPrice, out int price))
                {
                    await newChannel.SendMessageAsync("This is not a valid price").ConfigureAwait(false);
                    responseCorrect = false;
                }

                await newChannel.SendMessageAsync("Enter session message for your guests").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                else
                {
                    message = string.IsNullOrEmpty(msg.Result.Content) ? "Host has not set a queue message" : msg.Result.Content;
                }

                await newChannel.SendMessageAsync("Enter how many people you want per group (max of 7)").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                maxGroupSize = msg.Result.Content;

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                if (!int.TryParse(maxGroupSize, out int groupSize) || groupSize > 7 || groupSize < 1)
                {
                    await newChannel.SendMessageAsync("This is not a valid group size").ConfigureAwait(false);
                    responseCorrect = false;
                }

                await newChannel.SendMessageAsync("Please send a photo of your price").ConfigureAwait(false);

                var attachmentMsg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member && x.Attachments.Count > 0, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (attachmentMsg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    if (secondTime)
                    {
                        await newChannel.DeleteAsync();
                    }
                    break;
                }

                attachment = attachmentMsg.Result.Attachments[0].Url;

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Turnip Session",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = attachment
                    }
                };

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Dodo Code: {dodoCode}");
                if (isDaisy)
                {
                    sb.AppendLine($"Daisy Price: {turnipPrice}");
                }
                else
                {
                    sb.AppendLine($"Turnip Price: {turnipPrice}");
                }
                sb.AppendLine($"Session Message: {message}");
                sb.AppendLine($"Group Size: {maxGroupSize}");
                sb.AppendLine($"If this information is correct press the :white_check_mark: otherwise press :x: to start again.");


                embed.Description = sb.ToString();
                if (responseCorrect)
                {
                    var sentMessage = await newChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);

                    await sentMessage.CreateReactionAsync(yes).ConfigureAwait(false);
                    await sentMessage.CreateReactionAsync(no).ConfigureAwait(false);

                    var response = await interactivity.WaitForReactionAsync(xe => xe.Emoji == yes || xe.Emoji == no,
                        sentMessage, ctx.User, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                    if (response.Result.Emoji == yes)
                    {
                        ready = true;
                        timedOut = false;
                    }
                }
                else
                {
                    await newChannel.SendMessageAsync("Parts of your response was incorrect please try again").ConfigureAwait(false);
                }
            }
        }

        private async void CreateQueueEmbed(string turnipPrice, CommandContext ctx, DiscordChannel channel, string attachment,
        string maxSize, string dodoCode, string message, bool isDaisy)
        {
            string sessionCode = new AlphaNumericStringGenerator().GetRandomUppercaseAlphaNumericValue(5);
            string embedTitle = isDaisy ? $"Daisy selling turnips for {turnipPrice}" : $"Nooks buying turnips for {turnipPrice}";
            var queueEmbed = new DiscordEmbedBuilder
            {
                Title = embedTitle,
                ImageUrl = attachment,
                Description = $"To join type ```?join {sessionCode}```",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Hosted by {ctx.Member.DisplayName}"
                }
            };
            ulong postChannelId = isDaisy ? daisyMaeChannel : turnipPostChannel;
            var postChannel = ctx.Guild.GetChannel(postChannelId);
            var queueMsg = await postChannel.SendMessageAsync(embed: queueEmbed).ConfigureAwait(false);

            int.TryParse(turnipPrice, out int tprice);
            if (ctx.Guild.Id == 744699540212416592 && tprice >= 500)
            {
                DiscordRole turnipRole; //753724832423870577
                turnipRole = ctx.Guild.GetRole(753724832423870577);
                var turnipChannel = ctx.Guild.GetChannel(turnipPostChannel);
                var turnipRoleEmbed = new DiscordEmbedBuilder
                {
                    Title = $"High turnip price at nooks",
                    Description = $"{turnipRole.Mention} {ctx.User.Mention} is hosting a high price of" +
                    $" {turnipPrice}. See {Formatter.Mention(turnipChannel)} for information on how to join.",
                    Color = DiscordColor.Aquamarine
                };
                var loungeChannel = ctx.Guild.GetChannel(744731248416784545);
                await loungeChannel.SendMessageAsync($"{turnipRole.Mention}", embed: turnipRoleEmbed).ConfigureAwait(false);
            }

            InsertQueueIntoDBAsync(ctx.User.Id, queueMsg.Id, channel.Id, int.Parse(maxSize), dodoCode, sessionCode, message, isDaisy);
        }

        [Command("queuehelp")]
        [Aliases("qh")]
        [Description("Shows common queue commands")]
        public async Task QueueHelp(CommandContext ctx)
        {
            if (ctx.Channel.Parent.Id == privateChannelGroup)
            {
                await ctx.Channel.SendMessageAsync(embed: sessionEmbed).ConfigureAwait(false);
            }
        }

        [Command("sendmessage")]
        [Aliases("sendmsg", "sm", "smessage", "smsg")]
        [Description("Sends a message to everyone in you queue")]
        public async Task SendMessageToQueuers(CommandContext ctx, [Description("Message you want to send"), RemainingText] string message)
        {
            try
            {
                if (ctx.Channel.Parent.Id != privateChannelGroup)
                {
                    await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                    return;
                }

                if (message.Length < 1 || string.IsNullOrEmpty(message))
                {
                    await ctx.Channel.SendMessageAsync("You have not entered a message").ConfigureAwait(false);
                    return;
                }

                List<ulong> discordIds = new List<ulong>();
                ulong queueOwnerId = 0;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?pChannelId and visited = 0";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?pChannelId", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            discordIds.Add(reader.GetUInt64("DiscordID"));
                        }
                    }
                    reader.Close();
                }
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "SELECT queueOwner from pathQueues WHERE privateChannelID = ?pChannelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?pChannelId", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            queueOwnerId = reader.GetUInt64("queueOwner");
                        }
                    }
                }

                if (queueOwnerId == 0)
                {
                    await ctx.Channel.SendMessageAsync("Could not find an owner for this queue channel").ConfigureAwait(false);
                    return;
                }
                DiscordMember queueOwner = await ctx.Guild.GetMemberAsync(queueOwnerId).ConfigureAwait(false);
                if (discordIds.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("Currently no one waiting in your queue or on island").ConfigureAwait(false);
                }
                else
                {
                    foreach (ulong id in discordIds)
                    {
                        var user = await ctx.Guild.GetMemberAsync(id).ConfigureAwait(false);
                        var userDmChannel = await user.CreateDmChannelAsync().ConfigureAwait(false);
                        await userDmChannel.SendMessageAsync($"Message from {queueOwner.DisplayName}: {message}").ConfigureAwait(false);
                    }
                    await ctx.Channel.SendMessageAsync($"Sent your message to the {discordIds.Count} members in your queue").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("showqueue")]
        [Hidden]
        public async Task ShowQueueCommand(CommandContext ctx)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                return;
            }
            try
            {
                List<QueueMember> queueMembers = new List<QueueMember>();
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "SELECT DiscordID, onisland, GroupNumber, PlaceInGroup from pathQueuers WHERE " +
                        "queueChannelID = ?channelID AND visited = 0 ORDER BY TimeJoined ASC, GroupNumber ASC, PlaceInGroup ASC";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            QueueMember member = new QueueMember(reader.GetUInt64("DiscordID"), reader.GetBoolean("onIsland"),
                                reader.GetInt32("GroupNumber"), reader.GetInt32("PlaceInGroup"));
                            queueMembers.Add(member);
                        }
                    }
                    else
                    {
                        //no one in queue
                        await ctx.Channel.SendMessageAsync("Currently there is no one in your queue").ConfigureAwait(false);
                        return;
                    }
                }
                queueMembers.OrderBy(qm => qm.GroupNumber).ThenBy(qm => qm.PlaceInGroup).ToList();
                List<QueueMember> onIslandMembers = queueMembers.FindAll(qm => qm.OnIsland == true).ToList();
                queueMembers.RemoveAll(qm => onIslandMembers.Contains(qm));
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "Users in your queue",
                    Color = DiscordColor.Blurple
                };

                List<Page> pagesEmbed = new List<Page>();
                if (onIslandMembers.Count > 0)
                {
                    Page onIslandPage = new Page();
                    DiscordEmbedBuilder oisEmbed = new DiscordEmbedBuilder
                    {
                        Title = "On Island"
                    };
                    onIslandMembers.OrderBy(oim => oim.PlaceInGroup);
                    foreach (QueueMember queueMember in onIslandMembers)
                    {
                        DiscordMember discordMember = await ctx.Guild.GetMemberAsync(queueMember.DiscordID);
                        oisEmbed.AddField($"Position {queueMember.PlaceInGroup}", $"{discordMember.DisplayName}");
                    }
                    onIslandPage.Embed = oisEmbed;
                    pagesEmbed.Add(onIslandPage);
                }
                if (queueMembers.Count > 0)
                {
                    Page page = new Page();
                    DiscordEmbedBuilder pageEmbed = new DiscordEmbedBuilder();
                    int currentGroup = 0;
                    for (int i = 0; i < queueMembers.Count; i++)
                    {
                        DiscordMember queuerMember = await ctx.Guild.GetMemberAsync(queueMembers.ElementAt(i).DiscordID);
                        if (queueMembers.ElementAt(i).GroupNumber != currentGroup)
                        {
                            if (currentGroup != 0)
                            {
                                page.Embed = pageEmbed;
                                pagesEmbed.Add(page);
                            }
                            currentGroup = queueMembers.ElementAt(i).GroupNumber;
                            page = new Page();
                            pageEmbed = new DiscordEmbedBuilder
                            {
                                Title = $"Group {currentGroup}"
                            };
                        }
                        pageEmbed.AddField($"Position {queueMembers.ElementAt(i).PlaceInGroup}", queuerMember.DisplayName);
                        if ((i + 1) >= queueMembers.Count)
                        {
                            page.Embed = pageEmbed;
                            pagesEmbed.Add(page);
                        }
                    }
                }

                if (pagesEmbed.Count <= 0)
                {
                    await ctx.Channel.SendMessageAsync("Could not find anyone in your queue").ConfigureAwait(false);
                    return;
                }

                InteractivityExtension interactivty = ctx.Client.GetInteractivity();

                await interactivty.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pagesEmbed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("There has been an error while running this command. " +
                    "If this persists please contact a mod/admin for help.").ConfigureAwait(false);
            }
            finally
            {
                await ctx.Message.DeleteAsync();
            }

        }

        [Command("join")]
        [Hidden]
        public async Task JoinQueue(CommandContext ctx, string code)
        {
            await ctx.Message.DeleteAsync();
            if (code.Length != 5)
            {
                var joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} This is not a valid code").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, joinMessage.Id);
                return;
            }
            bool banned = IsUserBannedFromQueue(code, ctx.Member.Id);

            if (banned)
            {
                var joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You have been banned from " +
                    $"this queue and therefore cannot join it.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, joinMessage.Id);
                return;
            }

            try
            {
                bool validCode = true;
                ulong queueChannelId = 0;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "SELECT privateChannelID from pathQueues WHERE sessionCode = ?sessionCode and active = 1";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            queueChannelId = reader.GetUInt64("privateChannelID");
                        }
                    }
                    else
                    {
                        // invalid code
                        var msg = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} " +
                            $"This is not a valid code or the queue is no longer active").ConfigureAwait(false);
                        StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                        validCode = false;
                    }
                }
                if (!validCode)
                {
                    return;
                }

                if (CheckIfUserInQueue(ctx.Member.Id, queueChannelId))
                {
                    var msg = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You are already in this queue.").ConfigureAwait(false);
                    StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                    return;
                }
                AddMemberToQueue(ctx.Member.Id, queueChannelId, code);

                await ctx.Member.SendMessageAsync("You have successfully joined the queue and will be DM'd when it's your turn. " +
                    $"To Check your place in the queue you can run {Formatter.InlineCode("?place " + code)} in #path-bot-commands");

                var pChannel = ctx.Guild.GetChannel(queueChannelId);
                await pChannel.SendMessageAsync($"{ctx.Member.DisplayName} has joined your queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                var joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} " +
                    $"There has been an error while running this command. " +
                    "If this persists please contact a mod/admin for help.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, joinMessage.Id);
            }
        }

        [Command("leave")]
        [Hidden]
        public async Task LeaveQueue(CommandContext ctx, string code)
        {
            if (code.Length != 5)
            {
                await ctx.Channel.SendMessageAsync("This is not a valid code").ConfigureAwait(false);
                return;
            }

            try
            {
                ulong privateChannel = GetPrivateChannelFromCode(code);
                if (privateChannel == 0)
                {
                    await ctx.Channel.SendMessageAsync("No active queues match this code").ConfigureAwait(false);
                    return;
                }
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemoveFromQueue", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.Add("sesCode", MySqlDbType.VarChar, 5).Value = code;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                var msg = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You are no longer in queue {code}.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                await ctx.Message.DeleteAsync();
                var queueChannel = ctx.Guild.GetChannel(privateChannel);
                await queueChannel.SendMessageAsync($"{ctx.Member.DisplayName} has left your queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("sendcode")]
        [Hidden]
        public async Task SendDodoCode(CommandContext ctx)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            try
            {
                string query;
                MySqlCommand command;
                bool returnEarly = false;
                int maxPeople = 0;
                string message = "";
                string dodo = "";
                List<ulong> discordIds = new List<ulong>();
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    query = "SELECT dodoCode, message, maxVisitorsAtOnce from pathQueues WHERE privateChannelID = ?channelid AND active = 1";
                    command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelid", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    //command.Parameters.Add("?owner", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        var msg = await ctx.Channel.SendMessageAsync("There is no active queue associated with this channel.").ConfigureAwait(false);
                        reader.Close();
                        StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                        returnEarly = true;
                    }
                    while (reader.Read())
                    {
                        maxPeople = reader.GetInt32("maxVisitorsAtOnce");
                        message = reader.GetString("message");
                        dodo = reader.GetString("dodoCode");
                    }
                    reader.Close();
                }
                if (returnEarly)
                {
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    // update current people on island
                    query = "UPDATE pathQueuers SET visited = 1 WHERE onisland = 1 AND queueChannelID = ?channelID";
                    command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    // get new people
                    query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                        "AND visited = 0 AND onisland = 0 ORDER BY TimeJoined ASC LIMIT ?limit";
                    command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    command.Parameters.Add("?limit", MySqlDbType.Int32).Value = maxPeople;
                    await connection.OpenAsync();
                    var reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        var msg = await ctx.Channel.SendMessageAsync("There is no one waiting in your queue currently.").ConfigureAwait(false);
                        reader.Close();
                        connection.Close();
                        StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                        return;
                    }
                    while (reader.Read())
                    {
                        discordIds.Add(reader.GetUInt64("DiscordID"));
                    }
                }
                MoveUsersToOnIsland(discordIds, ctx.Channel.Id);
                foreach(var user in discordIds)
                {
                    var dmUser = await ctx.Guild.GetMemberAsync(user);
                    var dmChannel = await dmUser.CreateDmChannelAsync().ConfigureAwait(false);
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{ctx.Member.DisplayName}'s queue",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Message sent at {ctx.Message.Timestamp}"
                        }
                    };
                    embed.AddField("Dodo Code", dodo);
                    embed.AddField("Message From host", message);
                    embed.AddField("Tip", $"Don't forget to tip the host by using the" +
                        $" {Formatter.InlineCode("?tip")} command in {Formatter.Bold("path-bot-commands")} channel.");

                    await dmChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                }
                await ctx.Channel.SendMessageAsync("Code sent to next people in the queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.Write(ex.StackTrace);
            }
            finally
            {
                await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            }
        }

        [Command("endqueue")]
        [Hidden]
        public async Task EndQueue(CommandContext ctx)
        {
            try
            {
                ulong messageId = 0;
                bool isDaisy = false;
                bool endEarly = false;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET active = 0 WHERE privateChannelID = ?channelID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                    query = "Select queueMessageID, daisy from pathQueues WHERE privateChannelID = ?channelID";
                    command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        var msg = await ctx.Channel.SendMessageAsync("Could not find a queue associated with this channel. " +
                            "Please contact a mod or admin to get the channel removed").ConfigureAwait(false);
                        StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                        endEarly = true;
                    }
                    while (reader.Read())
                    {
                        messageId = reader.GetUInt64("queueMessageID");
                        isDaisy = reader.GetBoolean("daisy");
                    }
                }
                if (endEarly)
                {
                    return;
                }
                ulong postChannel = isDaisy ? daisyMaeChannel : turnipPostChannel;
                var message = await ctx.Guild.GetChannel(postChannel).GetMessageAsync(messageId);

                if (message == null) return;

                var embedContent = message.Embeds[0];
                var newEmbed = new DiscordEmbedBuilder
                {
                    Title = embedContent.Title,
                    Description = "Closed",
                    ImageUrl = embedContent.Image.Url.ToString()
                };
                DiscordEmbed discordEmbed = newEmbed;
                await message.ModifyAsync(embed: discordEmbed).ConfigureAwait(false);
                await ctx.Channel.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("updatemessage")]
        [Aliases("um")]
        [Description("update your queues message")]
        public async Task UpdateQueueMessage(CommandContext ctx, [Description("new queue message"), RemainingText] string message)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET message = ?message WHERE privateChannelID = ?channelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?message", MySqlDbType.VarChar).Value = message;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Message now updated to: {message}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync("Could not update message at this point in time").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("updatedodo")]
        [Aliases("updatecode", "uc")]
        [Hidden]
        public async Task UpdateDodoCode(CommandContext ctx, string newcode)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            if (newcode.Length != 5)
            {
                var msg = await ctx.Channel.SendMessageAsync("Dodo codes must be 5 characters long.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET dodoCode = ?newcode WHERE privateChannelID = ?currentChannel";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?newcode", MySqlDbType.VarChar, 5).Value = newcode.Trim();
                    command.Parameters.Add("?currentChannel", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                var msg = await ctx.Channel.SendMessageAsync($"Dodo code is now updated to {newcode} and the new code has been sent to everyone on island")
                    .ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);

                ulong msgId = GetQueueMessageIdFromPrivateChannelId(ctx.Channel.Id);
                string turnipPrice = "", message = "", maxGroupSize = "";
                bool isDaisy = false;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select embedMessageID, daisy, turnipPrice, message, maxGroupSize from pathQueueMessage" +
                        " where privateChannelID = ?pChannelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?pChannelId", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            msgId = reader.GetUInt64("embedMessageID");
                            turnipPrice = reader.GetString("turnipPrice");
                            message = reader.GetString("message");
                            maxGroupSize = reader.GetString("maxGroupSize");
                        }
                    }
                }

                if (string.IsNullOrEmpty(turnipPrice))
                {
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Dodo Code: {newcode}");
                if (isDaisy)
                {
                    sb.AppendLine($"Daisy Price: {turnipPrice}");
                }
                else
                {
                    sb.AppendLine($"Turnip Price: {turnipPrice}");
                }
                sb.AppendLine($"Session Message: {message}");
                sb.AppendLine($"Group Size: {maxGroupSize}");
                sb.AppendLine($"If this information is correct press the :white_check_mark: otherwise press :x: to start again.");

                var oldEmbed = await ctx.Channel.GetMessageAsync(msgId).ConfigureAwait(false);
                var embed = new DiscordEmbedBuilder
                {
                    Title = oldEmbed.Embeds[0].Title,
                    Url = oldEmbed.Embeds[0].Url.ToString()
                };
                embed.Description = sb.ToString();

                DiscordEmbed newEmbed = embed;
                await oldEmbed.ModifyAsync(embed: newEmbed).ConfigureAwait(false);
                await ResendCodeToOnIslandPeeps(ctx, ctx.Channel.Id, newcode);

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                var msg = await ctx.Channel.SendMessageAsync($"There was an error updating your dodo code, " +
                    $"if this persists please contact an admin or mod for help.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
            }
            finally
            {
                await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            }
        }

        private async Task ResendCodeToOnIslandPeeps(CommandContext ctx, ulong channelId, string dodoCode)
        {
            try
            {
                List<ulong> discordIds = new List<ulong>();
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select DiscordID from pathQueuers WHERE onisland = 1 AND visited = 0 and queueChannelID = ?channelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            discordIds.Add(reader.GetUInt64("DiscordID"));
                        }
                    }
                }
                if (discordIds.Count < 1)
                {
                    return;
                }

                foreach (ulong id in discordIds)
                {
                    var member = await ctx.Guild.GetMemberAsync(id);
                    var dmChannel = await member.CreateDmChannelAsync();
                    await dmChannel.SendMessageAsync($"The dodo code for the queue has changed to {dodoCode}. " +
                        $"If you have problems connection please try this new code").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("resume")]
        [Aliases("unpause")]
        [Description("resumes a paused queue")]
        public async Task UnPauseQueue(CommandContext ctx)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            ulong messageId = GetQueueMessageIdFromPrivateChannelId(ctx.Channel.Id);
            if (messageId == 0)
            {
                var msg = await ctx.Channel.SendMessageAsync("Could not find a queue message associated with this channel.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            ulong channelToSearch;
            if (IsDaisyQueue(ctx.Channel.Id))
            {
                channelToSearch = daisyMaeChannel;
            }
            else
            {
                channelToSearch = turnipPostChannel;
            }
            try
            {
                DiscordMessage message = await ctx.Guild.GetChannel(channelToSearch).GetMessageAsync(messageId).ConfigureAwait(false);
                DiscordEmbed oldEmbed = message.Embeds[0];
                DiscordEmbed newEmbed = new DiscordEmbedBuilder
                {
                    Title = oldEmbed.Title,
                    Description = $"To join type ```?join {GetCodeFromChannelId(ctx.Channel.Id)}```",
                    ImageUrl = oldEmbed.Image.Url.ToString(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = oldEmbed.Footer.Text
                    }
                };
                await message.DeleteAsync();
                MakeQueueActive(ctx.Channel.Id);
                var newMsg = await ctx.Guild.GetChannel(channelToSearch).SendMessageAsync(embed: newEmbed).ConfigureAwait(false); // var newMsg = 
                UpdateMessageID(ctx.Channel.Id, newMsg.Id);
                await ctx.Channel.SendMessageAsync("Queue has been resumed").ConfigureAwait(false); 
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("pause")]
        [Aliases("pausequeue")]
        [Description("pauses queue associated with private channel")]
        public async Task PauseQueueCommand(CommandContext ctx)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            ulong messageId = GetQueueMessageIdFromPrivateChannelId(ctx.Channel.Id);
            if (messageId == 0)
            {
                var msg = await ctx.Channel.SendMessageAsync("Could not find a queue message associated with this channel.").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }
            ulong channelToSearch;
            if (IsDaisyQueue(ctx.Channel.Id))
            {
                channelToSearch = daisyMaeChannel;
            }
            else
            {
                channelToSearch = turnipPostChannel;
            }
            try
            {
                DiscordMessage queueMessage = await ctx.Guild.GetChannel(channelToSearch).GetMessageAsync(messageId);

                DiscordEmbed embed = new DiscordEmbedBuilder
                {
                    Title = queueMessage.Embeds[0].Title,
                    Description = "Queue is currently paused",
                    ImageUrl = queueMessage.Embeds[0].Image.Url.ToString(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = queueMessage.Embeds[0].Footer.Text
                    }
                };

                await queueMessage.ModifyAsync(embed: embed).ConfigureAwait(false);
                MakeQueueInactive(messageId);
                await ctx.Channel.SendMessageAsync("Queue has been paused").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private bool IsDaisyQueue(ulong privateChannelId)
        {
            bool isDaisy = false;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT daisy FROM pathQueues WHERE privateChannelID = ?channelID";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = privateChannelId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        isDaisy = reader.GetBoolean("daisy");
                    }
                }
            }
            return isDaisy;
        }

        private void MoveUsersToOnIsland(List<ulong> discordIDs, ulong channelId)
        {
            foreach (var user in discordIDs)
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueuers SET onisland = 1 WHERE DiscordID = ?discordid AND queueChannelID = ?channelId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = user;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        [Command("kick")]
        [Hidden]
        public async Task KickUser(CommandContext ctx, string grouposition, string userPosition)
        {
            if (ctx.Channel.ParentId != privateChannelGroup)
            {
                await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                return;
            }
            if (!int.TryParse(userPosition, out int result))
            {
                await ctx.Channel.SendMessageAsync("User position is not a number").ConfigureAwait(false);
                return;
            }
            if (!int.TryParse(grouposition, out int groupPos))
            {
                await ctx.Channel.SendMessageAsync("Group position is not a number").ConfigureAwait(false);
                return;
            }

            try
            {
                string query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                         "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                ulong discordID = 0;
                bool returnEarly = false;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPos;
                    command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = result;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        await ctx.Channel.SendMessageAsync("Could not find any user at this position.").ConfigureAwait(false);
                        reader.Close();
                        await connection.CloseAsync();
                        returnEarly = true;
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            discordID = reader.GetUInt64("DiscordID");
                        }
                        reader.Close();
                    }
                }

                if (returnEarly)
                {
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    query = "DELETE from pathQueuers WHERE queueChannelID = ?channelID " +
                         "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPos;
                    command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = result;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                DiscordMember removedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"Removed {removedUser.DisplayName} from your queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = await ctx.Channel.SendMessageAsync("An error has occured while running this command").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
            }
            finally
            {
                await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            }
        }

        [Command("ban")]
        [Hidden]
        public async Task BanUserFromQueue(CommandContext ctx, int groupPosition, int userPosition)
        {
            if (ctx.Channel.ParentId != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                return;
            }

            try
            {
                string query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                            "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                ulong discordID = 0;
                bool returnEarly = false;

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPosition;
                    command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = userPosition;
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        var msg = await ctx.Channel.SendMessageAsync("Could not find any user at this position.").ConfigureAwait(false);
                        reader.Close();
                        StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                        returnEarly = true;
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            discordID = reader.GetUInt64("DiscordID");
                        }
                        reader.Close();
                    }
                }

                if (returnEarly)
                {
                    return;
                }

                string code = GetCodeFromChannelId(ctx.Channel.Id);

                if (!IsUserBannedFromQueue(code, discordID))
                {
                    using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        query = "Insert Into pathQueueBans (DiscordID, queueChannelID) values (?discordID, ?queueChannelID)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID;
                        command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync())) {
                        query = "DELETE from pathQueuers WHERE queueChannelID = ?channelID " +
                                "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                        command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPosition;
                        command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = userPosition;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    var bannedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                    var msg = await ctx.Channel.SendMessageAsync($"{bannedUser.DisplayName} has been banned from your queue session").ConfigureAwait(false);
                    StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                }
                else
                {
                    var bannedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                    var msg = await ctx.Channel.SendMessageAsync($"{bannedUser.DisplayName} is already banned from your queue").ConfigureAwait(false);
                    StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
                }
            }
            catch (Exception ex)
            {
                var msg = await ctx.Channel.SendMessageAsync("An error has occured while running this command").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                StartTimer(ctx.Client, ctx.Guild.Id, ctx.Channel.Id, msg.Id);
            }
        }

        private string GetCodeFromChannelId(ulong channelId)
        {
            string code = "";
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select sessionCode from pathQueues where privateChannelID = ?channelID";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = channelId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        code = reader.GetString("sessionCode");
                    }
                }
            }
            return code;
        }

        private bool IsUserBannedFromQueue(string code, ulong discordId)
        {
            bool isBanned = false;
            ulong channelId = GetPrivateChannelFromCode(code);
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select DiscordID from pathQueueBans Where DiscordID = ?discordID AND queueChannelID = ?channelId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordId;
                command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                connection.Open();
                var reader = command.ExecuteReader();
                isBanned = reader.HasRows;
            }

            return isBanned;
        }

        private int GetMaxVisitorsAtOnceFromCode(string code)
        {
            int maxVisitors = 0;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT maxVisitorsAtOnce from pathQueues WHERE sessionCode = ?sessionCode";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        maxVisitors = reader.GetInt32("maxVisitorsAtOnce");
                    }
                }
            }
            return maxVisitors;
        }

        private bool CheckIfUserInQueue(ulong userid, ulong channelid)
        {
            bool inQueue = false;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT * from pathQueuers WHERE DiscordID = ?userid AND queueChannelID = ?channelid " +
                    "AND visited = 0 AND onisland = 0";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userid", MySqlDbType.VarChar, 40).Value = userid;
                command.Parameters.Add("?channelid", MySqlDbType.VarChar, 40).Value = channelid;
                connection.Open();
                var reader = command.ExecuteReader();
                inQueue = reader.HasRows;
                reader.Close();
            }

            return inQueue;
        }

        private ulong GetPrivateChannelFromCode(string code)
        {
            return GetPrivateChannelFromCode(code, true);
        }

        private ulong GetPrivateChannelFromCode(string code, bool active)
        {
            ulong queueChannelId = 0;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT privateChannelID from pathQueues WHERE sessionCode = ?sessionCode AND active = ?active";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();
                command.Parameters.Add("?active", MySqlDbType.VarChar).Value = active == true ? 1 : 0;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        queueChannelId = reader.GetUInt64("privateChannelID");
                    }
                }
            }
            return queueChannelId;
        }

        private async void CreateQueueEmbed(string turnipPrice, CommandContext ctx, DiscordChannel channel, string attachment,
            string maxSize, string dodoCode, string message, bool isDaisy, DiscordMessage embedMessage)
        {
            try
            {
                string sessionCode = new AlphaNumericStringGenerator().GetRandomUppercaseAlphaNumericValue(5);
                string embedTitle = isDaisy ? $"Daisy selling turnips for {turnipPrice}" : $"Nooks buying turnips for {turnipPrice}";
                var queueEmbed = new DiscordEmbedBuilder
                {
                    Title = embedTitle,
                    ImageUrl = attachment,
                    Description = $"To join type ```?join {sessionCode}```",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Hosted by {ctx.Member.DisplayName}"
                    }
                };
                ulong postChannelId = isDaisy ? daisyMaeChannel : turnipPostChannel;
                var postChannel = ctx.Guild.GetChannel(postChannelId);
                var queueMsg = await postChannel.SendMessageAsync(embed: queueEmbed).ConfigureAwait(false);

                InsertQueueEmbedIntoDB(channel.Id, embedMessage.Id, isDaisy, turnipPrice, message, maxSize);
                InsertQueueIntoDBAsync(ctx.User.Id, queueMsg.Id, channel.Id, int.Parse(maxSize), dodoCode, sessionCode, message, isDaisy);

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private void InsertQueueEmbedIntoDB(ulong privateChannelId, ulong embedMessageId, bool isDaisy, string turnipPrice,
            string message, string maxGroupSize)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "INSERT INTO pathQueueMessage (privateChannelID, embedMessageID, daisy, turnipPrice, message, maxGroupSize)" +
                        " VALUES (?channelId, ?embedId, ?daisy, ?turnipPrice, ?message, ?maxGroupSize)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = privateChannelId;
                    command.Parameters.Add("?embedId", MySqlDbType.VarChar, 40).Value = embedMessageId;
                    command.Parameters.Add("?daisy", MySqlDbType.Int16).Value = isDaisy;
                    command.Parameters.Add("?turnipPrice", MySqlDbType.VarChar, 4).Value = turnipPrice;
                    command.Parameters.Add("?message", MySqlDbType.VarChar).Value = message;
                    command.Parameters.Add("?maxGroupSize", MySqlDbType.VarChar, 2).Value = maxGroupSize;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private void InsertQueueIntoDBAsync(
            ulong queueOwner, ulong queueMessageID, ulong privateChannel, int maxVisitors, string dodoCode, string sessionCode, string message, bool isDaisy)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "INSERT INTO pathQueues (queueOwner, queueMessageID, privateChannelID, maxVisitorsAtOnce, dodoCode, sessionCode, message, daisy) " +
                        "VALUES (?queueOwner, ?queueMessage, ?privateChannel, ?maxVisitors, ?dodoCode, ?sessionCode, ?message, ?daisy)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?queueOwner", MySqlDbType.VarChar, 40).Value = queueOwner.ToString();
                    command.Parameters.Add("?queueMessage", MySqlDbType.VarChar, 40).Value = queueMessageID.ToString();
                    command.Parameters.Add("?privateChannel", MySqlDbType.VarChar, 40).Value = privateChannel.ToString();
                    command.Parameters.Add("?maxVisitors", MySqlDbType.Int32).Value = maxVisitors;
                    command.Parameters.Add("?dodoCode", MySqlDbType.VarChar, 5).Value = dodoCode;
                    command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = sessionCode;
                    command.Parameters.Add("?message", MySqlDbType.VarChar, 2550).Value = message;
                    command.Parameters.Add("?daisy", MySqlDbType.Int16).Value = isDaisy;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private void MakeQueueInactive(ulong queueMessageID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET active = 0 WHERE queueMessageID = ?queueMessage";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?queueMessage", MySqlDbType.VarChar, 40).Value = queueMessageID.ToString();
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private void UpdateMessageID(ulong privateChannelID, ulong MessageID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET queueMessageID = ?msgId WHERE privateChannelID = ?channelId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?msgId", MySqlDbType.VarChar, 40).Value = MessageID;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = privateChannelID;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void MakeQueueActive(ulong privateChannelID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "UPDATE pathQueues SET active = 1 WHERE privateChannelID = ?channelId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = privateChannelID;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AddMemberToQueue(ulong discordID, ulong queueChannelID, string sessionCode)
        {
            int maxVisitorsInGroup = GetMaxVisitorsAtOnceFromCode(sessionCode);
            (int, int) groupInfoNumebrs = GetGroupNumber(queueChannelID, maxVisitorsInGroup);

            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "INSERT INTO pathQueuers (DiscordID, queueChannelID, GroupNumber, PlaceInGroup) " +
                    "VALUES (?discordID, ?queueChannelID, ?groupNumber, ?placeInGroup)";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID.ToString();
                command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = queueChannelID.ToString();
                command.Parameters.Add("?groupNumber", MySqlDbType.Int32).Value = groupInfoNumebrs.Item1;
                command.Parameters.Add("?placeInGroup", MySqlDbType.Int32).Value = groupInfoNumebrs.Item2;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private ulong GetQueueMessageIdFromPrivateChannelId(ulong privateChannelId)
        {
            ulong messageId = 0;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT queueMessageID FROM pathQueues WHERE privateChannelID = ?channelId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = privateChannelId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        messageId = reader.GetUInt64("queueMessageID");
                    }
                }
            }
            return messageId;
        }

        private (int, int) GetGroupNumber(ulong queueChannelID, int maxInGroup)
        {
            int groupNumber = 1;
            int positionInGroup = 1;
            bool firstPerson = false;
            bool isOnisland = false;
            using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT * FROM pathQueuers WHERE queueChannelID = ?queueChannelID AND visited = 0 " +
                    "ORDER BY GroupNumber DESC, PlaceInGroup DESC, TimeJoined DESC LIMIT 1";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = queueChannelID;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        groupNumber = reader.GetInt32("GroupNumber");
                        positionInGroup = reader.GetInt32("PlaceInGroup");
                        isOnisland = reader.GetBoolean("onisland");
                    }
                }
                reader.Close();
            }

            if (firstPerson)
            {
                return (groupNumber, positionInGroup);
            }

            if (positionInGroup >= maxInGroup)
            {
                groupNumber++;
                positionInGroup = 1;
            }
            else if (isOnisland)
            {
                groupNumber++;
                positionInGroup = 1;
            }
            else
            {
                positionInGroup++;
            }
            return (groupNumber, positionInGroup);
        }
    }
}