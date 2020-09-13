using System;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using ThePathBot.Attributes;
using DSharpPlus;
using MySql.Data.MySqlClient;
using ThePathBot.Utilities;
using DSharpPlus.Entities;
using System.Data.SqlClient;

namespace ThePathBot.Commands.Giveaway
{
    [Group("giveaway")]
    [Aliases("gw")]
    public class GiveawayCommands : BaseCommandModule
    {
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("configure")]
        [Aliases("setup")]
        [Description("Sets up configuration for hosting giveaways in this guild")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task ConfigureGiveaways(CommandContext ctx,
            [Description("Channel for giveaways to be posted in")] DiscordChannel channel)
        {
            try
            {
                InsertGiveawayConfig(ctx.Guild.Id, channel.Id);
                await ctx.Channel.SendMessageAsync($"Giveaway configuration update to post in {channel.Mention}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                await ctx.Channel.SendMessageAsync("Error occured during giveaway configuration").ConfigureAwait(false);
            }
        }

        [Command("create")]
        [Aliases("c")]
        [Description("Create a giveaway")]
        [FromChannel(746852898465644544)]
        public async Task CreateGiveaway(CommandContext ctx)
        {
            DiscordMessage deleteOnerror = null;
            try
            {
                await ctx.Channel.SendMessageAsync("What is the giveaway prize?").ConfigureAwait(false);

                var interactivity = ctx.Client.GetInteractivity();
                var prizeMsg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member,
                    TimeSpan.FromMinutes(4));
                if (prizeMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync(Formatter.Bold("Giveaway creation has timed out")).ConfigureAwait(false);
                    return;
                }
                string prize = prizeMsg.Result.Content;

                await ctx.Channel.SendMessageAsync("How many winners would you like?").ConfigureAwait(false);

                var numOfWinnersMsg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member,
                    TimeSpan.FromMinutes(4));
                if (numOfWinnersMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync(Formatter.Bold("Giveaway creation has timed out")).ConfigureAwait(false);
                    return;
                }

                if (!int.TryParse(numOfWinnersMsg.Result.Content, out int winners))
                {
                    await ctx.Channel.SendMessageAsync("This is not a number please start again").ConfigureAwait(false);
                    return;
                }

                await ctx.Channel.SendMessageAsync("How many hours until you would like the giveaway to end?").ConfigureAwait(false);
                var endDateMsg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member,
                    TimeSpan.FromMinutes(5));
                if (endDateMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync(Formatter.Bold("Giveaway creation has timed out")).ConfigureAwait(false);
                    return;
                }
                if (!int.TryParse(endDateMsg.Result.Content, out int hours))
                {
                    await ctx.Channel.SendMessageAsync("This is not a number please start again").ConfigureAwait(false);
                    return;
                }

                var endTime = DateTime.Now.AddHours(hours).ToShortTimeString();
                DateTime dateTime = DateTime.Parse(endTime);
                string formatForMySql = dateTime.ToString("yyyy-MM-dd HH:mm");

                ulong postChannelId = GetPostChannelId(ctx.Guild.Id);
                if (postChannelId == 0)
                {
                    await ctx.Channel.SendMessageAsync("This guild hasn't been configured for giveaways").ConfigureAwait(false);
                    return;
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Giveaway for: {prize}",
                    Description = $"React with :tada: to enter {ctx.Member.DisplayName}'s giveaway for: {Formatter.Bold(prize)}",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{winners} winners | Ending {dateTime.ToLongDateString()} {dateTime.ToShortTimeString()}"
                    }
                };
                embed.AddField("Host", $"{ctx.Member.Mention}");

                var gwayMsg = await ctx.Guild.GetChannel(postChannelId).SendMessageAsync(embed: embed).ConfigureAwait(false);
                await gwayMsg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":tada:"));
                deleteOnerror = gwayMsg;
                InsertGiveawayIntoDb(ctx.Guild.Id, ctx.Member.Id, gwayMsg.Id, formatForMySql, winners);
                await ctx.Channel.SendMessageAsync($"Giveaway posted {gwayMsg.JumpLink}");
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync("There was an error while creating your giveaway").ConfigureAwait(false);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                if (deleteOnerror != null)
                {
                    await deleteOnerror.DeleteAsync();
                }
            }
        }


        private ulong GetPostChannelId(ulong guildId)
        {
            ulong channelId = 0;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select ChannelID from giveawayConfig where GuildID = ?guildId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            channelId = reader.GetUInt64("ChannelID");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
            return channelId;
        }
        private void InsertGiveawayIntoDb(ulong guildId, ulong hostId, ulong MessageId, string endTime, int winners)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Insert into giveaways (GuildID, HostID, MessageID, EndTime, Winners)" +
                        " Values (?guildId, ?hostId, ?messageId, ?endTime, ?winners)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                    command.Parameters.Add("?hostId", MySqlDbType.VarChar, 40).Value = hostId;
                    command.Parameters.Add("?messageId", MySqlDbType.VarChar, 40).Value = MessageId;
                    command.Parameters.Add("?endTime", MySqlDbType.VarChar).Value = endTime;
                    command.Parameters.Add("?winners", MySqlDbType.Int32).Value = winners;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void InsertGiveawayConfig(ulong guildId, ulong channelId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Insert into giveawayConfig (GuildID, ChannelID) Values (?guildId, ?channelId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = guildId;
                    command.Parameters.Add("?channelId", MySqlDbType.VarChar, 40).Value = channelId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
