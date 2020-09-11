using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MySql.Data.MySqlClient;
using ThePathBot.Utilities;

namespace ThePathBot.Listeners
{
    public class ReactionService
    {
        public ReactionService()
        {
        }

        public static async Task CheckReactionAddedIsFridgeReaction(MessageReactionAddEventArgs e)
        {
            var fridgeEmoji = DiscordEmoji.FromName(e.Client, ":trophy:");
            bool guildSetup = IsFridgeBoardSetup(e.Guild.Id);
            if (!guildSetup || e.Emoji != fridgeEmoji)
            {
                return;
            }
            var messageId = e.Message.Id;

            Tuple<ulong, int> guildFridgeInfo = GetGuildFridgeInfo(e.Guild.Id);
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

            bool exists = DoesStarExistInDBAsync(messageId);

            if (newReactionCount < guildFridgeInfo.Item2 || exists)
            {
                return;
            }

            var author = await e.Guild.GetMemberAsync(originalMessage.Author.Id);

            ulong roleIdToGive = GetRoleToGiveToUser(e.Guild.Id);

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
                else
                {
                    embed.ImageUrl = originalMessage.Attachments[0].Url;
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

        private static void AddMessageToDb(ulong messageId, ulong userId, ulong guildId, ulong channelId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(DBConnectionUtils.ReturnPopulatedConnectionStringStatic()))
                {
                    string query = "Insert into fridgeBoard (DiscordID, MessageID, ChannelID, GuildID) " +
                    "Values (?userId, ?messageId, ?channelId, ?guildId)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?userId", MySqlDbType.VarChar, 40).Value = userId;
                    command.Parameters.Add("?messageId", MySqlDbType.VarChar, 40).Value = messageId;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                    command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (MySqlException mySqlException)
            {
                Console.Out.WriteLine(mySqlException.Message);
                Console.Out.WriteLine(mySqlException.StackTrace);
                Console.Out.WriteLine(mySqlException.SqlState);
            }
        }
        private static bool DoesStarExistInDBAsync(ulong messageId)
        {
            bool hasRows = false;
            using (MySqlConnection connection = new MySqlConnection(DBConnectionUtils.ReturnPopulatedConnectionStringStatic()))
            {
                string query = "select * from fridgeBoard where MessageID = ?msgId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("msgId", MySqlDbType.VarChar, 40).Value = messageId;

                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                hasRows = reader.HasRows;
                reader.Close();
            }
            return hasRows;
        }

        private static bool IsFridgeBoardSetup(ulong guildId)
        {
            bool hasRows = false;
            using (MySqlConnection connection = new MySqlConnection(DBConnectionUtils.ReturnPopulatedConnectionStringStatic()))
            {
                string query = "select * from fridgeBoardConfig where GuildID = ?guildId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                hasRows = reader.HasRows;
                reader.Close();
            }
            return hasRows;
        }

        private static Tuple<ulong, int> GetGuildFridgeInfo(ulong guildId)
        {
            Tuple<ulong, int> result = new Tuple<ulong, int>(0, 0);
            using (MySqlConnection connection = new MySqlConnection(DBConnectionUtils.ReturnPopulatedConnectionStringStatic()))
            {
                string query = "select fridgeBoardChannelID, trophiesNeeded from fridgeBoardConfig where GuildID = ?guildId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = new Tuple<ulong, int>(reader.GetUInt64("fridgeBoardChannelID"), reader.GetInt32("trophiesNeeded"));
                }
                reader.Close();
            }
            return result;
        }

        private static ulong GetRoleToGiveToUser(ulong guildId)
        {
            ulong value = 0;
            using (MySqlConnection connection = new MySqlConnection(DBConnectionUtils.ReturnPopulatedConnectionStringStatic()))
            {
                string query = "select roleIdToGive from fridgeBoardConfig WHERE GuildID = ?guildId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    value = reader.GetUInt64("roleIdToGive");
                }
                reader.Close();
            }
            return value;
        }
    }
}
