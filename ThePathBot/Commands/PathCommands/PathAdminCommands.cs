using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.PathCommands
{
    public class PathAdminCommands : BaseCommandModule
    {
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("showpaths")]
        [Description("show paths for mentioned user")]
        //[RequirePermissions(Permissions.KickMembers)]
        [RequireOwner]
        [Hidden]
        public async Task showPaths(CommandContext ctx, [Description("mention the user you want to get the paths of")]
            params string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }
            var mentions = ctx.Message.MentionedUsers;
            Console.Out.WriteLine(mentions[0].Id.ToString());
            try
            {
                Dictionary<String, String> paths = new Dictionary<string, string>();
                bool specificPath = false;
                string query = "Select link, pathname from pathLinks WHERE DiscordID = ?discordID";

                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordid", MySqlDbType.VarChar, 40).Value = mentions[0].Id.ToString();
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        paths.Add(reader.GetString("pathname"), reader.GetString("link"));
                    }
                }
                StringBuilder sb = new StringBuilder();
                foreach (var pathsKey in paths.Keys)
                {
                    sb.AppendLine(pathsKey + " - " + paths[pathsKey]);
                    // pathEmbed.AddField(pathsKey, paths[pathsKey], false);
                }

                var interactivity = ctx.Client.GetInteractivity();
                var pathsPages =
                    interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pathsPages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
