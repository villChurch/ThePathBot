using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Listeners
{
    public class ReactionService
    {
        private static readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public ReactionService()
        {
        }

        public static async Task CheckReactionAddedIsFridgeReaction(MessageReactionAddEventArgs e)
        {
            var fridgeEmoji = DiscordEmoji.FromName(e.Client, ":trophy:");
            bool guildSetup = await IsFridgeBoardSetup(e.Guild.Id);
            if (!guildSetup || e.Emoji != fridgeEmoji)
            {
                return;
            }
            var messageId = e.Message.Id;

            Tuple<ulong, int> guildFridgeInfo = await GetGuildFridgeInfo(e.Guild.Id);
            if (guildFridgeInfo.Item1 == 0 && guildFridgeInfo.Item2 == 0)
            {
                await e.Channel.SendMessageAsync("Failed to fetch guild configuration").ConfigureAwait(false);
                return;
            }

            var originalMessageChannel = await e.Client.GetChannelAsync(e.Channel.Id);
            var originalMessage = await originalMessageChannel.GetMessageAsync(e.Message.Id);

            IReadOnlyList<DiscordReaction> reactionList = originalMessage.Reactions;
            DiscordReaction trophyReaction = reactionList.First(react => react.Emoji == e.Emoji);
            int newReactionCount = trophyReaction.Count;

            bool exists = await DoesStarExistInDBAsync(messageId);

            if (newReactionCount < guildFridgeInfo.Item2 || exists)
            {
                return;
            }

            var author = await e.Guild.GetMemberAsync(originalMessage.Author.Id);

            ulong roleIdToGive = await GetRoleToGiveToUser(e.Guild.Id);

            DiscordMember member = await e.Guild.GetMemberAsync(originalMessage.Author.Id).ConfigureAwait(false);
            var grantedRoles = member.Roles;

            if (roleIdToGive != 0)
            {
                DiscordRole role = e.Guild.GetRole(roleIdToGive);
                if (!grantedRoles.Contains(role))
                {
                    await member.GrantRoleAsync(role).ConfigureAwait(false);
                }
            }

            // Add to DB
            AddMessageToDb(originalMessage.Id, originalMessage.Author.Id, e.Guild.Id, originalMessage.ChannelId);

            var embed = new DiscordEmbedBuilder
            {
                Title = author.DisplayName,
                Timestamp = originalMessage.CreationTimestamp,
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = author.AvatarUrl
                }
            };

            if (string.IsNullOrEmpty(originalMessage.Content))
            {
                //look for attachment
                if (originalMessage.Attachments.Count <= 0)
                {
                    embed.AddField("Message", $"Could not find any relevant content in this message", false);
                }
            }
            else
            {
                embed.AddField("Message", originalMessage.Content, false);
            }

            embed.AddField("Original", $"[Jump!]({originalMessage.JumpLink.AbsoluteUri})", false);

            ulong fridgeChannelId = guildFridgeInfo.Item1;
            var fridgeChannel = await e.Client.GetChannelAsync(fridgeChannelId);
            await fridgeChannel.SendMessageAsync(originalMessage.Author.Mention, embed: embed).ConfigureAwait(false);
        }

        private static async void AddMessageToDb(ulong messageId, ulong userId, ulong guildId, ulong channelId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "Insert into fridgeBoard (DiscordID, MessageID, ChannelID, GuildID) " +
                "Values (?userId, ?messageId, ?channelId, ?guildId)";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?userId", MySqlDbType.VarChar, 40).Value = userId;
            command.Parameters.Add("?messageId", MySqlDbType.VarChar, 40).Value = messageId;
            command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
            command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
            try
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            catch (MySqlException mySqlException)
            {
                Console.Out.WriteLine(mySqlException.Message);
                Console.Out.WriteLine(mySqlException.StackTrace);
                Console.Out.WriteLine(mySqlException.SqlState);
            }
        }
        private static async Task<bool> DoesStarExistInDBAsync(ulong messageId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "select * from fridgeBoard where MessageID = ?msgId";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("msgId", MySqlDbType.VarChar, 40).Value = messageId;

            await connection.OpenAsync();
            MySqlDataReader reader = command.ExecuteReader();
            bool hasRows = reader.HasRows;
            reader.Close();
            await connection.CloseAsync();
            return hasRows;
        }

        private static async Task<bool> IsFridgeBoardSetup(ulong guildId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "select * from fridgeBoardConfig where GuildID = ?guildId";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
            await connection.OpenAsync();
            MySqlDataReader reader = command.ExecuteReader();
            bool hasRows = reader.HasRows;
            reader.Close();
            await connection.CloseAsync();
            return hasRows;
        }

        private static async Task<Tuple<ulong, int>> GetGuildFridgeInfo(ulong guildId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "select fridgeBoardChannelID, trophiesNeeded from fridgeBoardConfig where GuildID = ?guildId";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
            Tuple<ulong, int> result = new Tuple<ulong, int>(0, 0);
            await connection.OpenAsync();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                result = new Tuple<ulong, int>(reader.GetUInt64("fridgeBoardChannelID"), reader.GetInt32("trophiesNeeded"));
            }
            reader.Close();
            await connection.CloseAsync();
            return result;
        }

        private static async Task<ulong> GetRoleToGiveToUser(ulong guildId)
        {
            MySqlConnection connection = await GetDBConnectionAsync();
            string query = "select roleIdToGive from fridgeBoardConfig WHERE GuildID = ?guildId";
            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
            ulong value = 0;
            await connection.OpenAsync();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                value = reader.GetUInt64("roleIdToGive");
            }
            reader.Close();
            await connection.CloseAsync();
            return value;
        }

        private static async Task<MySqlConnection> GetDBConnectionAsync()
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
