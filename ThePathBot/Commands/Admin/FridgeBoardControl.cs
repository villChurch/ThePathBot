using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.Admin
{
    public class FridgeBoardControl : BaseCommandModule
    {

        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("fridgeSetup")]
        [Aliases("fs")]
        [Description("Starts a dialogue to setup the fridge board for your guild")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageChannels)]
        public async Task SetupFridgeChannel(CommandContext ctx, DiscordChannel channel)
        {
            ulong guildId = ctx.Guild.Id;
            ulong userId = ctx.Member.Id;
            DiscordRole roleToGive = null;

            var interactivity = ctx.Client.GetInteractivity();

            await ctx.Channel.SendMessageAsync("How many trophies are needed to reach the fridge?").ConfigureAwait(false);

            var msg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member).ConfigureAwait(false);

            await ctx.Channel.SendMessageAsync("What role do you want to give recipients? If none type none").ConfigureAwait(false);

            var roleMsg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member).ConfigureAwait(false);

            if (roleMsg.TimedOut)
            {
                await ctx.Channel.SendMessageAsync("Dialouge timed out, please start again").ConfigureAwait(false);
                return;
            }

            if (roleMsg.Result.Content.ToLower() != "none")
            {
                if (roleMsg.Result.MentionedRoles.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("You did not mention a role please start again").ConfigureAwait(false);
                    return;
                }
                roleToGive = roleMsg.Result.MentionedRoles[0];
            }

            if (!int.TryParse(msg.Result.Content, out int trophiesNeeded))
            {
                await ctx.Channel.SendMessageAsync("You did not enter a number please try the command again").ConfigureAwait(false);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Insert into fridgeBoardConfig (GuildID, fridgeBoardChannelID, UpdatedByID, trophiesNeeded, roleIdToGive) " +
                        "Values (?guildId, ?fbcId, ?updatedId, ?trophies, ?roleIdToGive) ON Duplicate KEY UPDATE fridgeBoardChannelID = ?fbcId, " +
                        "UpdatedByID = ?updatedId, trophiesNeeded = ?trophies, roleIdToGive = ?roleIdToGive";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = ctx.Guild.Id;
                    command.Parameters.Add("?fbcId", MySqlDbType.VarChar, 40).Value = channel.Id;
                    command.Parameters.Add("?updatedId", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    command.Parameters.Add("?trophies", MySqlDbType.Int32).Value = trophiesNeeded;
                    command.Parameters.Add("?roleIdToGive", MySqlDbType.VarChar, 40).Value = roleToGive?.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Fridge Board config updated to post in {channel.Mention} and needing {trophiesNeeded} :trophy:").ConfigureAwait(false);
            }
            catch (MySqlException mySqlEx)
            {
                Console.Out.WriteLine(mySqlEx.Message);
                Console.Out.WriteLine(mySqlEx.StackTrace);
                Console.Out.WriteLine(mySqlEx.SqlState);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
