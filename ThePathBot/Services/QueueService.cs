using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Services
{
    public class QueueService
    {
        private readonly DiscordEmbedBuilder sessionEmbed = new DiscordEmbedBuilder
        {
            Title = "Your Queue has been created",
            Description = "To see whos in the queue run ```?showqueue ``` To send the next group of people run ```?sendcode```" +
        "To change your dodocode run ```?updatedodo``` To kick someone from your queue run " +
    "```?kick groupNumber positionIngroup``` To end your session run ```?endqueue```",
            Color = DiscordColor.Blurple
        };
        private readonly ulong privateChannelGroup = 744273831602028645; // honna 745024494464270448; //test server 744273831602028645;
        private readonly ulong turnipPostChannel = 744644693479915591; // honna 744733259748999270; //test server 744644693479915591;
        private readonly ulong daisyMaeChannel = 744733207148232845;
        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string turnipPrice = "0";
        string attachment = "";
        string maxGroupSize = "0";
        string dodoCode = "";
        string message = "Welcome";
        bool isDaisy = false;
        bool timedOut = false;

        public async Task CreateQueue(CommandContext ctx)
        {
            //first we must create a private channel
            Guid guid = Guid.NewGuid();
            var newChannel = await ctx.Guild.CreateChannelAsync(guid.ToString(),
                DSharpPlus.ChannelType.Text, ctx.Guild.GetChannel(privateChannelGroup));
            //Then give the user access to the channel
            await newChannel.AddOverwriteAsync(ctx.Member, DSharpPlus.Permissions.AccessChannels);
            await QueueDialogue(ctx, newChannel);
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

                if (result.TimedOut)
                {
                    //close channel
                }
                else
                {
                    await QueueDialogue(ctx, newChannel);
                }
            }
        }

        private async Task QueueDialogue(CommandContext ctx, DiscordChannel newChannel)
        {
            bool ready = false;
            var interactivity = ctx.Client.GetInteractivity();
            DiscordEmoji yes = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            DiscordEmoji no = DiscordEmoji.FromName(ctx.Client, ":x:");
            // nooks :70xTimmy:
            // Daisy :70zdaisymae:
            DiscordEmoji daisy = DiscordEmoji.FromName(ctx.Client, ":thumbsup:"); //":70zdaisymae:");
            DiscordEmoji nooks = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"); //":70xTimmy:");
            while (!ready)
            {
                var daisyNooksMessage = await newChannel.SendMessageAsync($"React {daisy} for daisy session or {nooks} for turnip session").ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(daisy).ConfigureAwait(false);
                await daisyNooksMessage.CreateReactionAsync(nooks).ConfigureAwait(false);

                var daisyOrNooks = await interactivity.WaitForReactionAsync(react => (react.Emoji == daisy ||
                react.Emoji == nooks) && react.User == ctx.User, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                if (daisyOrNooks.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    break;
                }
                isDaisy = daisyOrNooks.Result.Emoji == daisy;

                bool responseCorrect = true;
                await newChannel.SendMessageAsync("Enter your Dodo Code").ConfigureAwait(false);

                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
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

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
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

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
                    break;
                }

                if (msg.Result.Content.ToLower() == "cancel")
                {
                    await newChannel.DeleteAsync();
                    return;
                }
                message = msg.Result.Content;

                await newChannel.SendMessageAsync("Enter how many people you want per group (max of 7)").ConfigureAwait(false);

                msg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member).ConfigureAwait(false);

                maxGroupSize = msg.Result.Content;

                if (msg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
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

                var attachmentMsg = await interactivity.WaitForMessageAsync(x => x.Channel == newChannel && x.Author == ctx.Member && x.Attachments.Count > 0).ConfigureAwait(false);

                if (attachmentMsg.TimedOut)
                {
                    await newChannel.SendMessageAsync("Queue creation has timed out.").ConfigureAwait(false);
                    timedOut = true;
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

        private async Task InsertQueueIntoDBAsync(ulong queueOwner, ulong queueMessageID,
            ulong privateChannel, int maxVisitors, string dodoCode, string sessionCode, string message, bool isDaisy)
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
