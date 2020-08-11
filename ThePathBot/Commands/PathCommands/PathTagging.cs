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

namespace ThePathBot.Commands.PathCommands
{
    public class PathTagging : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Command("addtag")]
        [Description("Add a tag for a path")]
        public async Task addTag(CommandContext ctx, params string[] tagContent)
        {
            string pathLink = tagContent[0];
            tagContent[0] = "";
            string tag = String.Join(" ", tagContent);

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
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);

            string query = "INSERT INTO pathTags (tagName, tagLink) values (?tagName, ?tagLink)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.Add("?tagName", MySqlDbType.VarChar, 2500).Value = tag.Trim();
            command.Parameters.Add("?tagLink", MySqlDbType.VarChar, 2500).Value = pathLink.Trim();
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
            await ctx.Channel.SendMessageAsync("Added tag <" + pathLink + "> to the list of tagged paths")
                .ConfigureAwait(false);
        }

        [Command("listtags")]
        [Description("List all tags")]
        public async Task listTags(CommandContext ctx)
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
            string query = "Select tagName from pathTags";

            StringBuilder sb = new StringBuilder();
            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
            var command = new MySqlCommand(query, connection);
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                sb.AppendLine(reader.GetString("tagName"));
            }

            connection.Close();

            var interactivity = ctx.Client.GetInteractivity();
            var tagsPages =
                interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, tagsPages)
                .ConfigureAwait(false);
        }

        [Command("showtag")]
        [Description("Gets a tag if set")]
        public async Task getTag(CommandContext ctx, params string[] tagName)
        {
            string tag = String.Join(" ", tagName);

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
            string tagLink = "";
            string query = "Select tagLink from pathTags WHERE tagName = ?tagName";

            MySqlConnection connection = new MySqlConnection(dbCon.connectionString);
            var command = new MySqlCommand(query, connection);
            command.Parameters.Add("?tagName", MySqlDbType.VarChar, 255).Value = tag.Trim();
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                tagLink = reader.GetString("tagLink");
            }

            connection.Close();

            await ctx.Channel.SendMessageAsync(tagLink).ConfigureAwait(false);
        }

        [Command("removetag")]
        [Description("removes selected tag")]
        public async Task removeTag(CommandContext ctx, [Description("name of tag to remove")] params string[] pathName)
        {
            string tag = string.Join(" ", pathName);
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

            MySqlCommand command = new MySqlCommand("RemoveTag", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.Add("tagName", MySqlDbType.VarChar, 40).Value = tag;
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();

            await ctx.Channel.SendMessageAsync("Deleted tag called " + tag).ConfigureAwait(false);
        }

    }
}
