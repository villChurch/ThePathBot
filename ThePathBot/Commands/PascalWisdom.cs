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

namespace ThePathBot.Commands
{
    public class PascalWisdom : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Command("wisdom")]
        public async Task getWisdom(CommandContext ctx)
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
            string query = "Select text from pascalWisdom";

            List<string> quotes = new List<string>();
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
            var command = new MySqlCommand(query, connection);
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                quotes.Add(reader.GetString("text"));
            }

            connection.Close();

            Random rnd = new Random();
            int quoteNumber = rnd.Next(0, quotes.Count);

            var embed = new DiscordEmbedBuilder
            {
                Description = quotes[quoteNumber].Trim(),
                Color = DiscordColor.Blurple
            };

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }
    }
}
