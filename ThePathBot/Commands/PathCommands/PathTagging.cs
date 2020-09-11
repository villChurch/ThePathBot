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
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("addtag")]
        [Description("Add a tag for a path")]
        public async Task addTag(CommandContext ctx, params string[] tagContent)
        {
            try
            {
                string pathLink = tagContent[0];
                tagContent[0] = "";
                string tag = String.Join(" ", tagContent);

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "INSERT INTO pathTags (tagName, tagLink) values (?tagName, ?tagLink)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?tagName", MySqlDbType.VarChar, 2500).Value = tag.Trim();
                    command.Parameters.Add("?tagLink", MySqlDbType.VarChar, 2500).Value = pathLink.Trim();
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync("Added tag <" + pathLink + "> to the list of tagged paths")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("listtags")]
        [Description("List all tags")]
        public async Task listTags(CommandContext ctx)
        {
            try
            {
                string query = "Select tagName from pathTags";

                StringBuilder sb = new StringBuilder();
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        sb.AppendLine(reader.GetString("tagName"));
                    }
                }
                var interactivity = ctx.Client.GetInteractivity();
                var tagsPages =
                    interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, tagsPages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("showtag")]
        [Description("Gets a tag if set")]
        public async Task getTag(CommandContext ctx, params string[] tagName)
        {
            try
            {
                string tag = string.Join(" ", tagName);
                string tagLink = "";
                string query = "Select tagLink from pathTags WHERE tagName = ?tagName";

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?tagName", MySqlDbType.VarChar, 255).Value = tag.Trim();
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        tagLink = reader.GetString("tagLink");
                    }
                }
                await ctx.Channel.SendMessageAsync(tagLink).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("removetag")]
        [Description("removes selected tag")]
        public async Task removeTag(CommandContext ctx, [Description("name of tag to remove")] params string[] pathName)
        {
            try
            {
                string tag = string.Join(" ", pathName);
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemoveTag", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("tagName", MySqlDbType.VarChar, 40).Value = tag;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync("Deleted tag called " + tag).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

    }
}
