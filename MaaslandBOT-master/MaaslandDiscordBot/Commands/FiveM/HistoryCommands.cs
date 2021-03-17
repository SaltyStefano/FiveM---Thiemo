namespace MaaslandDiscordBot.Commands.FiveM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BOT;
    using MaaslandDiscordBot.Models.FiveM;

    using MySql.Data.MySqlClient;

    public class HistoryCommands : BaseCommands
    {
        public override string Function => "history";

        public HistoryCommands()
        {
            RegisterCommand("-history", PlayerLookupCommand);
        }

        public override async Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index = messageStore.GetReaction() - 1;
            var identifier = players.ElementAt(index).Key;


            var avatar = await identifier.GetAvatarByIdentifier();
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = avatar,
                    Name = $"Geschiedenis van {players.ElementAt(index).Value} op Maasland?"
                },
                Color = Color.Blue,
                Footer = new EmbedFooterBuilder
                {
                    Text = "HISTORY OF " + players.ElementAt(index).Value.ToUpper()
                },
                Fields = new List<EmbedFieldBuilder>(),
                Timestamp = DateTimeOffset.Now,
            };

            var history = await GetPlayerJoinHistory(identifier);

            embed.AddField("Voor het eerst op Maasland", history.FirstJoin.HasValue ? history.FirstJoin.Value.ToString("dd-MM-yyy HH:MM:ss") : "VOOR 04-11-2019 (GEEN LOG)");
            embed.AddField("Voor het laatst op Maasland", history.LastJoin.HasValue ? history.LastJoin.Value.ToString("dd-MM-yyy HH:MM:ss") : "VOOR 04-11-2019 (GEEN LOG)");
            embed.AddField("Aantal dagen ingelogd op Maasland vanaf 04-11-2019", history.TotalNumberOfDaysPlayed);
            embed.AddField("Aantal dagen online de afgelopen 30 dagen", history.NumberOfDaysPlayedLastThirtyDays);

            await message.ModifyAsync(properties =>
            {
                properties.Embed = embed.Build();
            });

            await mysqlConnection.CloseAsync();
        }

        public async Task<PlayerJoinHistory> GetPlayerJoinHistory(string identifier)
        {
            var mySqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mySqlConnection.OpenAsync();

            var firstJoin = DateTime.Now;
            var lastJoin = DateTime.MinValue;
            var today = DateTime.Now;
            var totalNumberOfDaysPlayed = 0;
            var numberOfDaysPlayedLast30Days = 0;
            var thirtyDaysAgoFromNow = today.AddDays(-30);
            var thirtyDaysAgo = new DateTime(thirtyDaysAgoFromNow.Year, thirtyDaysAgoFromNow.Month, thirtyDaysAgoFromNow.Day);

            using (var mySqlCommand =
                new MySqlCommand("SELECT DATE_FORMAT(`date`, '%Y') AS `year`, DATE_FORMAT(`date`, '%m') AS `month`, DATE_FORMAT(`date`, '%d') AS `day`, DATE_FORMAT(`date`, '%H') AS `hour`, DATE_FORMAT(`date`, '%i') AS `min` FROM `user_logs` WHERE `identifier` = @identifier GROUP BY DATE_FORMAT(`date`, '%Y%m%d')",  mySqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mySqlReader.HasRows)
                    {
                        await mySqlConnection.CloseAsync();

                        return new PlayerJoinHistory();
                    }

                    while (await mySqlReader.ReadAsync())
                    {
                        var year = (await mySqlReader.GetValue<string>("year") ?? "0").ToInt();
                        var month = (await mySqlReader.GetValue<string>("month") ?? "0").ToInt();
                        var day = (await mySqlReader.GetValue<string>("day") ?? "0").ToInt();
                        var hour = (await mySqlReader.GetValue<string>("hour") ?? "0").ToInt();
                        var min = (await mySqlReader.GetValue<string>("min") ?? "0").ToInt();

                        var currentRowDateRecord = new DateTime(year, month, day, hour, min, default);

                        totalNumberOfDaysPlayed++;

                        if (currentRowDateRecord > thirtyDaysAgo)
                        {
                            numberOfDaysPlayedLast30Days++;
                        }

                        if (firstJoin > currentRowDateRecord)
                        {
                            firstJoin = currentRowDateRecord;
                        }

                        if (lastJoin < currentRowDateRecord)
                        {
                            lastJoin = currentRowDateRecord;
                        }
                    }
                }
            }

            await mySqlConnection.CloseAsync();

            return new PlayerJoinHistory
            {
                FirstJoin = firstJoin,
                LastJoin = lastJoin,
                TotalNumberOfDaysPlayed = totalNumberOfDaysPlayed,
                NumberOfDaysPlayedLastThirtyDays = numberOfDaysPlayedLast30Days
            };
        }
    }
}
