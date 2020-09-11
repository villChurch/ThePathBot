using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using ThePathBot.Models;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.TipSystem
{
    public class TipHistory : BaseCommandModule
    {
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("tiphistory")]
        [Description("returns your last ten tips")]
        public async Task GetTipHistory(CommandContext ctx)
        {
            List<Tip> tips = new List<Tip>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {

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
                }

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
                    userName = ctx.Guild.GetMemberAsync(item.SenderId).Result.DisplayName;
                    Page nextPage = new Page();
                    nextPage.Embed = new DiscordEmbedBuilder
                    {
                        Title = $"Reivew from {userName}",
                        Description = $"{item.Message}",
                        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                        {
                            Url = ctx.Guild.GetMemberAsync(item.SenderId).Result.GetAvatarUrl(DSharpPlus.ImageFormat.Png)
                        },
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
