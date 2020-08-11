using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.PathCommands
{
    public class MainPathCommands : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        [Command("addPath")]
        [Description("Adds a path to your list")]
        public async Task AddPath(CommandContext ctx, [Description("link to path followed by path name, eg. link path name")] params String[] args)
        {
            try
            {
                var pathLink = args[0];
                args[0] = "";
                var pathName = String.Join(" ", args);
                DiscordMember pathOwner = ctx.Member;
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

                string query = "INSERT INTO pathLinks (DiscordID, link, pathname) values (?discordid, ?link, ?pathName)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = pathOwner.Id.ToString();
                command.Parameters.Add("?link", MySqlDbType.VarChar, 2500).Value = pathLink;
                command.Parameters.Add("?pathName", MySqlDbType.VarChar, 255).Value = pathName.Trim();
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                await ctx.Channel.SendMessageAsync("Added path <" + pathLink + "> to your list of saved paths")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Color = DiscordColor.Red
                };
                failureEmbed.AddField("Error",
                    "There has been an error during this command, please try again later.");
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
            }
        }

        [Command("myPaths")]
        [Description("DM you a list of your paths")]
        public async Task MyPaths(CommandContext ctx, [Description("optional: name of single path from your list, otherwise returns all paths")] params String[] pathName)
        {
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
                Console.Out.WriteLine(dbCon.connectionString);
                Dictionary<String, String> paths = new Dictionary<string, string>();
                bool specificPath = false;
                string query = "Select link, pathname from pathLinks WHERE DiscordID = ?discordID";
                string pathToSearch = "";
                if (pathName.Length > 0)
                {
                    pathToSearch = String.Join(" ", pathName);
                    query =
                        "Select link, pathname from pathLinks WHERE DiscordID = ?discordID AND pathname = ?pathName";
                    specificPath = true;
                }
                MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
                var command = new MySqlCommand(query, connection);

                if (specificPath)
                {
                    command.Parameters.Add("?pathName", MySqlDbType.VarChar, 255).Value = pathToSearch;
                }
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = ctx.Member.Id.ToString();
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    paths.Add(reader.GetString("pathname"), reader.GetString("link"));
                }

                connection.Close();
                var pathEmbed = new DiscordEmbedBuilder
                {
                    Title = ctx.Member.Nickname + " saved paths",
                    Color = DiscordColor.Blue
                };
                StringBuilder sb = new StringBuilder();
                foreach (var pathsKey in paths.Keys)
                {
                    sb.AppendLine(pathsKey + " - " + paths[pathsKey]);
                    // pathEmbed.AddField(pathsKey, paths[pathsKey], false);
                }

                var interactivity = ctx.Client.GetInteractivity();
                var pathsPages =
                    interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pathsPages)
                    .ConfigureAwait(false);
                //await ctx.Channel.SendMessageAsync(embed: pathEmbed).ConfigureAwait(false);
            }
            catch (ArgumentException argumentException)
            {
                Console.Out.WriteLine(argumentException.Message);
                var noPathsEmbed = new DiscordEmbedBuilder
                {
                    Title = "Wuh Oh!",
                    Color = DiscordColor.Blurple,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "It looks like you have no paths or none match the name entered, please add some paths and try again after."
                    }
                };
                await ctx.Channel.SendMessageAsync(embed: noPathsEmbed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Color = DiscordColor.Red
                };
                failureEmbed.AddField("Error",
                    "There has been an error during this command, please try again later.");
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
            }
        }

        [Command("removePath")]
        [Description("Removes a path from your list")]
        public async Task removePath(CommandContext ctx, [Description("name of path you want to remove")] params String[] args)
        {
            try
            {
                string path = String.Join(" ", args);
                DiscordMember pathOwner = ctx.Member;
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

                string query = "DELETE FROM pathLinks WHERE (DiscordID = ?discordId) AND (pathname = '?path')";
                var command = new MySqlCommand("RemovePath", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("DiscordID", MySqlDbType.VarChar, 40).Value = pathOwner.Id.ToString();
                command.Parameters.Add("pathName", MySqlDbType.VarChar, 255).Value = path.Trim();
                connection.Open();
                var commandtext = command.CommandText;
                foreach (MySqlParameter p in command.Parameters)
                    commandtext = commandtext.Replace(p.ParameterName, p.Value.ToString());
                Console.Out.WriteLine(commandtext);
                command.ExecuteNonQuery();
                connection.Close();
                await ctx.Channel.SendMessageAsync("Deleted " + path + " from your list of saved paths")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Color = DiscordColor.Red
                };
                failureEmbed.AddField("Error",
                    "There has been an error during this command, please try again later.");
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
            }
        }

        private Dictionary<string, string> getEmbedPaths(Dictionary<string, string> paths)
        {
            List<string> keys = new List<string>(paths.Keys);
            Dictionary<string, string> embedPaths = new Dictionary<string, string>();
            int threshold = 5;
            for (int i = 0; i < keys.Count; i++)
            {
                if ((i + 1) == threshold)
                {
                    i = keys.Count;
                }
                embedPaths.Add(keys.ElementAt(i), paths[keys.ElementAt(i)]);
            }
            return embedPaths;
        }

        [Command("sharepath")]
        [Description("Shares path with the mentioned user by dm")]
        public async Task sharePathByDm(CommandContext ctx, [Description("name of path followed by @Users to send it to")] params string[] mentions)
        {
            try
            {
                List<string> userMentionsAndPath = new List<string>(mentions);
                var users = ctx.Message.MentionedUsers;
                if (users.Count < 1)
                {
                    var sendEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Color = DiscordColor.Red,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "You haven't mentioned anyone so I can't share the path"
                        }
                    };
                    await ctx.Channel.SendMessageAsync(embed: sendEmbed).ConfigureAwait(false);
                    return;
                }

                foreach(var mention in userMentionsAndPath)
                {
                    Console.Out.WriteLine(mention);
                }
                foreach (var user in users)
                {
                    Console.Out.WriteLine(user.Mention);
                    userMentionsAndPath.Remove(user.Mention);
                    userMentionsAndPath.Remove(user.Mention.Replace("<@!", "<@"));
                }

                string searchpath = string.Join(" ", userMentionsAndPath);

                string query =
                    "Select link, pathname from pathLinks WHERE DiscordID = ?discordID AND pathname = ?pathName";

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
                Dictionary<string, string> paths = new Dictionary<string, string>();

                MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
                MySqlCommand command = new MySqlCommand(query, connection);

                command.Parameters.Add("?pathName", MySqlDbType.VarChar, 255).Value = searchpath;
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = ctx.Member.Id.ToString();
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    paths.Add(reader.GetString("pathname"), reader.GetString("link"));
                }

                connection.Close();
                if (paths.Count < 1)
                {
                    DiscordEmbedBuilder noPathsEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Color = DiscordColor.Red,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "I could not find any paths called " + searchpath + " in your collection"
                        }
                    };
                    await ctx.Channel.SendMessageAsync(embed: noPathsEmbed).ConfigureAwait(false);
                    return;
                }
                foreach (var user in users)
                {
                    var dmChannelMember = await ctx.Guild.GetMemberAsync(user.Id);
                    var dmChannel = await dmChannelMember.CreateDmChannelAsync().ConfigureAwait(false);
                    await dmChannel.SendMessageAsync(ctx.Message.Author.Username + " has shared path - " + paths[searchpath] +
                                                     " with you");
                }
                var successEmbed = new DiscordEmbedBuilder
                {
                    Title = "Path shared",
                    Color = DiscordColor.Blurple,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Your path has been shared"
                    }
                };
                await ctx.Channel.SendMessageAsync(embed: successEmbed).ConfigureAwait(false);
            }
            catch (Exception ex)
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

        [Command("uploadPath")]
        [Description("adds a path to your list from an attachment")]
        public async Task uploadPath(CommandContext ctx, [Description("name of path")] params string[] args)
        {
            try
            {
                var attachmentList = ctx.Message.Attachments;
                if (attachmentList.Count < 1)
                {
                    var noAttachmentEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Missing attachments",
                        Color = DiscordColor.Red,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "This command requires an attachment, eg a photo"
                        }
                    };

                    await ctx.Channel.SendMessageAsync(embed: noAttachmentEmbed).ConfigureAwait(false);
                    return;
                }

                if (attachmentList.Count != 1)
                {
                    var tooManyAttachmentsEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Too many Attachments",
                        Color = DiscordColor.Red,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "Please only use one attachment with this command"
                        }
                    };
                    await ctx.Channel.SendMessageAsync(embed: tooManyAttachmentsEmbed).ConfigureAwait(false);
                    return;
                }

                var pathLink = attachmentList[0].Url;
                var pathName = String.Join(" ", args);
                DiscordMember pathOwner = ctx.Member;
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
                    "INSERT INTO pathLinks (DiscordID, link, pathname) values (?discordid, ?link, ?pathName)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = pathOwner.Id.ToString();
                command.Parameters.Add("?link", MySqlDbType.VarChar, 2500).Value = pathLink.Trim();
                command.Parameters.Add("?pathName", MySqlDbType.VarChar, 255).Value = pathName.Trim();
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                await ctx.Channel.SendMessageAsync("Added path <" + pathLink + "> to your list of saved paths")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
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

        [Command("renamepath")]
        [Description("renames the chosen path")]
        [Hidden]
        [RequireOwner]
        public async Task renamePath(CommandContext ctx, [Description("current path name")] params string[] args)
        {
            var interactivity = ctx.Client.GetInteractivity();

            var message = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel).ConfigureAwait(false);

            if (message.Result.Author == ctx.Message.Author)
            {
                await ctx.Channel.SendMessageAsync(StringTools.ReverseString(message.Result.Content));
            }
        }
    }
}
