namespace MaaslandDiscordBot.Commands.FiveM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using CsvHelper;

    using global::Discord;
    using global::Discord.WebSocket;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BanLists;
    using MaaslandDiscordBot.Models.BOT;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;

    public class WipeCommands : BaseCommands
    {
        public override string Function => "wipe";

        public override Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            return Task.CompletedTask;
        }

        public WipeCommands()
        {
            RegisterCommand("-wipe", Wipe);
        }

        public async Task Wipe(string[] arguments, SocketMessage message)
        {
            if (!IsPlayerDiscordModerator(message))
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + " Je moet een van de volgende rollen hebben: " + GetAllAllowedModRoles());
                return;
            }

            var discordBotMessage = await message.Channel.SendMessageAsync(message.Author.Mention + " ik ben bezig met alle perms uit het systeem te halen");

            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                    Name = "Verwijderen van PERM ban's"
                },
                Color = Color.DarkRed,
                Footer = new EmbedFooterBuilder
                {
                    Text = "DELETE PERM BANS"
                },
                Fields = new List<EmbedFieldBuilder>(),
                Timestamp = DateTimeOffset.Now,
            };

            var easyAdmins = await LoadEasyAdminBans();
            var antiCheats = await LoadAntiCheatBans();

            var permBanPlayers = new Dictionary<string, string>();
            var clearedTables = new Dictionary<string, int>();

            using (var usersCommand = new MySqlCommand("SELECT * FROM `users`", mysqlConnection))
            {
                using (var userReader = await usersCommand.ExecuteReaderAsync())
                {
                    if (!userReader.HasRows)
                    {
                        embed.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = "Resultaat",
                            Value = "Geen gebruiker gevonden in `users`"
                        });

                        await discordBotMessage.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });

                        await mysqlConnection.CloseAsync();

                        return;
                    }

                    while (await userReader.ReadAsync())
                    {
                        var identifier = await userReader.IsDBNullAsync(0) ? string.Empty : userReader.GetString(0);
                        var license = await userReader.IsDBNullAsync(1) ? string.Empty : userReader.GetString(1);
                        var username = await userReader.IsDBNullAsync(3) ? string.Empty : userReader.GetString(3);
                        var hasPern = HasPlayerPermBan(identifier, license, easyAdmins, antiCheats);

                        if (hasPern && !permBanPlayers.ContainsKey(identifier))
                        {
                            permBanPlayers.Add(identifier, username);
                        }
                    }
                }
            }

            if (!permBanPlayers.Any())
            {
                embed.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "Resultaat",
                    Value = "Alle spelers zijn al uit SQL verwijderd"
                });

                await discordBotMessage.ModifyAsync(properties =>
                {
                    properties.Embed = embed.Build();
                });

                await mysqlConnection.CloseAsync();

                return;
            }

            foreach (var permBanPlayer in permBanPlayers)
            {
                using (var deleteUserCommand = new MySqlCommand(
                    "DELETE FROM `users` WHERE `identifier` = @identifier",
                    mysqlConnection))
                {
                    deleteUserCommand.Parameters.AddWithValue("@identifier", permBanPlayer.Key);

                    var rows = await deleteUserCommand.ExecuteNonQueryAsync();

                    if (clearedTables.ContainsKey("users"))
                    {
                        clearedTables["users"] += rows;
                    }
                    else
                    {
                        clearedTables.Add("users", rows);
                    }
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `aad` FROM `addon_account_data` AS `aad` LEFT JOIN `users` AS `u` ON `aad`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL AND `aad`.`owner` IS NOT NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("addon_account_data"))
                {
                    clearedTables["addon_account_data"] += rows;
                }
                else
                {
                    clearedTables.Add("addon_account_data", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `aii` FROM `addon_inventory_items` AS `aii` LEFT JOIN `users` AS `u` ON `aii`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL AND `aii`.`owner` IS NOT NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("addon_inventory_items"))
                {
                    clearedTables["addon_inventory_items"] += rows;
                }
                else
                {
                    clearedTables.Add("addon_inventory_items", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `b` FROM `billing` AS `b` LEFT JOIN `users` AS `u` ON `b`.`identifier` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("billing"))
                {
                    clearedTables["billing"] += rows;
                }
                else
                {
                    clearedTables.Add("billing", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `c` FROM `characters` AS `c` LEFT JOIN `users` AS `u` ON `c`.`identifier` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("characters"))
                {
                    clearedTables["characters"] += rows;
                }
                else
                {
                    clearedTables.Add("characters", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `dsd` FROM `datastore_data` AS `dsd` LEFT JOIN `users` AS `u` ON `dsd`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL AND `dsd`.`owner` IS NOT NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("datastore_data"))
                {
                    clearedTables["datastore_data"] += rows;
                }
                else
                {
                    clearedTables.Add("datastore_data", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `j` FROM `jail` AS `j` LEFT JOIN `users` AS `u` ON `j`.`identifier` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("jail"))
                {
                    clearedTables["jail"] += rows;
                }
                else
                {
                    clearedTables.Add("jail", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `jot` FROM `job_onduty_time` AS `jot` LEFT JOIN `users` AS `u` ON `jot`.`identifier` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("job_onduty_time"))
                {
                    clearedTables["job_onduty_time"] += rows;
                }
                else
                {
                    clearedTables.Add("job_onduty_time", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `op` FROM `owned_properties` AS `op` LEFT JOIN `users` AS `u` ON `op`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL AND `op`.`owner` IS NOT NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("owned_properties"))
                {
                    clearedTables["owned_properties"] += rows;
                }
                else
                {
                    clearedTables.Add("owned_properties", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `ov` FROM `owned_vehicles` AS `ov` LEFT JOIN `users` AS `u` ON `ov`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL AND `ov`.`owner` IS NOT NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("owned_vehicles"))
                {
                    clearedTables["owned_vehicles"] += rows;
                }
                else
                {
                    clearedTables.Add("owned_vehicles", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `ti` FROM `ml_vehicle_trunk` AS `ti` LEFT JOIN `users` AS `u` ON `u`.`identifier` = `ti`.`owner` WHERE `u`.`identifier` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("trunk_inventory"))
                {
                    clearedTables["trunk_inventory"] += rows;
                }
                else
                {
                    clearedTables.Add("trunk_inventory", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `uc` FROM `user_contacts` AS `uc` LEFT JOIN `users` AS `u` ON `uc`.`identifier` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("user_contacts"))
                {
                    clearedTables["user_contacts"] += rows;
                }
                else
                {
                    clearedTables.Add("user_contacts", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `ul` FROM `user_licenses` AS `ul` LEFT JOIN `users` AS `u` ON `ul`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("user_licenses"))
                {
                    clearedTables["user_licenses"] += rows;
                }
                else
                {
                    clearedTables.Add("user_licenses", rows);
                }
            }

            using (var deleteDataCommand = new MySqlCommand(
                @"DELETE `ul` FROM `user_licenses` AS `ul` LEFT JOIN `users` AS `u` ON `ul`.`owner` = `u`.`identifier` WHERE `u`.`name` IS NULL",
                mysqlConnection))
            {
                var rows = await deleteDataCommand.ExecuteNonQueryAsync();

                if (clearedTables.ContainsKey("user_licenses"))
                {
                    clearedTables["user_licenses"] += rows;
                }
                else
                {
                    clearedTables.Add("user_licenses", rows);
                }
            }

            embed.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "Resultaat",
                Value = "Alle perm ban spelers zijn uit de database gehaald, dit zijn de resulaten:"
            });

            var fields = clearedTables.Select(ct 
                => new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = ct.Key,
                    Value = ct.Value
                });

            embed.Fields.AddRange(fields);

            await discordBotMessage.ModifyAsync(properties =>
            {
                properties.Embed = embed.Build();
            });

            await mysqlConnection.CloseAsync();

            var ds = Path.DirectorySeparatorChar;
            var _number = 0;
            var _filename = "verwijderde_spelers";
            var programPath = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists($"{programPath}{BotConfiguration.BOT["MessageStore"]}"))
            {
                Directory.CreateDirectory($"{programPath}{BotConfiguration.BOT["MessageStore"]}");
            }

            while (File.Exists($"{programPath}{BotConfiguration.BOT["MessageStore"]}{ds}{_filename}.csv"))
            {
                _number++;
                _filename = $"{_filename} ({_number})";
            }

            using (var writter = new StreamWriter($"{programPath}{BotConfiguration.BOT["MessageStore"]}{ds}{_filename}.csv"))
            using (var csvWriter = new CsvWriter(writter, CultureInfo.InvariantCulture))
            {
                await csvWriter.WriteRecordsAsync(permBanPlayers);
            }

            await message.Channel.SendFileAsync($"{programPath}{BotConfiguration.BOT["MessageStore"]}{ds}{_filename}.csv", $"{message.Author.Mention} Verwijderde spelers {DateTime.Now:dd-MM-yyyy HH:mm} document (csv)");
        }

        public Task<List<EasyAdmin>> LoadEasyAdminBans()
        {
            var baseDirectory = BotConfiguration.FiveM["Location"];
            var easyAdmin = baseDirectory + BotConfiguration.BanLists["EasyAdmin"];

            if (File.Exists(easyAdmin))
            {
                var easyAdminJsonData = File.ReadAllText(easyAdmin, Encoding.UTF8);
                var easyAdminResults = JsonConvert.DeserializeObject<List<EasyAdmin>>(easyAdminJsonData);

                return Task.FromResult(easyAdminResults);
            }

            return Task.FromResult(new List<EasyAdmin>());
        }

        public Task<List<AntiCheat>> LoadAntiCheatBans()
        {
            var baseDirectory = BotConfiguration.FiveM["Location"];
            var antiCheat = baseDirectory + BotConfiguration.BanLists["AntiCheat"];

            if (File.Exists(antiCheat))
            {
                var antiCheatJsonData = File.ReadAllText(antiCheat, Encoding.UTF8);
                var antiCheatResults = JsonConvert.DeserializeObject<List<AntiCheat>>(antiCheatJsonData);

                return Task.FromResult(antiCheatResults);
            }

            return Task.FromResult(new List<AntiCheat>());
        }

        public bool HasPlayerPermBan(string identifier, string license, List<EasyAdmin> easyAdmins, List<AntiCheat> antiCheats)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            foreach (var easyAdminItem in easyAdmins)
            {
                if (!string.IsNullOrWhiteSpace(easyAdminItem.Identifier) &&
                    string.Equals(easyAdminItem.Identifier.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return easyAdminItem.Expire == 10444633200;
                }

                var identifiers = easyAdminItem.Identifiers;

                if (!identifiers.IsNullOrDefault() && identifiers
                        .Any(id => string.Equals(id.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    return easyAdminItem.Expire == 10444633200;
                }

                if (!identifiers.IsNullOrDefault() && identifiers
                        .Any(id => string.Equals(id.Trim(), license.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    return easyAdminItem.Expire == 10444633200;
                }
            }

            foreach (var antiCheatItem in antiCheats)
            {
                if (!antiCheatItem.Identifiers.IsNullOrDefault() && antiCheatItem.Identifiers
                        .Any(id => string.Equals(id.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    return true;
                }

                if (!antiCheatItem.Identifiers.IsNullOrDefault() && antiCheatItem.Identifiers
                        .Any(id => string.Equals(id.Trim(), license.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
