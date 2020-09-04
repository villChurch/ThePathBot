using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Services
{
    public class TicketService
    {
        private static readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public static async Task CreateTicket(DiscordChannel channel, DiscordGuild guild, string content, string type)
        {
            MySqlConnection connection = await GetDBConnectionAsync();

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
