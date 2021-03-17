namespace MaaslandDiscordBot.Commands.Discord
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Rest;
    using global::Discord.WebSocket;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BOT;

    using MySql.Data.MySqlClient;

    public class DiscordCommands : BaseCommands
    {
        public override string Function => "discord";

        public DiscordCommands()
        {
            RegisterCommand("-discord", Discord);
        }

        public async Task Discord(string[] arguments, SocketMessage message)
        {
            await PlayerLookupCommand(arguments, message);
        }

        public override async Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index = messageStore.GetReaction() - 1;
            var player = players.ElementAt(index);
            var name = player.Value;
            var identifier = player.Key;

            using (var searchLogCommand = new MySqlCommand(
                "SELECT * FROM `user_logs` WHERE `identifier` = @identifier AND `discord` IS NOT NULL ORDER BY `date` DESC LIMIT 1",
                mysqlConnection))
            {
                searchLogCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var logReader = await searchLogCommand.ExecuteReaderAsync())
                {
                    if (!logReader.HasRows)
                    {
                        var embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                                Name = "Discord van Onbekend"
                            },
                            Color = Color.Purple,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "DISCORD ONBEKEND"
                            },
                            Fields = new List<EmbedFieldBuilder>(),
                            Timestamp = DateTimeOffset.Now,
                        };

                        embed.Fields.AddRange(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord ID",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord Naam",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord actief vanaf",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Snelkoppeling",
                                Value = "-"
                            },
                        });

                        await message.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });

                        await mysqlConnection.CloseAsync();

                        return;
                    }

                    while (logReader.Read())
                    {
                        var embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                                Name = $"Discord van {name}"
                            },
                            Color = Color.Purple,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "DISCORD " + name.ToUpper()
                            },
                            Fields = new List<EmbedFieldBuilder>(),
                            Timestamp = DateTimeOffset.Now,
                        };

                        var userId = Convert.ToUInt64(logReader.GetString(6));
                        RestUser discordUser = null;

                        try
                        {
                            var discordBot = new DiscordRestClient(
                                new DiscordSocketConfig());

                            await discordBot.LoginAsync(TokenType.Bot, BotConfiguration.Token);

                            discordUser = await discordBot.GetUserAsync(userId);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        if (discordUser.IsNullOrDefault())
                        {
                            await message.ModifyAsync(properties =>
                            {
                                properties.Content = $"{message.Author.Mention} Kan de discord van {name} niet vinden";
                                properties.Embed = null;
                            });

                            return;
                        }

                        embed.Author.IconUrl = discordUser.GetAvatarUrl();
                        embed.Fields.AddRange(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord ID",
                                Value = Convert.ToUInt64(logReader.GetString(6))
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord Naam",
                                Value = discordUser.Username
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Discord actief vanaf",
                                Value = discordUser.CreatedAt.DateTime.ToLongDateString() + " " + discordUser.CreatedAt.DateTime.ToLongTimeString()
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Snelkoppeling",
                                Value = discordUser.Mention
                            },
                        });

                        await message.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });

                        await mysqlConnection.CloseAsync();

                        return;
                    }
                }
            }

            await mysqlConnection.CloseAsync();
        }
    }
}
