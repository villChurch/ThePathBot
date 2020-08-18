using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThePathBot.Models;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.TipSystem
{
    public class TipHistory : BaseCommandModule
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Command("tiphistory")]
        [Description("returns your last ten tips")]
        public async Task GetTipHistory(CommandContext ctx)
        {
            List<Tip> tips = new List<Tip>();
            try
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

                string query = "SELECT * FROM `pathTips` WHERE RecipientID = ?recipient ORDER BY TimeStamp DESC LIMIT 10";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.Add("?recipient", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Tip tip = new Tip(reader.GetUInt64("SenderId"), reader.GetString("Message"), reader.GetString("TimeStamp"));
                        tips.Add(tip);
                    }
                }

                reader.Close();
                connection.Close();

                if (tips.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("You do not have any tips yet").ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
            List<Page> pages = new List<Page>();
            int counter = 1;
            foreach (var item in tips)
            {
                string userName;
                try
                {
                    userName = ctx.Guild.GetMemberAsync((ulong)item.SenderId).Result.DisplayName;
                    Page nextPage = new Page();
                    nextPage.Embed = new DiscordEmbedBuilder
                    {
                        Title = $"Reivew from {userName}",
                        Description = $"{item.Message}",
                        ThumbnailUrl = ctx.Guild.GetMemberAsync((ulong)item.SenderId).Result.GetAvatarUrl(DSharpPlus.ImageFormat.Png),
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Tip {counter}/{tips.Count} - Recived at {item.Timestamp}"
                        },
                        Color = DiscordColor.Blurple
                    };
                    pages.Add(nextPage);
                }
                catch (Exception)
                {
                    userName = item.SenderId.ToString();
                    Page nextPage = new Page();
                    nextPage.Embed = new DiscordEmbedBuilder
                    {
                        Title = $"Reivew from {userName}",
                        Description = $"{item.Message}",
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = $"Tip {counter}/{tips.Count} - Recived at {item.Timestamp}"
                        },
                        Color = DiscordColor.Blurple
                    };
                    pages.Add(nextPage);
                }
                counter++;
            }

            var interactivity = ctx.Client.GetInteractivity();

            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
                .ConfigureAwait(false);

        }
    }
}
