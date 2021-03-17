namespace MaaslandDiscordBot.Commands.FiveM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.WebSocket;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BOT;
    using MaaslandDiscordBot.Models.FiveM;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;

    using OfficeOpenXml;

    public class LogCommands : BaseCommands
    {
        public override string Function => "log";

        private IDictionary<string, Tuple<string, Func<IUserMessage, MessageStore, Task>>> logActions = 
            new Dictionary<string, Tuple<string, Func<IUserMessage, MessageStore, Task>>>();

        public LogCommands()
        {
            logActions.Clear();
            logActions.Add("weapons_excel", new Tuple<string, Func<IUserMessage, MessageStore, Task>>("Wapens exporteren naar Excel document", LogWeaponsToExcel));

            RegisterCommand("-log", LogCommnad);
        }

        public async Task LogCommnad(string[] arguments, SocketMessage message)
        {
            var function = Function;
            var options = new Dictionary<string, string>();

            var discordBotMessage = await message.Channel.SendMessageAsync($"{message.Author.Mention} Ik ben opzoek naar de beschikbare opties");

            if (!logActions.Any())
            {
                await discordBotMessage.ModifyAsync(props =>
                {
                    props.Content = "Geen opties beschikbaar";
                    props.Embed = null;
                });

                return;
            }

            foreach (var logAction in logActions)
            {
                options.Add(logAction.Key, logAction.Value.Item1);
            }

            await discordBotMessage.ModifyAsync(props =>
            {
                props.Content = $"{message.Author.Mention} ik heb de volgende opties gevonden:";

                for (var i = 0; i < options.Count; i++)
                {
                    var description = options.ElementAt(i).Value;

                    props.Content += $"\n{BotExtensions.IndexToIcon(i)} **{description}**";
                }
            });

            var emojies = new List<IEmote>();

            for (var i = 0; i < options.Count; i++)
            {
                emojies.Add(new Emoji(BotExtensions.IndexToEmoji(i)));
            }

            var messageStore = new MessageStore
            {
                Command = function,
                MessageId = discordBotMessage.Id,
                UserId = message.Author.Id,
                ChannelId = message.Channel.Id,
                Reaction = string.Empty,
                Players = options
            };

            await AddOrUpdateRecord(messageStore);
            await discordBotMessage.AddReactionsAsync(emojies.ToArray());
        }

        public override async Task ActionHandler(Dictionary<string, string> options, IUserMessage message, MessageStore messageStore)
        {
            var index = messageStore.GetReaction() - 1;
            var option = options.ElementAt(index);
            var option_key = option.Key;

            if (logActions.ContainsKey(option_key))
            {
                await logActions[option_key].Item2(message, messageStore);
            }
        }

        public async Task LogWeaponsToExcel(IUserMessage message, MessageStore messageStore)
        {
            var data = new Dictionary<string, WeaponInfo>();
            var players = new Dictionary<string, string>();

            var mySqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mySqlConnection.OpenAsync();

            using (var mySqlCommand = new MySqlCommand("SELECT * FROM `users`", mySqlConnection))
            {
                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    while (await mySqlReader.ReadAsync())
                    {
                        var identifier = mySqlReader.GetString(0);
                        var playerName = await mySqlReader.IsDBNullAsync(3) ? "unknown" : mySqlReader.GetString(3);

                        if (players.ContainsKey(identifier))
                        {
                            players[identifier] = playerName;
                        }
                        else
                        {
                            players.Add(identifier, playerName);
                        }

                        var layoutRaw = await mySqlReader.IsDBNullAsync(9) ? "[]" : mySqlReader.GetString(9);

                        if (string.IsNullOrWhiteSpace(layoutRaw))
                        {
                            layoutRaw = "[]";
                        }

                        var layout = JsonConvert.DeserializeObject<List<Layout>>(layoutRaw);

                        if (layout.IsNullOrDefault())
                        {
                            layout = new List<Layout>();
                        }

                        foreach (var weaponLayout in layout)
                        {
                            var name = weaponLayout.Name.Trim().ToUpper();

                            if (weaponLayout.Count.IsNullOrDefault() || weaponLayout.Count <= default(int))
                            {
                                weaponLayout.Count = 1;
                            }

                            if (data.ContainsKey(name))
                            {
                                if (data[name].PlayerAmountOfWeapons.IsNullOrDefault())
                                {
                                    data[name].PlayerAmountOfWeapons = new Dictionary<string, int>();
                                }

                                data[name].AddNumberOfWeaponsToPlayer(identifier, weaponLayout.Count);
                            }
                            else
                            {
                                var weaponInfo = new WeaponInfo
                                {
                                    Name = name,
                                    PlayerAmountOfWeapons = new Dictionary<string, int>()
                                };

                                weaponInfo.AddNumberOfWeaponsToPlayer(identifier, weaponLayout.Count);

                                data.Add(name, weaponInfo);
                            }
                        }
                    }
                }
            }

            using (var mySqlCommand = new MySqlCommand("SELECT * FROM `datastore_data`", mySqlConnection))
            {
                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    while (await mySqlReader.ReadAsync())
                    {
                        var type = await mySqlReader.IsDBNullAsync(1) ? "unknown" : mySqlReader.GetString(1);
                        var owner = await mySqlReader.IsDBNullAsync(2) ? type : mySqlReader.GetString(2);
                        var rawData = await mySqlReader.IsDBNullAsync(3) ? "{}" : mySqlReader.GetString(3);

                        var weaponData = JsonConvert.DeserializeObject<WeaponData>(rawData);

                        if (weaponData.IsNullOrDefault())
                        {
                            weaponData = new WeaponData { Weapons = new Layout[] { } };
                        }

                        if (weaponData.Weapons.IsNullOrDefault())
                        {
                            weaponData.Weapons = new Layout[] { };
                        }

                        foreach (var weaponLayout in weaponData.Weapons)
                        {
                            var name = weaponLayout.Name.Trim().ToUpper();

                            if (weaponLayout.Count.IsNullOrDefault() || weaponLayout.Count <= default(int))
                            {
                                if (owner.ToLower().Trim().Contains("society"))
                                {
                                    weaponLayout.Count = 0;
                                } else
                                {
                                    weaponLayout.Count = 1;
                                }
                            }

                            if (data.ContainsKey(name))
                            {
                                if (data[name].PlayerAmountOfWeapons.IsNullOrDefault())
                                {
                                    data[name].PlayerAmountOfWeapons = new Dictionary<string, int>();
                                }

                                data[name].AddNumberOfWeaponsToPlayer(owner, weaponLayout.Count);
                            }
                            else
                            {
                                var weaponInfo = new WeaponInfo
                                {
                                    Name = name,
                                    PlayerAmountOfWeapons = new Dictionary<string, int>()
                                };

                                weaponInfo.AddNumberOfWeaponsToPlayer(owner, weaponLayout.Count);

                                data.Add(name, weaponInfo);
                            }
                        }
                    }
                }
            }

            using (var mySqlCommand = new MySqlCommand(
                "SELECT `ti`.*, `ov`.`owner`, `ov`.`type` FROM `trunk_inventory` AS `ti` LEFT JOIN `owned_vehicles` AS `ov` ON `ti`.`plate` = `ov`.`plate`",
                mySqlConnection))
            {
                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    while (await mySqlReader.ReadAsync())
                    {
                        var rawData = await mySqlReader.IsDBNullAsync(2) ? "{}" : mySqlReader.GetString(2);
                        var owner = await mySqlReader.IsDBNullAsync(4) ? "unknown" : mySqlReader.GetString(4);

                        var weaponData = JsonConvert.DeserializeObject<WeaponData>(rawData);

                        if (weaponData.IsNullOrDefault())
                        {
                            weaponData = new WeaponData { Weapons = new Layout[] { } };
                        }

                        if (weaponData.Weapons.IsNullOrDefault())
                        {
                            weaponData.Weapons = new Layout[] { };
                        }

                        foreach (var weaponLayout in weaponData.Weapons)
                        {
                            var name = weaponLayout.Name.Trim().ToUpper();

                            if (weaponLayout.Count.IsNullOrDefault() || weaponLayout.Count <= default(int))
                            {
                                weaponLayout.Count = 1;
                            }

                            if (data.ContainsKey(name))
                            {
                                if (data[name].PlayerAmountOfWeapons.IsNullOrDefault())
                                {
                                    data[name].PlayerAmountOfWeapons = new Dictionary<string, int>();
                                }

                                data[name].AddNumberOfWeaponsToPlayer(owner, weaponLayout.Count);
                            }
                            else
                            {
                                var weaponInfo = new WeaponInfo
                                {
                                    Name = name,
                                    PlayerAmountOfWeapons = new Dictionary<string, int>()
                                };

                                weaponInfo.AddNumberOfWeaponsToPlayer(owner, weaponLayout.Count);

                                data.Add(name, weaponInfo);
                            }
                        }
                    }
                }
            }

            using (var excel = new ExcelPackage())
            {
                var overviewIndex = 0;
                var overviewSheet = excel.Workbook.Worksheets.Add("Overzicht");

                var headers = new List<string[]>()
                {
                    new string[] { "Wapen", "Aantal" }
                };

                var headerRange = "A1:" + char.ConvertFromUtf32(headers[0].Length + 64) + "1";

                overviewSheet.Cells[headerRange].LoadFromArrays(headers);
                overviewSheet.Cells[headerRange].Style.Font.Bold = true;
                overviewSheet.Cells[headerRange].Style.Font.Color.SetColor(1, 0, 0, 0);
                overviewSheet.Cells[headerRange].AutoFilter = true;

                foreach (var weapon in data)
                {
                    var worksheet = excel.Workbook.Worksheets.Add(weapon.Key);
                    headers = new List<string[]>()
                    {
                        new string[] { "Aantal", "Naam", "Identifier" }
                    };

                    headerRange = "A1:" + char.ConvertFromUtf32(headers[0].Length + 64) + "1";

                    worksheet.Cells[headerRange].LoadFromArrays(headers);
                    worksheet.Cells[headerRange].Style.Font.Bold = true;
                    worksheet.Cells[headerRange].Style.Font.Color.SetColor(1, 0, 0, 0);
                    worksheet.Cells[headerRange].AutoFilter = true;

                    for (var i = 0; i < weapon.Value.PlayerAmountOfWeapons.Count; i++)
                    {
                        var weaponInfo = weapon.Value.PlayerAmountOfWeapons.ElementAt(i);
                        var playerName = players.ContainsKey(weaponInfo.Key) ? players[weaponInfo.Key] : null;

                        if (string.IsNullOrWhiteSpace(playerName) && weaponInfo.Key.Contains("society", true))
                        {
                            playerName = weaponInfo.Key.ToLower().Replace("society_", string.Empty)
                                .FirstLetterToUpperCaseOrConvertNullToEmptyString();
                        }
                        else if (string.IsNullOrWhiteSpace(playerName))
                        {
                            playerName = "Unknown";
                        }

                        var rowData = new List<string[]>()
                        {
                            new string[] { weaponInfo.Value.ToString(), playerName, weaponInfo.Key }
                        };

                        var cellRange = $"A{i+2}:{char.ConvertFromUtf32(rowData[0].Length + 64)}{i+2}";

                        worksheet.Cells[cellRange].LoadFromArrays(rowData);
                    }

                    var rawOverviewData = new List<string[]>()
                    {
                        new string[] { weapon.Key, weapon.Value.Count().ToString() }
                    };

                    var overviewCellRange = $"A{overviewIndex + 2}:{char.ConvertFromUtf32(rawOverviewData[0].Length + 64)}{overviewIndex + 2}";

                    overviewSheet.Cells[overviewCellRange].LoadFromArrays(rawOverviewData);

                    overviewIndex++;
                }

                using (var ms = new MemoryStream(excel.GetAsByteArray()))
                {
                    await message.Channel.SendFileAsync(ms, $"weapons_{DateTime.Now:yyyyMMddHHmmSS}.xlsx", $"{message.Author.Mention} Exports van alle wapens in Maasland {DateTime.Now:dd-MM-yyyy HH:mm:SS}");
                }
            }
        }
    }
}
