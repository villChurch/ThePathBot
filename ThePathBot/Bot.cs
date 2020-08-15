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

namespace ThePathBot
{
    public class Bot
    {
        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        public InteractivityConfiguration Interactivity { get; private set; }
        private int countNumber = 0;
        private ulong lastCountId { get; set; }

        public Bot()
        {
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
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
            };

            // var shardClient = new DiscordShardedClient(config);
            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;
            Client.GuildAvailable += this.Client_GuildAvailable;
            Client.ClientErrored += this.Client_ClientError;
            Client.MessageCreated += this.Client_MessageCreated;

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
                DmHelp = true
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
            // Commands.RegisterCommands<FunCommands>();

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            //return;
            if (e.Channel.Id == 742996544680099931 && e.Message.Author.Id != 648636613286690836)
            {
                string message = e.Message.Content;
                bool isANumber = int.TryParse(message, out int number);
                number++;
                if (isANumber)
                {
                    await e.Channel.SendMessageAsync(number.ToString()).ConfigureAwait(false);
                }
            }
            /* if (e.Channel.Id == 742996544680099931)
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
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                        {
                            Title = "Wrong number!",
                            Color = DiscordColor.Red,
                            Description = e.Guild.GetMemberAsync(e.Message.Author.Id).Result.DisplayName + " has enetered the wrong number! Start again from 1"
                        };
                        countNumber = 0;
                        lastCountId = 0;
                        await e.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    }
                    Console.Out.WriteLine("Current number is " + countNumber);
                }
            } */
        }

        private Task OnClientReady(ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "The Path", "Client is ready to process events.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "The Path", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "The Path", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "The Path", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "The Path", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

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
    }
}
