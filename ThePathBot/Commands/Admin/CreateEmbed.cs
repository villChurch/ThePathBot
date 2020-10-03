using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using ThePathBot.Attributes;
using ThePathBot.Utilities;

namespace ThePathBot.Commands.Admin
{
    [Group("embed")]
    public class CreateEmbed : BaseCommandModule
    {
        private readonly DBConnectionUtils dBConnectionUtils = new DBConnectionUtils();

        [Command("create")]
        //[Aliases("c")]
        [Description("starts a dialouge to create an embed")]
        [OwnerOrPermission(DSharpPlus.Permissions.Administrator)]
        public async Task CreateNewEmbed(CommandContext ctx, [Description("channel to post in")] DiscordChannel channel)
        {
            try
            {
                var interactivity = ctx.Client.GetInteractivity();
                await ctx.Channel.SendMessageAsync("What should the title of the embed be?").ConfigureAwait(false);
                var titleResponse = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Message.Author && !string.IsNullOrEmpty(x.Content),
                    TimeSpan.FromMinutes(5));

                if (titleResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }
                string title = titleResponse.Result.Content;

                await ctx.Channel.SendMessageAsync("What should the embed say?").ConfigureAwait(false);
                var contentResponse = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Message.Author && !string.IsNullOrEmpty(x.Content),
                    TimeSpan.FromMinutes(5));
                if (contentResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }
                string content = contentResponse.Result.Content;

                DiscordEmoji thumbsup = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                DiscordEmoji thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
                var imageReaction = await ctx.Channel.SendMessageAsync("Do you want to add an image? Use :thumbsup: for yes and :thumbsdown: for no").ConfigureAwait(false);
                await imageReaction.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:")).ConfigureAwait(false);
                await imageReaction.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:")).ConfigureAwait(false);
                var imageReactionResponse = await interactivity.WaitForReactionAsync(x => x.Message == imageReaction && x.User == ctx.User &&
                (x.Emoji == thumbsup || x.Emoji == thumbsDown), TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (imageReactionResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                string imageUrl = "";
                if (imageReactionResponse.Result.Emoji == thumbsup)
                {
                    await ctx.Channel.SendMessageAsync("Please send the image you want to use").ConfigureAwait(false);
                    var imageResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel && x.Attachments.Count > 0,
                        TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                    if (imageResponse.TimedOut)
                    {
                        await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                        return;
                    }
                    imageUrl = imageResponse.Result.Attachments[0].Url;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Title = title,
                    Description = content,
                    Color = DiscordColor.Aquamarine,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Create by {ctx.Member.DisplayName} at {ctx.Message.CreationTimestamp}"
                    }
                };

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    embed.ImageUrl = imageUrl;
                }

                var embedMsg = await channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync($"Your embed has been sent in {channel.Mention}").ConfigureAwait(false);
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "INSERT INTO createdEmbeds (DiscordChannel, DiscordID, MessageID) VALUES (?discordChannel, ?discordId, ?msgId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordChannel", MySqlDbType.VarChar, 40).Value = channel.Id;
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    command.Parameters.Add("?msgId", MySqlDbType.VarChar, 40).Value = embedMsg.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("edit")]
        [Description("edits a previously posted embed")]
        [OwnerOrPermission(DSharpPlus.Permissions.Administrator)]
        public async Task EditEmbed(CommandContext ctx, ulong msgId)
        {
            try
            {
                bool hasRows = false;
                ulong channelId = 0;
                using (MySqlConnection connection = new MySqlConnection(dBConnectionUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select * from createdEmbeds Where MessageID = ?msgId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?msgId", MySqlDbType.VarChar, 40).Value = msgId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    hasRows = reader.HasRows;
                    if (hasRows)
                    {
                        while (reader.Read())
                        {
                            channelId = reader.GetUInt64("DiscordChannel");
                        }
                    }
                }
                if (!hasRows)
                {
                    await ctx.Channel.SendMessageAsync("Looks like I didn't send this message or it was not created using the command `?create embed`.").ConfigureAwait(false);
                    return;
                }
                DiscordChannel channel = ctx.Guild.GetChannel(channelId);
                DiscordMessage msg = await channel.GetMessageAsync(msgId);
                var currentEmbed = msg.Embeds[0];

                var interactivity = ctx.Client.GetInteractivity();
                DiscordEmoji thumbsup = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                DiscordEmoji thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
                var titleQMsg = await ctx.Channel.SendMessageAsync($"Do you want to change the title? Current title is: {currentEmbed.Title}.");
                await titleQMsg.CreateReactionAsync(thumbsup);
                await titleQMsg.CreateReactionAsync(thumbsDown);
                var titleQMsgResponse = await interactivity.WaitForReactionAsync(reaction => reaction.Message == titleQMsg && reaction.User == ctx.User &&
                (reaction.Emoji == thumbsDown || reaction.Emoji == thumbsup), TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (titleQMsgResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }
                string title;
                if (titleQMsgResponse.Result.Emoji == thumbsup)
                {
                    await ctx.Channel.SendMessageAsync("Please enter the new title").ConfigureAwait(false);
                    var newTitleResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel && !string.IsNullOrEmpty(x.Content),
                        TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                    if (newTitleResponse.TimedOut)
                    {
                        await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                        return;
                    }
                    title = newTitleResponse.Result.Content;
                }
                else
                {
                    title = currentEmbed.Title;
                }

                var descriptionQMsg = await ctx.Channel.SendMessageAsync($"Do you want to change the description? Current description is: {currentEmbed.Description}").ConfigureAwait(false);
                await descriptionQMsg.CreateReactionAsync(thumbsup);
                await descriptionQMsg.CreateReactionAsync(thumbsDown);
                var descriptionQMsgResponse = await interactivity.WaitForReactionAsync(reaction => reaction.Message == descriptionQMsg && reaction.User == ctx.User
                 && (reaction.Emoji == thumbsDown || reaction.Emoji == thumbsup), TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (descriptionQMsgResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }
                string description;
                if (descriptionQMsgResponse.Result.Emoji == thumbsup)
                {
                    await ctx.Channel.SendMessageAsync("Please enter the new description").ConfigureAwait(false);
                    var newDescriptionResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel && !string.IsNullOrEmpty(x.Content),
                        TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                    if (newDescriptionResponse.TimedOut)
                    {
                        await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                        return;
                    }
                    description = newDescriptionResponse.Result.Content;
                }
                else
                {
                    description = currentEmbed.Description;
                }

                var imageQMsg = await ctx.Channel.SendMessageAsync($"Do you want to change the image? Current image url is: {currentEmbed.Image.Url}").ConfigureAwait(false);
                await imageQMsg.CreateReactionAsync(thumbsup);
                await imageQMsg.CreateReactionAsync(thumbsDown);
                var imageQMsgResponse = await interactivity.WaitForReactionAsync(reaction => reaction.Message == imageQMsg && reaction.User == ctx.User
                 && (reaction.Emoji == thumbsDown || reaction.Emoji == thumbsup), TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (imageQMsgResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }
                string imageUrl;
                if (imageQMsgResponse.Result.Emoji == thumbsup)
                {
                    await ctx.Channel.SendMessageAsync("Please send the new image for this embed").ConfigureAwait(false);
                    var newImageUrlResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel && x.Attachments.Count > 0,
                        TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                    if (newImageUrlResponse.TimedOut)
                    {
                        await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                        return;
                    }
                    imageUrl = newImageUrlResponse.Result.Attachments[0].Url;
                }
                else
                {
                    if (string.IsNullOrEmpty(currentEmbed.Image.Url.ToString()))
                    {
                        imageUrl = "";
                    }
                    else
                    {
                        imageUrl = currentEmbed.Image.Url.ToString();
                    }
                }
                var newEmbed = new DiscordEmbedBuilder
                {
                    Title = title,
                    Description = description,
                    Color = DiscordColor.Aquamarine,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Edited by {ctx.Member.DisplayName} at {ctx.Message.CreationTimestamp}"
                    }
                };

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    newEmbed.ImageUrl = imageUrl;
                }

                DiscordEmbed newEmbedToPost = newEmbed;
                await msg.ModifyAsync(embed: newEmbedToPost).ConfigureAwait(false);
                await ctx.Channel.SendMessageAsync("Embed has been modified").ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
