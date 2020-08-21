using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands
{
    public class UtilityCommands : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private string botLink =
            "https://discord.com/oauth2/authorize?client_id=741036620912001045&permissions=1544551670&scope=bot";

        [Command("guide")]
        [Description("Returns a brief guide on how to use the bot")]
        public async Task getGuide(CommandContext ctx)
        {
            var guideEmbed = new DiscordEmbedBuilder
            {
                Title = "Guide",
                Color = DiscordColor.Blurple,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Made By - " + ctx.Guild.GetMemberAsync(272151652344266762).Result.Nickname
                }
            };
            guideEmbed.AddField("How do I get all of my stored paths?", "```?mypaths```", false);
            guideEmbed.AddField("How do I get a specific path from my stored paths?", "```?mypaths path name```", false);
            guideEmbed.AddField("How do I add a path to my list using a link?", "```?addpath link path name```", false);
            guideEmbed.AddField("How do I add a path to my list using a photo?",
                "```?uploadpath path name``` Note add this command to the same message as the image, ie. as the comment", false);
            guideEmbed.AddField("How do I delete a path?", "```?removepath path name```", false);
            guideEmbed.AddField("How do I share one of my stored paths with someone?",
                "```?sharepath path name @usertosharewith```", false);
            guideEmbed.AddField("How do I set/update my creator code?", "```?set-cc creator-code```", false);
            guideEmbed.AddField("How do I get my creator code?", "```?get-cc```", false);

            DiscordMember dmMember = await ctx.Guild.GetMemberAsync(ctx.Message.Author.Id).ConfigureAwait(false);
            var dmChannel = await dmMember.CreateDmChannelAsync().ConfigureAwait(false);

            await dmMember.SendMessageAsync(embed: guideEmbed).ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync("A guide has been sent to your DMs").ConfigureAwait(false);
        }

        [Command("invite")]
        [Description("Give an invite link for this bot")]
        [RequireOwner]
        [Hidden]
        public async Task getInviteLink(CommandContext ctx)
        {
            var owner = await ctx.Guild.GetMemberAsync(272151652344266762);
            var inviteEmbed = new DiscordEmbedBuilder
            {
                Title = "Inivte The Path to your server",
                Color = DiscordColor.CornflowerBlue,
                Url = botLink,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Made by " + owner.Nickname
                }
            };
            await ctx.Channel.SendMessageAsync(embed: inviteEmbed).ConfigureAwait(false);
        }

        [Command("set-cc")]
        [Description("Set your creator code")]
        public async Task SetCreatorCode(CommandContext ctx, [Description("your creator code")] string creatorCode)
        {
            string discordId = ctx.Message.Author.Id.ToString();
            try
            {
                var dbCon = DBConnection.Instance();
                var json = string.Empty;

                using (var fs =
                    File.OpenRead(configFilePath + "/config.json")
                )
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    json = await sr.ReadToEndAsync().ConfigureAwait(false);

                var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
                dbCon.DatabaseName = configJson.databaseName;
                dbCon.Password = configJson.databasePassword;
                dbCon.databaseUser = configJson.databaseUser;
                dbCon.databasePort = configJson.databasePort;
                MySqlConnection connection = new MySqlConnection(dbCon.connectionString);

                string query =
                    "INSERT INTO creatorCodes (DiscordID, CreatorCode) values (?discordid, ?creatorCode)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = discordId;
                command.Parameters.Add("?creatorCode", MySqlDbType.VarChar, 40).Value = creatorCode.Trim();
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Creator Code Set",
                    Description = "Your creator code has been set",
                    Color = DiscordColor.PhthaloBlue
                };

                await ctx.Channel.SendMessageAsync(embed: successEmbed).ConfigureAwait(false);
            }
            catch (MySqlException mse)
            {
                if (mse.Message.Contains("Duplicate entry"))
                {
                    string query = "UPDATE creatorCodes SET CreatorCode = ?creatorCode WHERE DiscordID = ?discordid";
                    var dbCon = DBConnection.Instance();
                    var json = string.Empty;

                    using (var fs =
                        File.OpenRead(configFilePath + "/config.json")
                    )
                    using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                        json = await sr.ReadToEndAsync().ConfigureAwait(false);

                    var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
                    dbCon.DatabaseName = configJson.databaseName;
                    dbCon.Password = configJson.databasePassword;
                    dbCon.databaseUser = configJson.databaseUser;
                    dbCon.databasePort = configJson.databasePort;
                    MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = discordId;
                    command.Parameters.Add("?creatorCode", MySqlDbType.VarChar, 40).Value = creatorCode.Trim();
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                    var updateEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Creator Code Updated",
                        Description = "Your creator code has been updated",
                        Color = DiscordColor.PhthaloBlue
                    };

                    await ctx.Channel.SendMessageAsync(embed: updateEmbed).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                Console.Out.WriteLine(ex.GetType());
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Color = DiscordColor.Red
                };
                failureEmbed.AddField("Wuh-Oh!",
                    "There has been an error during this command, please try again later.");
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
            }
        }

        [Command("get-cc")]
        [Description("returns your creator code if set")]
        public async Task GetCreatorCode(CommandContext ctx)
        {
            try
            {
                string discordId = ctx.Message.Author.Id.ToString();

                string query =
                       "Select CreatorCode from creatorCodes WHERE DiscordID = ?discordID";

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
                Console.Out.WriteLine(dbCon.connectionString);
                string creatorCode = "";

                MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
                MySqlCommand command = new MySqlCommand(query, connection);

                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = discordId;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    creatorCode = reader.GetString("CreatorCode");
                }

                connection.Close();

                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = ctx.Guild.GetMemberAsync(ctx.Message.Author.Id).Result.Nickname + "'s Creator Code",
                    Description = creatorCode,
                    Color = DiscordColor.Blurple
                };

                await ctx.Channel.SendMessageAsync(embed: successEmbed).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Color = DiscordColor.Red
                };
                failureEmbed.AddField("Wuh-Oh!",
                    "There has been an error during this command, please try again later.");
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
            }
        }

        [Command("addemoji")]
        [Hidden]
        [RequireOwner]
        public async Task addEmojiToServer(CommandContext ctx, params string[] emoji)
        {
            string emojiLink = emoji[0];

            emoji[0] = "";

            string emojiName = String.Join("", emoji);

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://cdn.discordapp.com/");
            var response = await client.GetAsync(emojiLink.Split(new string[] { "https://cdn.discordapp.com/" }, StringSplitOptions.None)[1]);

            byte[] res = await response.Content.ReadAsByteArrayAsync();
            Stream emojiImage = new MemoryStream(res);
            await ctx.Guild.CreateEmojiAsync(emojiName, emojiImage).ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync("Added emoji called :" + emojiName + ":").ConfigureAwait(false);
        }

        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner]
        public async Task SudoAsync(CommandContext ctx, [Description("Member to execute the command as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            Command cmd = ctx.CommandsNext.FindCommand(command, out var args);
            if (cmd == null)
            {
                throw new CommandNotFoundException(command);
            }

            CommandContext fctx = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, command, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx);
        }
    }

}