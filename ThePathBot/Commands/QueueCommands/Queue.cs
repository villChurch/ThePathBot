using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Models;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.QueueCommands
{
    public class Queue : BaseCommandModule
    {
        private readonly ulong privateChannelGroup = 745024494464270448; //test server 744273831602028645;
        private readonly ulong turnipPostChannel = 744733259748999270; //test server 744644693479915591;
        private readonly ulong daisyMaeChannel = 744733207148232845;
        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private Timer msgDestructTimer;
        private readonly DiscordEmbedBuilder sessionEmbed = new DiscordEmbedBuilder
        {
            Title = "Your Queue has been created",
            Description = "To see whos in the queue run ```?showqueue ``` To send the next group of people run ```?sendcode```" +
                "To change your dodocode run ```?updatedodo``` To kick someone from your queue run " +
            "```?kick groupNumber positionIngroup``` To end your session run ```?endqueue```",
            Color = DiscordColor.Blurple
        };

        private void StartTimer(DiscordMessage msg)
        {
            msgDestructTimer = new Timer
            {
                Interval = 5000 // 5 seconds
            };
            msgDestructTimer.Elapsed += (sender, e) => DestructMessage(sender, e, msg);
            msgDestructTimer.AutoReset = true;
            msgDestructTimer.Enabled = true;
        }

        private async void DestructMessage(object source, ElapsedEventArgs e, DiscordMessage msg)
        {
            try
            {
                await msg.DeleteAsync();
                msgDestructTimer.Stop();
                msgDestructTimer.Dispose();
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
            //first we must create a private channel
            Guid guid = Guid.NewGuid();
            var newChannel = await ctx.Guild.CreateChannelAsync(guid.ToString(), DSharpPlus.ChannelType.Text, ctx.Guild.GetChannel(privateChannelGroup));
            //Then give the user access to the channel
            await newChannel.AddOverwriteAsync(ctx.Member, DSharpPlus.Permissions.AccessChannels);

            // Now we can get some information
            bool ready = false;
            var interactivity = ctx.Client.GetInteractivity();
            DiscordEmoji yes = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            DiscordEmoji no = DiscordEmoji.FromName(ctx.Client, ":x:");
            // nooks :70xTimmy:
            // Daisy :70zdaisymae:
            DiscordEmoji daisy = DiscordEmoji.FromName(ctx.Client, ":70zdaisymae:");
            DiscordEmoji nooks = DiscordEmoji.FromName(ctx.Client, ":70xTimmy:");
            string turnipPrice = "0";
            string attachment = "";
            string maxGroupSize = "0";
            string dodoCode = "";
            string message = "Welcome";
            bool isDaisy = false;
            while (!ready)
            {
                var daisyNooksMessage = await newChannel.SendMessageAsync($"React {daisy} for daisy session or {nooks} for turnip session").ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(daisy).ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(nooks).ConfigureAwait(false);

                var daisyOrNooks = await interactivity.WaitForReactionAsync(react => (react.Emoji == daisy ||
                react.Emoji == nooks) && react.User == ctx.User, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                isDaisy = daisyOrNooks.Result.Emoji == daisy;

                bool responseCorrect = true;
                await newChannel.SendMessageAsync("Enter your Dodo Code").ConfigureAwait(false);

                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

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

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

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

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                message = msg.Result.Content;

                await newChannel.SendMessageAsync("Enter how many people you want per group (max of 7)").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                maxGroupSize = msg.Result.Content;

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

                var attachmentMsg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member && x.Attachments.Count > 0).ConfigureAwait(false);

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
                        sentMessage, ctx.User, TimeSpan.FromMinutes(1)).ConfigureAwait(false);

                    if (response.Result.Emoji == yes)
                    {
                        ready = true;
                    }
                }
                else
                {
                    await newChannel.SendMessageAsync("Parts of your response was incorrect please try again").ConfigureAwait(false);
                }
            }
            var dodoMsg = await newChannel.SendMessageAsync(embed: sessionEmbed).ConfigureAwait(false);

            CreateQueueEmbed(turnipPrice, ctx, newChannel, attachment, maxGroupSize, dodoCode, message, isDaisy);
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
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "SELECT DiscordID, onisland, GroupNumber, PlaceInGroup from pathQueuers WHERE " +
                    "queueChannelID = ?channelID AND visited = 0 ORDER BY TimeJoined ASC, GroupNumber ASC, PlaceInGroup ASC";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                List<QueueMember> queueMembers = new List<QueueMember>();
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
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
                    reader.Close();
                    connection.Close();
                    return;
                }
                reader.Close();
                connection.Close();

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
            DiscordMessage joinMessage;
            if (code.Length != 5)
            {
                joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} This is not a valid code").ConfigureAwait(false);
                StartTimer(joinMessage);
                return;
            }
            bool banned = await IsUserBannedFromQueue(code, ctx.Member.Id);

            if (banned)
            {
                joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You have been banned from " +
                    $"this queue and therefore cannot join it.").ConfigureAwait(false);
                StartTimer(joinMessage);
                return;
            }

            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "SELECT privateChannelID from pathQueues WHERE sessionCode = ?sessionCode and active = 1";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();

                ulong queueChannelId = 0;

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
                    joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} " +
                        $"This is not a valid code or the queue is no longer active").ConfigureAwait(false);
                    StartTimer(joinMessage);
                    reader.Close();
                    connection.Close();
                    return;
                }
                reader.Close();
                connection.Close();
                if (await CheckIfUserInQueue(ctx.Member.Id, queueChannelId))
                {
                    joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You are already in this queue.").ConfigureAwait(false);
                    StartTimer(joinMessage);
                    return;
                }
                await AddMemberToQueue(ctx.Member.Id, queueChannelId, code);
                joinMessage = await ctx.Channel.SendMessageAsync(
                    $"{ctx.Member.Mention} You have successfully joined the queue and will be DM'd when " +
                    "it's your turn.").ConfigureAwait(false);
                StartTimer(joinMessage);
                var pChannel = ctx.Guild.GetChannel(queueChannelId);
                await pChannel.SendMessageAsync($"{ctx.Member.DisplayName} has joined your queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                joinMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} " +
                    $"There has been an error while running this command. " +
                    "If this persists please contact a mod/admin for help.").ConfigureAwait(false);
                StartTimer(joinMessage);
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
                ulong privateChannel = await GetPrivateChannelFromCode(code);
                if (privateChannel == 0)
                {
                    await ctx.Channel.SendMessageAsync("No active queues match this code").ConfigureAwait(false);
                    return;
                }
                MySqlConnection connection = await GetDBConnectionAsync();
                MySqlCommand command = new MySqlCommand("RemoveFromQueue", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                command.Parameters.Add("sesCode", MySqlDbType.VarChar, 5).Value = code;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                var msg = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} You are no longer in queue {code}.").ConfigureAwait(false);
                StartTimer(msg);
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
                StartTimer(msg);
                return;
            }
            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "SELECT dodoCode, message, maxVisitorsAtOnce from pathQueues WHERE privateChannelID = ?channelid AND queueOwner = ?owner AND active = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelid", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                command.Parameters.Add("?owner", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                await connection.OpenAsync();
                var reader = command.ExecuteReader();
                int maxPeople = 0;
                string message = "";
                string dodo = "";
                if (!reader.HasRows)
                {
                    var msg = await ctx.Channel.SendMessageAsync("There is no active queue associated with this channel.").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
                    StartTimer(msg);
                    return;
                }
                while (reader.Read())
                {
                    maxPeople = reader.GetInt32("maxVisitorsAtOnce");
                    message = reader.GetString("message");
                    dodo = reader.GetString("dodoCode");
                }
                reader.Close();
                await connection.CloseAsync();
                // update current people on island
                query = "UPDATE pathQueuers SET visited = 1 WHERE onisland = 1 AND queueChannelID = ?channelID";
                command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                // get new people
                query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                    "AND visited = 0 AND onisland = 0 ORDER BY TimeJoined ASC LIMIT ?limit";
                command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                command.Parameters.Add("?limit", MySqlDbType.Int32).Value = maxPeople;
                await connection.OpenAsync();
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    var msg = await ctx.Channel.SendMessageAsync("There is no one waiting in your queue currently.").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
                    StartTimer(msg);
                    return;
                }
                List<ulong> discordIds = new List<ulong>();
                while (reader.Read())
                {
                    discordIds.Add(reader.GetUInt64("DiscordID"));
                }

                await MoveUsersToOnIsland(discordIds, ctx.Channel.Id);
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

                    await dmChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    //List<ulong> users = new List<ulong>();
                    //users.Add(user);
                    //await MoveUsersToOnIsland(users);
                }
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
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "UPDATE pathQueues SET active = 0 WHERE privateChannelID = ?channelID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                ulong messageId = 0;
                bool isDaisy = false;
                query = "Select queueMessageID, daisy from pathQueues WHERE privateChannelID = ?channelID";
                command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                connection.Open();
                var reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    var msg = await ctx.Channel.SendMessageAsync("Could not find a queue associated with this channel. " +
                        "Please contact a mod or admin to get the channel removed").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
                    StartTimer(msg);
                    return;
                }
                while (reader.Read())
                {
                    messageId = reader.GetUInt64("queueMessageID");
                    isDaisy = reader.GetBoolean("daisy");
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
                //await ctx.Guild.GetChannel(turnipPostChannel).DeleteMessageAsync(message);
                await ctx.Channel.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("updatedodo")]
        [Hidden]
        public async Task UpdateDodoCode(CommandContext ctx, string newcode)
        {
            if (ctx.Channel.Parent.Id != privateChannelGroup)
            {
                var msg = await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                StartTimer(msg);
                return;
            }
            if (newcode.Length != 5)
            {
                var msg = await ctx.Channel.SendMessageAsync("Dodo codes must be 5 characters long.").ConfigureAwait(false);
                StartTimer(msg);
                return;
            }
            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "UPDATE pathQueues SET dodoCode = ?newcode WHERE privateChannelID = ?currentChannel";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?newcode", MySqlDbType.VarChar, 5).Value = newcode;
                command.Parameters.Add("?currentChannel", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();

                var msg = await ctx.Channel.SendMessageAsync($"Dodo code is now updated to {newcode}").ConfigureAwait(false);
                StartTimer(msg);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                var msg = await ctx.Channel.SendMessageAsync($"There was an error updating your dodo code, " +
                    $"if this persists please contact an admin or mod for help.").ConfigureAwait(false);
                StartTimer(msg);
            }
            finally
            {
                await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            }
        }

        private async Task MoveUsersToOnIsland(List<ulong> discordIDs, ulong channelId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "UPDATE pathQueuers SET onisland = 1 WHERE DiscordID = ?discordid AND queueChannelID = ?channelId";
            foreach (var user in discordIDs)
            {
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = user;
                command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
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
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                    "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                ulong discordID = 0;
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPos;
                command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = result;
                await connection.OpenAsync();
                MySqlDataReader reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    await ctx.Channel.SendMessageAsync("Could not find any user at this position.").ConfigureAwait(false);
                    reader.Close();
                    await connection.CloseAsync();
                    return;
                }
                else
                {
                    while (reader.Read())
                    {
                        discordID = reader.GetUInt64("DiscordID");
                    }
                    reader.Close();
                    await connection.CloseAsync();

                    query = "DELETE from pathQueuers WHERE queueChannelID = ?channelID " +
                         "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                    command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                    command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPos;
                    command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = result;
                    await connection.OpenAsync();
                    command.ExecuteNonQuery();
                    await connection.CloseAsync();
                    DiscordMember removedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                    await ctx.Channel.SendMessageAsync($"Removed {removedUser.DisplayName} from your queue").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var msg = await ctx.Channel.SendMessageAsync("An error has occured while running this command").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                StartTimer(msg);
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
                StartTimer(msg);
                return;
            }

            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "SELECT DiscordID from pathQueuers WHERE queueChannelID = ?channelID " +
                    "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                ulong discordID = 0;
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPosition;
                command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = userPosition;
                await connection.OpenAsync();
                MySqlDataReader reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    var msg = await ctx.Channel.SendMessageAsync("Could not find any user at this position.").ConfigureAwait(false);
                    reader.Close();
                    await connection.CloseAsync();
                    StartTimer(msg);
                    return;
                }
                else
                {
                    while (reader.Read())
                    {
                        discordID = reader.GetUInt64("DiscordID");
                    }
                    reader.Close();
                    await connection.CloseAsync();

                    string code = await GetCodeFromChannelId(ctx.Channel.Id);

                    if (!await IsUserBannedFromQueue(code, discordID))
                    {
                        query = "Insert Into pathQueueBans (DiscordID, queueChannelID) values (?discordID, ?queueChannelID)";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID;
                        command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                        await connection.CloseAsync();

                        query = "DELETE from pathQueuers WHERE queueChannelID = ?channelID " +
                                "AND GroupNumber = ?groupPosition AND PlaceInGroup = ?userPosition";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                        command.Parameters.Add("?groupPosition", MySqlDbType.Int32).Value = groupPosition;
                        command.Parameters.Add("?userPosition", MySqlDbType.Int32).Value = userPosition;
                        await connection.OpenAsync();
                        command.ExecuteNonQuery();
                        await connection.CloseAsync();

                        var bannedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                        var msg = await ctx.Channel.SendMessageAsync($"{bannedUser.DisplayName} has been banned from your queue session").ConfigureAwait(false);
                        StartTimer(msg);
                    }
                    else
                    {
                        var bannedUser = await ctx.Guild.GetMemberAsync(discordID).ConfigureAwait(false);
                        var msg = await ctx.Channel.SendMessageAsync($"{bannedUser.DisplayName} is already banned from your queue").ConfigureAwait(false);
                        StartTimer(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = await ctx.Channel.SendMessageAsync("An error has occured while running this command").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                StartTimer(msg);
            }
        }

        private async Task<string> GetCodeFromChannelId(ulong channelId)
        {
            string code = "";
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "Select sessionCode from pathQueues where privateChannelID = ?channelID";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = channelId;
            await connection.OpenAsync();
            var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    code = reader.GetString("sessionCode");
                }
            }

            return code;
        }

        private async Task<bool> IsUserBannedFromQueue(string code, ulong discordId)
        {
            ulong channelId = await GetPrivateChannelFromCode(code);
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "Select DiscordID from pathQueueBans Where DiscordID = ?discordID AND queueChannelID = ?channelId";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordId;
            command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
            await connection.OpenAsync();
            var reader = command.ExecuteReader();
            bool isBanned = reader.HasRows;
            reader.Close();
            await connection.CloseAsync();

            return isBanned;
        }

        private async Task<int> GetMaxVisitorsAtOnceFromCode(string code)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "SELECT maxVisitorsAtOnce from pathQueues WHERE sessionCode = ?sessionCode";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();

            int queueChannelId = 0;

            connection.Open();
            var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    queueChannelId = reader.GetInt32("maxVisitorsAtOnce");
                }
                return queueChannelId;
            }
            else
            {
                return 0;
            }
        }

        private async Task<bool> CheckIfUserInQueue(ulong userid, ulong channelid)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "SELECT * from pathQueuers WHERE DiscordID = ?userid AND queueChannelID = ?channelid " +
                "AND visited = 0 AND onisland = 0";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?userid", MySqlDbType.VarChar, 40).Value = userid;
            command.Parameters.Add("?channelid", MySqlDbType.VarChar, 40).Value = channelid;
            await connection.OpenAsync();
            var reader = await command.ExecuteReaderAsync();
            bool inQueue = reader.HasRows;
            reader.Close();
            await connection.CloseAsync();

            return inQueue;
        }

        private async Task<ulong> GetPrivateChannelFromCode(string code)
        {
            return await GetPrivateChannelFromCode(code, true);
        }

        private async Task<ulong> GetPrivateChannelFromCode(string code, bool active)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "SELECT privateChannelID from pathQueues WHERE sessionCode = ?sessionCode AND active = ?active";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = code.ToUpper();
            command.Parameters.Add("?active", MySqlDbType.VarChar).Value = active == true ? 1 : 0;
            ulong queueChannelId = 0;

            connection.Open();
            var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    queueChannelId = reader.GetUInt64("privateChannelID");
                }
                return queueChannelId;
            }
            else
            {
                return 0;
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
                Description = $"To join type ```?join {sessionCode}```"
            };
            ulong postChannelId = isDaisy ? daisyMaeChannel : turnipPostChannel;
            var postChannel = ctx.Guild.GetChannel(postChannelId);
            var queueMsg = await postChannel.SendMessageAsync(embed: queueEmbed).ConfigureAwait(false);

            await InsertQueueIntoDBAsync(ctx.User.Id, queueMsg.Id, channel.Id, int.Parse(maxSize), dodoCode, sessionCode, message, isDaisy);
        }

        private async Task InsertQueueIntoDBAsync(
            ulong queueOwner, ulong queueMessageID, ulong privateChannel, int maxVisitors, string dodoCode, string sessionCode, string message, bool isDaisy)
        {
            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
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
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private async Task MakeQueueInactive(ulong queueMessageID)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "UPDATE pathQueues SET active = 0 WHERE queueMessageID = ?queueMessage";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?queueMessage", MySqlDbType.VarChar, 40).Value = queueMessageID.ToString();
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        private async Task AddMemberToQueue(ulong discordID, ulong queueChannelID, string sessionCode)
        {
            int maxVisitorsInGroup = await GetMaxVisitorsAtOnceFromCode(sessionCode);
            (int, int) groupInfoNumebrs = await GetGroupNumber(queueChannelID, maxVisitorsInGroup);
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "INSERT INTO pathQueuers (DiscordID, queueChannelID, GroupNumber, PlaceInGroup) " +
                "VALUES (?discordID, ?queueChannelID, ?groupNumber, ?placeInGroup)";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID.ToString();
            command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = queueChannelID.ToString();
            command.Parameters.Add("?groupNumber", MySqlDbType.Int32).Value = groupInfoNumebrs.Item1;
            command.Parameters.Add("?placeInGroup", MySqlDbType.Int32).Value = groupInfoNumebrs.Item2;
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        private async Task<(int, int)> GetGroupNumber(ulong queueChannelID, int maxInGroup)
        {
            int groupNumber = 1;
            int positionInGroup = 1;
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "SELECT * FROM pathQueuers WHERE queueChannelID = ?queueChannelID AND visited = 0 " +
                "ORDER BY GroupNumber DESC, PlaceInGroup DESC, TimeJoined DESC LIMIT 1";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = queueChannelID;
            await connection.OpenAsync();
            MySqlDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    groupNumber = reader.GetInt32("GroupNumber");
                    positionInGroup = reader.GetInt32("PlaceInGroup");
                }
            }
            else
            {
                return (groupNumber, positionInGroup);
            }
            reader.Close();
            await connection.CloseAsync();

            if (positionInGroup >= maxInGroup)
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

        private async Task<MySqlConnection> GetDBConnectionAsync()
        {
            DBConnection dbCon = DBConnection.Instance();
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            dbCon.DatabaseName = configJson.databaseName;
            dbCon.Password = configJson.databasePassword;
            dbCon.databaseUser = configJson.databaseUser;
            dbCon.databasePort = configJson.databasePort;
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
            return connection;
        }
    }
}