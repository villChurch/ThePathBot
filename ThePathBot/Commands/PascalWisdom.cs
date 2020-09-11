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
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("wisdom")]
        public async Task getWisdom(CommandContext ctx)
        {
            try
            {
                List<string> quotes = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select text from pascalWisdom";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        quotes.Add(reader.GetString("text"));
                    }

                }

                Random rnd = new Random();
                int quoteNumber = rnd.Next(0, quotes.Count);

                var embed = new DiscordEmbedBuilder
                {
                    Description = quotes[quoteNumber].Trim(),
                    Color = DiscordColor.Blurple
                };

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
