using System;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity;
using DSharpPlus;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ThePathBot.Commands.PathCommands;
using ThePathBot.Commands;
using ThePathBot.Commands.UrbanDictionary;
using ThePathBot.Commands.ACNHCommands;
using ThePathBot.Commands.Admin;
using ThePathBot.Commands.TipSystem;
using ThePathBot.Commands.QueueCommands;
using ThePathBot.Utilities;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace ThePathBot
{
    public class Bot
    {
        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        public InteractivityConfiguration Interactivity { get; private set; }
        private int countNumber = GetCountNumberOnRestart();
        private ulong lastCountId { get; set; }
        private static readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private readonly ulong daisymaeChannelId = 744733207148232845;
        private readonly ulong nookShopChannelId =  744733259748999270; // test channel 746852898465644544;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private int totalMembers = 0;

        public Bot()
        {
        }

        private static int GetCountNumberOnRestart()
        {
            int foundNumber = 0;
            DBConnection dbCon = DBConnection.Instance();
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            dbCon.DatabaseName = configJson.databaseName;
            dbCon.Password = configJson.databasePassword;
            dbCon.databaseUser = configJson.databaseUser;
            dbCon.databasePort = configJson.databasePort;
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);

            string query = "SELECT value FROM `configuration` WHERE item = ?item";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?item", MySqlDbType.VarChar, 255).Value = "countNumber";
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    foundNumber = int.Parse(reader.GetString("value"));
                }
            }
            reader.Close();
            connection.Close();
            return foundNumber;
        }

        private void UpdateCountNumberInDb(int newNumber)
        {
            DBConnection dbCon = DBConnection.Instance();
            string json = string.Empty;

            using (FileStream fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            dbCon.DatabaseName = configJson.databaseName;
            dbCon.Password = configJson.databasePassword;
            dbCon.databaseUser = configJson.databaseUser;
            dbCon.databasePort = configJson.databasePort;
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);

            string query = "UPDATE configuration SET value = ?newNumber where item = ?item";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?item", MySqlDbType.VarChar, 255).Value = "countNumber";
            command.Parameters.Add("?newNumber", MySqlDbType.VarChar, 255).Value = newNumber.ToString();
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        public async Task RunAsync()
        {

            var json = string.Empty;

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using (var fs =
                File.OpenRead(path + "/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
            };

            // var shardClient = new DiscordShardedClient(config);
            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;
            Client.GuildAvailable += this.Client_GuildAvailable;
            Client.ClientErrored += this.Client_ClientError;
            Client.MessageCreated += this.Client_MessageCreated;
            //Client.Logger. += this.DebugLogger_LogMessageReceived;

            // let's enable interactivity, and set default options
#pragma warning disable IDE0058 // Expression value is never used
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // default pagination behaviour to just ignore the reactions
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                // default timeout for other actions to 2 minutes
                Timeout = TimeSpan.FromMinutes(2)
            });
#pragma warning restore IDE0058 // Expression value is never used

            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                DmHelp = false,
                IgnoreExtraArguments = true,
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.CommandExecuted += this.Commands_CommandExecuted;
            Commands.CommandErrored += this.Commands_CommandErrored;

            Commands.RegisterCommands<MainPathCommands>();
            Commands.RegisterCommands<UtilityCommands>();
            Commands.RegisterCommands<PathAdminCommands>();
            Commands.RegisterCommands<PathTagging>();
            Commands.RegisterCommands<Net>();
            Commands.RegisterCommands<PascalWisdom>();
            Commands.RegisterCommands<UrbanDictionarySearch>();
            Commands.RegisterCommands<Axe>();
            Commands.RegisterCommands<Villager>();
            Commands.RegisterCommands<Fish>();
            Commands.RegisterCommands<Emoji>();
            Commands.RegisterCommands<TipUser>();
            Commands.RegisterCommands<Queue>();
            Commands.RegisterCommands<TipHistory>();
            // Commands.RegisterCommands<FunCommands>();

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            //if (e.Channel.IsPrivate && !e.Author.IsBot)
            //{
            //    await e.Channel.SendMessageAsync("Hello").ConfigureAwait(false);
            //}
            if (e.Channel.Id == daisymaeChannelId || e.Channel.Id == nookShopChannelId)
            {
                if (e.Author.IsBot)
                {
                    return;
                }

                if (e.Message.Content.StartsWith("?join"))
                {
                    return;
                }

                if (!(e.Message.Attachments.Count > 0) || e.Message.Embeds.Count > 0)
                {
                    await e.Message.DeleteAsync();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Channel is Image only",
                        Description = $"{e.Message.Author.Mention} " +
                        $"All posts in this channel must have an image attached to them or be an embed",
                        Color = DiscordColor.Red
                    };
                    await e.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                }
            }
            else if (e.Channel.Id == 744753163558584320 && e.Client.CurrentUser.Id != 648636613286690836)
            {
                string message = e.Message.Content;
                bool isANumber = int.TryParse(message, out int number);
                if (isANumber)
                {
                    var emoji = DiscordEmoji.FromName(e.Client, ":thumbsup:");
                    if (number == (countNumber + 1) && lastCountId != e.Message.Author.Id)
                    {
                        await e.Message.CreateReactionAsync(emoji).ConfigureAwait(false);
                        countNumber++;
                        lastCountId = e.Message.Author.Id;
                    }
                    else if(e.Message.Author.Id == lastCountId)
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "Wait your turn!",
                            Color = DiscordColor.Red,
                            Description = $"You can't enter more than one number in a row please wait your turn {e.Guild.GetMemberAsync(e.Message.Author.Id).Result.DisplayName}. Start again from 1"
                        };
                        countNumber = 0;
                        lastCountId = 0;
                        await e.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    }
                    else
                    {
                        DiscordMember member = await e.Guild.GetMemberAsync(e.Message.Author.Id);
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "Wrong number!",
                            Color = DiscordColor.Red,
                            Description = $"{member.DisplayName} has enetered the wrong number! Start again from 1"
                        };
                        countNumber = 0;
                        lastCountId = 0;
                        await e.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    }
                    UpdateCountNumberInDb(countNumber);
                }
            }
        }

        private Task OnClientReady(ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            e.Client.Logger.Log(LogLevel.Information, $"The Path - Client is ready to process events.");
            return Task.CompletedTask;
        }

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.Logger.Log(LogLevel.Information, $"The Path - Guild available: {e.Guild.Name}");
            await UpdatePresenceAsync(e, e.Guild.MemberCount);
            //return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.Logger.Log(LogLevel.Error, $"The Path - Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Information,
                $"The Path - {e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'");

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Error, $"The Path - {e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}");

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            else if (e.Exception is CommandNotFoundException Cnfex)
            {
                if (e.Context.Message.Content.Contains("??") || e.Context.Message.Content.Contains("?!"))
                {
                    return; // for when people do ?????!?....
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Command not found",
                    Description = $"I do not know this command. See ?help for a list of commands I know.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }

        private async Task UpdatePresenceAsync(object _, int amount)
        {
            GuildCreateEventArgs e = _ as GuildCreateEventArgs;

            totalMembers += amount;
            try
            {
                await e.Client.UpdateStatusAsync(new DiscordActivity($"Keeper of Paths for {totalMembers} users"), UserStatus.Online, null);
            }
            catch (Exception ex)
            {
                //client.DebugLogger.LogMessage(LogLevel.Error, LOG_TAG, $"Could not update presence ({ex.GetType()}: {ex.Message})", DateTime.Now);
            }
        }
    }
}
