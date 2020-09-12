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
            //MySqlConnection connection = await GetDBConnectionAsync();

        }
    }
}
