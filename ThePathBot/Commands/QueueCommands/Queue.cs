using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.QueueCommands
{
    public class Queue : BaseCommandModule
    {
        private readonly ulong privateChannelGroup = 745024494464270448;
        private readonly ulong turnipPostChannel = 744733259748999270;
        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private readonly DiscordEmbedBuilder sessionEmbed = new DiscordEmbedBuilder
        {
            Title = "Your Queue has been created",
            Description = "To see whos in the queue run ```?showqueue ``` To send the next group of people run ```?sendcode```" +
                "To show who is currently on island run ```?onisland``` To end your session run ```?endqueue```",
            Color = DiscordColor.Blurple
        };

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
            string turnipPrice = "0";
            string attachment = "";
            string maxGroupSize = "0";
            string dodoCode = "";
            string message = "Welcome";
            while (!ready)
            {
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

                await newChannel.SendMessageAsync("Enter your turnip price").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                turnipPrice = msg.Result.Content;

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                if (!int.TryParse(turnipPrice, out int price))
                {
                    await newChannel.SendMessageAsync("This is not a valid turnip price").ConfigureAwait(false);
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
                    ThumbnailUrl = attachment
                };

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Dodo Code: {dodoCode}");
                sb.AppendLine($"Turnip Price: {turnipPrice}");
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

            CreateQueueEmbed(turnipPrice, ctx, newChannel, attachment, maxGroupSize, dodoCode, message);
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
                string query = "SELECT DiscordID, onisland from pathQueuers WHERE queueChannelID = ?channelID AND visited = 0 ORDER BY TimeJoined ASC";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;

                List<ulong> discordIDs = new List<ulong>();
                Dictionary<ulong, bool> queuers = new Dictionary<ulong, bool>();
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        discordIDs.Add(reader.GetUInt64("DiscordID"));
                        if (!queuers.ContainsKey(reader.GetUInt64("DiscordID")))
                        {
                            queuers.Add(reader.GetUInt64("DiscordID"), reader.GetBoolean("onisland"));
                        }
                    }
                }
                else
                {
                    //no one in queue
                    await ctx.Channel.SendMessageAsync("Currently there is no one in your queue").ConfigureAwait(false);
                    return;
                }
                reader.Close();
                connection.Close();
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Users in your queue",
                    Color = DiscordColor.Blurple
                };

                List<Page> pagesEmbed = new List<Page>();
                var onIslandPage = new Page();
                var oisEmbed = new DiscordEmbedBuilder
                {
                    Title = "On Island"
                };
                //foreach(var item in queuers.Keys)
                //{
                //    if (queuers[item])
                //    {
                //        oisEmbed.AddField("Member", ctx.Guild.GetMemberAsync(item).Result.DisplayName);
                //        queuers.Remove(item);
                //    }
                //}
                //onIslandPage.Embed = oisEmbed;
                //pagesEmbed.Add(onIslandPage);

                StringBuilder sb = new StringBuilder();
                foreach(var item in queuers.Keys)
                {
                    var user = await ctx.Guild.GetMemberAsync(item);
                    sb.AppendLine(user.DisplayName);
                }
                var interactivty = ctx.Client.GetInteractivity();

                var pages = interactivty.GeneratePagesInEmbed(sb.ToString(), SplitType.Line);
                //pagesEmbed.AddRange(pages);

                await interactivty.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("There has been an error while running this command. " +
                    "If this persists please contact a mod/admin for help.").ConfigureAwait(false);
            }

        }

        [Command("join")]
        [Hidden]
        public async Task JoinQueue(CommandContext ctx, string code)
        {
            if (code.Length != 5)
            {
                await ctx.Channel.SendMessageAsync("This is not a valid code").ConfigureAwait(false);
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
                    await ctx.Channel.SendMessageAsync("This is not a valid code or the queue is no longer active").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
                    return;
                }
                reader.Close();
                connection.Close();
                if (await CheckIfUserInQueue(ctx.Member.Id, queueChannelId))
                {
                    await ctx.Channel.SendMessageAsync("You are already in this queue.").ConfigureAwait(false);
                    return;
                }
                await AddMemberToQueue(ctx.Member.Id, queueChannelId);
                await ctx.Channel.SendMessageAsync("You have successfully joined the queue and will be DM'd when " +
                    "it's your turn.").ConfigureAwait(false);
                var pChannel = ctx.Guild.GetChannel(queueChannelId);
                await pChannel.SendMessageAsync($"{ctx.Member.DisplayName} has joined your queue").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("There has been an error while running this command. " +
                    "If this persists please contact a mod/admin for help.").ConfigureAwait(false);
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
                await ctx.Channel.SendMessageAsync($"You are no longer in queue {code}.").ConfigureAwait(false);
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
                await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
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
                    await ctx.Channel.SendMessageAsync("There is no active queue associated with this channel.").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
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
                    await ctx.Channel.SendMessageAsync("There is no one waiting in your queue currently.").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
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
                    var dmChannel = await ctx.Guild.GetMemberAsync(user).Result.CreateDmChannelAsync().ConfigureAwait(false);
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
                query = "Select queueMessageID from pathQueues WHERE privateChannelID = ?channelID";
                command = new MySqlCommand(query, connection);
                command.Parameters.Add("?channelID", MySqlDbType.VarChar, 40).Value = ctx.Channel.Id;
                connection.Open();
                var reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    await ctx.Channel.SendMessageAsync("Could not find a queue associated with this channel. " +
                        "Please contact a mod or admin to get the channel removed").ConfigureAwait(false);
                    reader.Close();
                    connection.Close();
                    return;
                }
                while (reader.Read())
                {
                    messageId = reader.GetUInt64("queueMessageID");
                }

                var message = await ctx.Guild.GetChannel(744644693479915591).GetMessageAsync(messageId);

                if (message == null) return;

                await ctx.Guild.GetChannel(744644693479915591).DeleteMessageAsync(message);
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
                await ctx.Channel.SendMessageAsync("You can only run this command from a private queue channel").ConfigureAwait(false);
                return;
            }
            if (newcode.Length != 5)
            {
                await ctx.Channel.SendMessageAsync("Dodo codes must be 5 characters long.").ConfigureAwait(false);
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

                await ctx.Channel.SendMessageAsync($"Dodo code is now updated to {newcode}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync($"There was an error updating your dodo code, " +
                    $"if this persists please contact an admin or mod for help.").ConfigureAwait(false);
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
            string query = "SELECT * from pathQueuers WHERE DiscordID = ?userid AND queueChannelID = ?channelid AND visited = 0";
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
            string maxSize, string dodoCode, string message)
        {
            string sessionCode = new AlphaNumericStringGenerator().GetRandomUppercaseAlphaNumericValue(5);
            var queueEmbed = new DiscordEmbedBuilder
            {
                Title = $"Nooks buying turnips for {turnipPrice}",
                ImageUrl = attachment,
                Description = $"To join type ```?join {sessionCode}```"
            };
            var postChannel = ctx.Guild.GetChannel(turnipPostChannel);
            var queueMsg = await postChannel.SendMessageAsync(embed: queueEmbed).ConfigureAwait(false);

            await InsertQueueIntoDBAsync(ctx.User.Id, queueMsg.Id, channel.Id, int.Parse(maxSize), dodoCode, sessionCode, message);
        }

        private async Task InsertQueueIntoDBAsync(
            ulong queueOwner, ulong queueMessageID, ulong privateChannel, int maxVisitors, string dodoCode, string sessionCode, string message)
        {

            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "INSERT INTO pathQueues (queueOwner, queueMessageID, privateChannelID, maxVisitorsAtOnce, dodoCode, sessionCode, message) " +
                "VALUES (?queueOwner, ?queueMessage, ?privateChannel, ?maxVisitors, ?dodoCode, ?sessionCode, ?message)";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?queueOwner", MySqlDbType.VarChar, 40).Value = queueOwner.ToString();
            command.Parameters.Add("?queueMessage", MySqlDbType.VarChar, 40).Value = queueMessageID.ToString();
            command.Parameters.Add("?privateChannel", MySqlDbType.VarChar, 40).Value = privateChannel.ToString();
            command.Parameters.Add("?maxVisitors", MySqlDbType.Int32).Value = maxVisitors;
            command.Parameters.Add("?dodoCode", MySqlDbType.VarChar, 5).Value = dodoCode;
            command.Parameters.Add("?sessionCode", MySqlDbType.VarChar, 5).Value = sessionCode;
            command.Parameters.Add("?message", MySqlDbType.VarChar, 255).Value = message;
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
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

        private async Task AddMemberToQueue(ulong discordID, ulong queueChannelID)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "INSERT INTO pathQueuers (DiscordID, queueChannelID) VALUES (?discordID, ?queueChannelID)";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID.ToString();
            command.Parameters.Add("?queueChannelID", MySqlDbType.VarChar, 40).Value = queueChannelID.ToString();
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
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