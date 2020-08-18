using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.TipSystem
{
    public class TipUser : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Command("tip")]
        [Description("tip a user for a positive transaction")]
        public async Task TipAUser(CommandContext ctx,
            [Description("mention users to tip followed by message")] params string[] args)
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

            var users = ctx.Message.MentionedUsers;
            List<string> messages = new List<string>(args);
            List<ulong> discordIds = new List<ulong>();

            foreach (DiscordUser user in users)
            {
                if (user.Id != ctx.Member.Id)
                {
                    discordIds.Add(user.Id);
                }
                messages.Remove(user.Mention);
                messages.Remove(user.Mention.Replace("<@!", "<@"));
            }
            if (discordIds.Count < 1)
            {
                var failureEmbed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-Oh!",
                    Description = "You haven't mentioned anyone. To use this command please do ?tip @user/s message",
                    Color = DiscordColor.Red
                };
                await ctx.Channel.SendMessageAsync(embed: failureEmbed).ConfigureAwait(false);
                return;
            }
            string message = string.Join(" ", messages);

            if (string.Empty == message || message == null)
            {
                message = "Unfortunately they did not leave a message";
            }

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
            try
            {
                foreach (ulong user in discordIds)
                {
                    AddTip(user, ctx.Member.Id, message, connection);
                    //command.Dispose();

                    int newTotal = GetTotal(user, connection);
                    UpdateTotal(user, newTotal, connection);
                    DiscordEmbedBuilder repEmbed = new DiscordEmbedBuilder
                    {
                        Title = $"{ctx.Guild.GetMemberAsync(user).Result.DisplayName} gained one tip from {ctx.Member.DisplayName}",
                        Description = $"{ctx.Member.DisplayName} said: {message}",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"{ctx.Guild.GetMemberAsync(user).Result.DisplayName} now has {newTotal} tips."
                        },
                        Color = DiscordColor.Blurple,
                        ThumbnailUrl = ctx.Guild.GetMemberAsync(user).Result.AvatarUrl
                    };
                    if (ctx.Guild.Channels.ContainsKey(744820641085259856))
                    {
                        await ctx.Guild.GetChannel(744820641085259856).SendMessageAsync(embed: repEmbed).ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync(embed: repEmbed).ConfigureAwait(false);
                    }
                    UpdateRoles(newTotal, user, ctx);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
            finally
            {
                connection.Close();
            }
        }

        [Command("givetips")]
        [Hidden]
        [RequireRoles(RoleCheckMode.Any, "Owners", "Dev", "Tom Nook Incarnate")]
        public async Task GiveTips(CommandContext ctx, params string[] user)
        {
            if (ctx.Guild.Id == 694013861560320081)
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-oh!",
                    Description = "Sorry... This command cannot be run in this server.",
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }
            IReadOnlyList<DiscordUser> mentionedUser = ctx.Message.MentionedUsers;
            if (mentionedUser.Count < 1)
            {
                return;
            }
            List<string> messages = new List<string>(user);

            foreach(DiscordUser mention in mentionedUser)
            {
                messages.Remove(mention.Mention);
                messages.Remove(mention.Mention.Replace("<@!", "<@"));
            }
            string message = string.Join("", messages);

            bool successfullyParsed = int.TryParse(message, out int numberToGive);

            if (!successfullyParsed)
            {
                return;
            }

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

            DiscordUser userToTip = mentionedUser[0];

            int currentTotal = GetTotal(userToTip.Id, connection) - 1;

            UpdateTotal(userToTip.Id, currentTotal + numberToGive, connection);

            await ctx.Channel.SendMessageAsync($"Succesfully given " +
                $"{ctx.Guild.GetMemberAsync(userToTip.Id).Result.DisplayName} {numberToGive} tips.").ConfigureAwait(false);
        }

        [Command("showtips")]
        [Description("show how many tips you have")]
        public async Task ShowTips(CommandContext ctx)
        {
            if (ctx.Guild.Id == 694013861560320081)
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "Wuh-oh!",
                    Description = "Sorry... This command cannot be run in this server.",
                    Color = DiscordColor.Blurple
                };
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return;
            }
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
            try
            {
                int tipTotal = 0;
                string query = "Select total from pathRep where DiscordID = ?discordId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tipTotal = reader.GetInt32("total");
                    }
                }

                reader.Close();

                DiscordEmbedBuilder tipEmbed = new DiscordEmbedBuilder
                {
                    Description = $"{ctx.Member.DisplayName} has {tipTotal} tips.",
                    ThumbnailUrl = ctx.Member.AvatarUrl,
                    Color = DiscordColor.Blurple
                };

                await ctx.Channel.SendMessageAsync(embed: tipEmbed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
            finally
            {
                connection.Close();
            }

        }

        private void AddTip(ulong recipient, ulong sender, string message, MySqlConnection connection)
        {
            try
            {
                string query =
                            "INSERT INTO pathTips (RecipientID, SenderID, Message) values (?recipient, ?sender, ?msg)";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?recipient", MySqlDbType.VarChar, 40).Value = recipient;
                command.Parameters.Add("?sender", MySqlDbType.VarChar, 40).Value = sender;
                command.Parameters.Add("?msg", MySqlDbType.VarChar, 255).Value = message.Trim();
                //command.Parameters.Add("?timestamp", MySqlDbType.Timestamp).Value = ctx.Message.Timestamp.ToUnixTimeMilliseconds();
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private int GetTotal(ulong user, MySqlConnection connection)
        {
            try
            {
                int tipTotal = 0;
                string query = "Select total from pathRep where DiscordID = ?discordId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = user;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tipTotal = reader.GetInt32("total");
                    }
                }

                reader.Close();
                connection.Close();
                return tipTotal + 1;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private void UpdateTotal(ulong user, int total, MySqlConnection connection)
        {
            try
            {
                string query = "Insert into pathRep (DiscordID, total) values (?discordId, ?total) on Duplicate KEY UPDATE total = ?total";
                MySqlCommand command = new MySqlCommand(query, connection);
                connection.Open();
                command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = user;
                command.Parameters.Add("?total", MySqlDbType.Int32).Value = total;
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                connection.Close();
            }
        }

        private async void UpdateRoles(int newTotal, ulong user, CommandContext ctx)
        {
            switch (newTotal)
            {
                case 50:
                    var embed = new DiscordEmbedBuilder
                    {
                        Description = $":tada: {ctx.Guild.GetMemberAsync(user).Result.DisplayName} has reached 50 tips!",
                        Color = DiscordColor.Blurple
                    };
                    if (ctx.Guild.Roles.ContainsKey(744722781106733077))
                    {
                        DiscordRole role = ctx.Guild.GetRole(744722781106733077);
                        await ctx.Guild.GetMemberAsync(user).Result.GrantRoleAsync(role, "Made 50 reviews").ConfigureAwait(false);
                        await ctx.Guild.GetChannel(744731248416784545).SendMessageAsync(embed: embed).ConfigureAwait(false);

                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    }
                    //await ctx.Guild.GetMemberAsync(user).Result.GrantRoleAsync().ConfigureAwait(false); give 50 tip role
                    break;

                case 100:
                    //await ctx.Guild.GetMemberAsync(user).Result.GrantRoleAsync().ConfigureAwait(false); give 100 tip role
                    break;

                default:
                    // do nothing
                    break;
            }

        }
    }
}
