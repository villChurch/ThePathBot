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

        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Command("fridgeSetup")]
        [Aliases("fs")]
        [Description("Starts a DM session to setup the fridge board for your guild")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageChannels)]
        public async Task SetupFridgeChannel(CommandContext ctx, DiscordChannel channel)
        {
            ulong guildId = ctx.Guild.Id;
            ulong userId = ctx.Member.Id;

            var interactivity = ctx.Client.GetInteractivity();

            await ctx.Channel.SendMessageAsync("How many trophies are needed to reach the fridge?").ConfigureAwait(false);

            var msg = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Member).ConfigureAwait(false);

            if (!int.TryParse(msg.Result.Content, out int trophiesNeeded))
            {
                await ctx.Channel.SendMessageAsync("You did not enter a number please try the command again").ConfigureAwait(false);
                return;
            }

            try
            {
                MySqlConnection connection = await GetDBConnectionAsync();
                string query = "Insert into fridgeBoardConfig (GuildID, fridgeBoardChannelID, UpdatedByID, trophiesNeeded) " +
                    "Values (?guildId, ?fbcId, ?updatedId, ?trophies) ON Duplicate KEY UPDATE fridgeBoardChannelID = ?fbcId, " +
                    "UpdatedByID = ?updatedId, trophiesNeeded = ?trophies";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?guildId", MySqlDbType.VarChar, 40).Value = ctx.Guild.Id;
                command.Parameters.Add("?fbcId", MySqlDbType.VarChar, 40).Value = channel.Id;
                command.Parameters.Add("?updatedId", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                command.Parameters.Add("?trophies", MySqlDbType.Int32).Value = trophiesNeeded;
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
                await ctx.Channel.SendMessageAsync($"Fridge Board config updated to post in {channel.Mention} and needing {trophiesNeeded} :trophy:").ConfigureAwait(false);
            }
            catch (MySqlException mySqlEx)
            {
                Console.Out.WriteLine(mySqlEx.Message);
                Console.Out.WriteLine(mySqlEx.StackTrace);
                Console.Out.WriteLine(mySqlEx.SqlState);
            }
        }

        private async Task<MySqlConnection> GetDBConnectionAsync()
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
