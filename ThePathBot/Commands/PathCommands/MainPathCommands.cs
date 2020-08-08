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
        [Command("sendpaginated"), Description("Sends a paginated message.")]
        public async Task SendPaginated(CommandContext ctx)
        {
            try
            {
                // first retrieve the interactivity module from the client
                var interactivity = ctx.Client.GetInteractivity();
                // generate pages.
                var lipsum =
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris vitae velit eget nunc iaculis laoreet vitae eu risus. Nullam sit amet cursus purus. Duis enim elit, malesuada consequat aliquam sit amet, interdum vel orci. Donec vehicula ut lacus consequat cursus. Aliquam pellentesque eleifend lectus vitae sollicitudin. Vestibulum sit amet risus rhoncus, hendrerit felis eget, tincidunt odio. Nulla sed urna ante. Mauris consectetur accumsan purus, ac dignissim ligula condimentum eu. Phasellus ullamcorper, arcu sed scelerisque tristique, ante elit tincidunt sapien, eu laoreet ipsum mauris eu justo. Curabitur mattis cursus urna, eu ornare lacus pulvinar in. Vivamus cursus gravida nunc. Sed dolor nisi, congue non hendrerit at, rutrum sed mi. Duis est metus, consectetur sed libero quis, dignissim gravida lacus. Mauris suscipit diam dolor, semper placerat justo sodales vel. Curabitur sed fringilla odio.\n\nMorbi pretium placerat nulla sit amet condimentum. Duis placerat, felis ornare vehicula auctor, augue odio consectetur eros, sit amet tristique dolor risus nec leo. Aenean vulputate ipsum sagittis augue malesuada, id viverra odio gravida. Curabitur aliquet elementum feugiat. Phasellus eu faucibus nibh, eget finibus nibh. Proin ac fermentum enim, non consequat orci. Nam quis elit vulputate, mollis eros ut, maximus lacus. Vivamus et lobortis odio. Suspendisse potenti. Fusce nec magna in eros tempor tincidunt non vel mi. Pellentesque auctor eros tellus, vel ultrices mi ultricies eu. Nam pharetra sed tortor id elementum. Donec sit amet mi eleifend, iaculis purus sit amet, interdum turpis.\n\nAliquam at consectetur lectus. Ut et ultrices augue. Etiam feugiat, tortor nec dictum pharetra, nulla mauris convallis magna, quis auctor libero ipsum vitae mi. Mauris posuere feugiat feugiat. Phasellus molestie purus sit amet ipsum sodales, eget pretium lorem pharetra. Quisque in porttitor quam, nec hendrerit ligula. Fusce tempus, diam ut malesuada semper, leo tortor vulputate erat, non porttitor nisi elit eget turpis. Nam vitae arcu felis. Aliquam molestie neque orci, vel consectetur velit mattis vel. Fusce eget tempus leo. Morbi sit amet bibendum mauris. Aliquam erat volutpat. Phasellus nunc lectus, vulputate vitae turpis vel, tristique vulputate nulla. Aenean sit amet augue at mauris laoreet convallis. Nam quis finibus dui, at lobortis lectus.\n\nSuspendisse potenti. Pellentesque massa enim, dapibus at tortor eu, posuere ultricies augue. Nunc condimentum enim id ex sagittis, ut dignissim neque tempor. Nulla cursus interdum turpis. Aenean auctor tempor justo, sed rhoncus lorem sollicitudin quis. Fusce non quam a ante suscipit laoreet eget at ligula. Aenean condimentum consectetur nunc, sit amet facilisis eros lacinia sit amet. Integer quis urna finibus, tristique justo ut, pretium lectus. Proin consectetur enim sed risus rutrum, eu vehicula augue pretium. Vivamus ultricies justo enim, id imperdiet lectus molestie at. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.\n\nNullam tincidunt dictum nibh, dignissim laoreet libero eleifend ut. Vestibulum eget maximus nulla. Suspendisse a auctor elit, ac facilisis tellus. Sed iaculis turpis ac purus tempor, ut pretium ante ultrices. Aenean commodo tempus vestibulum. Morbi vulputate pharetra molestie. Ut rhoncus quam felis, id mollis quam dapibus id. Curabitur faucibus id justo in ornare. Praesent facilisis dolor lorem, non vulputate velit finibus ut. Praesent vestibulum nunc ac nibh iaculis porttitor.\n\nFusce mattis leo sed ligula laoreet accumsan. Pellentesque tortor magna, ornare vitae tellus eget, mollis placerat est. Suspendisse potenti. Ut sit amet lacus sed nibh pulvinar mattis in bibendum dui. Mauris vitae turpis tempor, malesuada velit in, sodales lacus. Sed vehicula eros in magna condimentum vestibulum. Aenean semper finibus lectus, vel hendrerit lorem euismod a. Sed tempor ante quis magna sollicitudin, eu bibendum risus congue. Donec lectus sem, accumsan ut mollis et, accumsan sed lacus. Nam non dui non tellus pretium mattis. Mauris ultrices et felis ut imperdiet. Nam erat risus, consequat eu eros ac, convallis viverra sapien. Etiam maximus nunc et felis ultrices aliquam.\n\nUt tincidunt at magna at interdum. Sed fringilla in sem non lobortis. In dictum magna justo, nec lacinia eros porta at. Maecenas laoreet mattis vulputate. Sed efficitur tempor euismod. Integer volutpat a odio eu sagittis. Aliquam congue tristique nisi, quis aliquet nunc tristique vitae. Vivamus ac iaculis nunc, et faucibus diam. Donec vitae auctor ipsum, quis posuere est. Proin finibus, dolor ac euismod consequat, urna sem ultrices lectus, in iaculis sem nulla et odio. Integer et vulputate metus. Phasellus finibus et lorem eget lacinia. Maecenas velit est, luctus quis fermentum nec, fringilla eu lorem.\n\nPellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Mauris faucibus neque eu consectetur egestas. Mauris aliquet nibh pellentesque mollis facilisis. Duis egestas lectus sed justo sagittis ultrices. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Curabitur hendrerit quis arcu id dictum. Praesent in massa eget lectus pulvinar consectetur. Aliquam eget ipsum et velit congue porta vitae ut eros. Quisque convallis lacus et venenatis sagittis. Phasellus sit amet eros ac nibh facilisis laoreet vel eget nisi. In ante libero, volutpat in risus vel, tristique blandit leo. Morbi posuere bibendum libero, non efficitur mi sagittis vel. Cras viverra pulvinar pellentesque. Mauris auctor et lacus ut pellentesque. Nunc pretium luctus nisi eu convallis.\n\nSed nec ultricies arcu. Aliquam eu tincidunt diam, nec luctus ligula. Ut laoreet dignissim est, eu fermentum massa fermentum eget. Nullam non viverra justo, sed congue felis. Phasellus id convallis mauris. Aliquam elementum euismod ex, vitae dignissim nunc consectetur vitae. Donec ut odio quis ex placerat elementum sit amet eget lectus. Suspendisse potenti. Nam non massa id mi suscipit euismod. Nullam varius tincidunt diam congue congue. Proin pharetra vestibulum eros, vel imperdiet sem rutrum at. Cras eget gravida ligula, quis facilisis ex.\n\nEtiam consectetur elit mauris, euismod porta urna auctor a. Nulla facilisi. Praesent massa ipsum, iaculis non odio at, varius lobortis nisi. Aliquam viverra erat a dapibus porta. Pellentesque imperdiet maximus mattis. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Donec luctus elit sit amet feugiat convallis. Phasellus varius, sem ut volutpat vestibulum, magna arcu porttitor libero, in dapibus metus dolor nec dolor. Fusce at eleifend magna. Mauris cursus pellentesque sagittis. Nullam nec laoreet ante, in sodales arcu.";
                var lipsum_pages =
                    interactivity.GeneratePagesInEmbed(lipsum, SplitType.Character, new DiscordEmbedBuilder());

                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, lipsum_pages)
                    .ConfigureAwait(false);
                // send the paginator
                // await interactivity.SendPaginatedMessage(ctx.Channel, ctx.User, lipsum_pages, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
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
