namespace MaaslandDiscordBot.Commands.FiveM
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
    using MaaslandDiscordBot.Models.FiveM;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;

    public class SocietyCommands : BaseCommands
    {
        public override string Function => "job";

        public SocietyCommands()
        {
            RegisterCommand("-job", SocietyLookupCommand);
        }

        public override async Task ActionHandler(Dictionary<string, string> societies, IUserMessage message, MessageStore messageStore)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index   = messageStore.GetReaction() - 1;
            var result  = new Society();
            var society = societies.ElementAt(index).Value;
            var name = society.Trim();
            var key = societies.ElementAt(index).Key.ToLower().Trim();

            result.Name = societies.ElementAt(index).Key;

            using (var mySqlCommand = new MySqlCommand("SELECT `account_name`, `money` FROM `addon_account_data` WHERE (LOWER(`account_name`) LIKE @societyLike AND `owner` IS NULL) OR (LOWER(`account_name`) LIKE @society AND `owner` IS NULL)",
                mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@societyLike", $"society_{key}%".ToLower());
                mySqlCommand.Parameters.AddWithValue("@society", $"society_{key}".ToLower());

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mySqlReader.HasRows)
                    {
                        await message.ModifyAsync(properties =>
                        {
                            properties.Content = $"{message.Author.Mention} Society niet gevonden";
                            properties.Embed = null;
                        });

                        return;
                    }

                    while (await mySqlReader.ReadAsync())
                    {
                        var account_name = await mySqlReader.IsDBNullAsync(0) ? string.Empty : mySqlReader.GetString(0);

                        if (string.IsNullOrWhiteSpace(account_name))
                        {
                            continue;
                        }

                        if (account_name.ToLower().Trim() == $"society_{key}".ToLower())
                        {
                            result.Bank = (await mySqlReader.GetValue<double>("money")).ToInt();
                        }
                        else if(account_name.ToLower().Trim() == $"society_{key}_black_money".ToLower())
                        {
                            result.Dirty = (await mySqlReader.GetValue<double>("money")).ToInt();
                        }
                        else if (account_name.ToLower().Trim() == $"society_{key}_money_wash".ToLower())
                        {
                            result.Wash = (await mySqlReader.GetValue<double>("money")).ToInt();
                        }
                    }
                }
            }

            var grades = new Dictionary<int, string>();

            using (var mySqlCommand = new MySqlCommand("SELECT `grade`,`label` FROM `job_grades` WHERE LOWER(`job_name`) = @job ORDER BY `grade` ASC",
                mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@job", key);

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (mySqlReader.HasRows)
                    {
                        while (await mySqlReader.ReadAsync())
                        {
                            var grade = await mySqlReader.GetValue<int>("grade");
                            var label = await mySqlReader.GetValue<string>("label");

                            if (!grades.ContainsKey(grade))
                            {
                                grades.Add(grade, label);
                            }
                        }
                    }
                }
            }

            using (var mySqlCommand = new MySqlCommand("SELECT `u`.`identifier`, `ul`.`name`,`u`.`job`,`u`.`job2`,`u`.`job_grade`,`u`.`job2_grade` FROM `users` AS `u` LEFT JOIN `user_logs` AS `ul` ON `ul`.`id` = (SELECT `ul2`.`id` FROM `user_logs` AS `ul2` WHERE (`ul2`.`identifier` = `u`.`identifier`) ORDER BY `id` DESC LIMIT 1) WHERE (LOWER(`u`.`job`) = @job) OR (LOWER(`u`.`job`) = @offJob) OR (LOWER(`u`.`job2`) = @job) OR (LOWER(`u`.`job2`) = @offJob) ORDER BY `u`.`job_grade`,`u`.`job2_grade` DESC",
                mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@job", key);
                mySqlCommand.Parameters.AddWithValue("@offJob", $"off{key}");

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (mySqlReader.HasRows)
                    {
                        while (await mySqlReader.ReadAsync())
                        {
                            var label = string.Empty;
                            var identifier = await mySqlReader.GetValue<string>("identifier");
                            var player = await mySqlReader.GetValue<string>("name");
                            var job1 = await mySqlReader.GetValue<string>("job");
                            var job2 = await mySqlReader.GetValue<string>("job2");
                            var job1_grade = await mySqlReader.GetValue<int>("job_grade");
                            var job2_grade = await mySqlReader.GetValue<int>("job2_grade");

                            if (job1.ToLower().Trim() == key.ToLower().Trim() || job1.ToLower().Trim() == $"off{key}".ToLower().Trim())
                            {
                                label = grades.ContainsKey(job1_grade) ? grades[job1_grade] : string.Empty;
                            }

                            if (job2.ToLower().Trim() == key.ToLower().Trim() || job2.ToLower().Trim() == $"off{key}".ToLower().Trim())
                            {
                                label = grades.ContainsKey(job2_grade) ? grades[job2_grade] : string.Empty;
                            }

                            result.Members.Add(new Tuple<string, string, string>(identifier, player, label));
                        }
                    }
                }
            }

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = message.Author.GetAvatarUrl(),
                    Name = $"Overzicht van {name}"
                },
                Color = Color.DarkGrey,
                Footer = new EmbedFooterBuilder
                {
                    Text = "SOCIETY " + name.ToUpper()
                },
                Fields = new List<EmbedFieldBuilder>(),
                Timestamp = DateTimeOffset.Now,
            };

            var list1 = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Wapens",
                Value = "‏‏‎ ‎"
            };

            var list2 = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "‏‏‎ ‎",
                Value = "‏‏‎ ‎"
            };

            var list3 = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "‏‏‎ ‎",
                Value = "‏‏‎ ‎"
            };

            var weapons = new Dictionary<WeaponHash, int>();

            embed.Fields.AddRange(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = "Name",
                    Value = name
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Bank",
                    Value = result.Bank == default ? "-" : $"{result.Bank:C0}"
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Zwartgeld",
                    Value = result.Dirty == default ? "-" : $"{result.Dirty:C0}"
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Witwas",
                    Value = result.Wash == default ? "-" : $"{result.Wash:C0}"
                },
            });

            var memberList1 = string.Empty;
            var memberList2 = string.Empty;
            var memberList3 = string.Empty;

            foreach (var (identifier, playerName, jobLabel) in result.Members)
            {
                if (string.IsNullOrWhiteSpace(memberList1)) { memberList1 = playerName; }
                else { memberList1 += "\n" + playerName; }

                if (string.IsNullOrWhiteSpace(memberList2)) { memberList2 = jobLabel; }
                else { memberList2 += "\n" + jobLabel; }

                if (string.IsNullOrWhiteSpace(memberList3)) { memberList3 = await GetDiscordNameByIdentifier(identifier); }
                else { memberList3 += "\n" + await GetDiscordNameByIdentifier(identifier); }
            }

            if (string.IsNullOrWhiteSpace(memberList1))
            {
                memberList1 = "‏‏‎ ‎";
            }

            if (string.IsNullOrWhiteSpace(memberList2))
            {
                memberList2 = "‏‏‎ ‎";
            }

            if (string.IsNullOrWhiteSpace(memberList3))
            {
                memberList3 = "‏‏‎ ‎";
            }

            embed.Fields.AddRange(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = $"Leden ({result.Members.Count})",
                    Value = memberList1
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "‏‏‎ ‎",
                    Value = memberList2
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "‏‏‎ ‎",
                    Value = memberList3
                },
            });

            using (var mySqlCommand = new MySqlCommand("SELECT `data` FROM `datastore_data` WHERE LOWER(`name`) = @name", mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@name", $"society_{key}");

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (mySqlReader.HasRows)
                    {
                        while (await mySqlReader.ReadAsync())
                        {
                            var data = await mySqlReader.GetValue<string>("data");

                            if (string.IsNullOrWhiteSpace(data))
                            {
                                data = "{\"weapons\":[]}";
                            }

                            var weaponData = JsonConvert.DeserializeObject<WeaponData>(data);

                            if (!weaponData.Weapons.IsNullOrDefault() && weaponData.Weapons.Length > default(int))
                            {
                                foreach (var weaponLayout in weaponData.Weapons)
                                {
                                    var hash = WeaponHash.Unarmed;

                                    if (!string.IsNullOrWhiteSpace(weaponLayout.Name))
                                    {
                                        hash = weaponLayout.Name.GetWeaponHashByID();
                                    }

                                    if (weapons.ContainsKey(hash))
                                    {
                                        var counter = weaponLayout.Count;

                                        if (counter > default(int))
                                        {
                                            weapons[hash] = weapons[hash] + counter;
                                        }
                                    }
                                    else
                                    {
                                        var counter = weaponLayout.Count;

                                        if (counter > default(int))
                                        {
                                            weapons.Add(hash, counter);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (weapons.Count <= 0)
            {
                list1.Value = "GEEN";

                embed.Fields.Add(list1);
                embed.Fields.Add(list2);
                embed.Fields.Add(list3);

                await message.ModifyAsync(properties =>
                {
                    properties.Embed = embed.Build();
                });

                await mysqlConnection.CloseAsync();

                return;
            }

            if (weapons.Count == 1)
            {
                list1.Value = weapons.First().Value + "x " + weapons.First().Key.GetWeaponIdByHash()
                    .Replace("WEAPON_", string.Empty);

                embed.Fields.Add(list1);

                await message.ModifyAsync(properties =>
                {
                    properties.Embed = embed.Build();
                });

                await mysqlConnection.CloseAsync();

                return;
            }

            if (weapons.Count == 2)
            {

                list1.Value = weapons.First().Value + "x " + weapons.First().Key.GetWeaponIdByHash()
                    .Replace("WEAPON_", string.Empty);

                list2.Value = weapons.Last().Value + "x " + weapons.Last().Key.GetWeaponIdByHash()
                    .Replace("WEAPON_", string.Empty);

                embed.Fields.Add(list1);
                embed.Fields.Add(list2);
                embed.Fields.Add(list3);

                await message.ModifyAsync(properties =>
                {
                    properties.Embed = embed.Build();
                });

                await mysqlConnection.CloseAsync();

                return;
            }

            if (weapons.Count >= 3)
            {
                var numberOfWeapons = weapons.Count;
                var firstRow = (int)Math.Ceiling(numberOfWeapons / 3.0);

                numberOfWeapons -= firstRow;

                var secondRow = (int)Math.Ceiling(numberOfWeapons / 2.0);

                numberOfWeapons -= secondRow;

                var lastRow = numberOfWeapons;

                var firstList = weapons
                    .Take(firstRow)
                    .ToList();

                var secondList = weapons
                    .Skip(firstRow)
                    .Take(secondRow)
                    .ToList();

                var lastList = weapons
                    .Skip(firstRow + secondRow)
                    .Take(lastRow)
                    .ToList();

                foreach (var weapon in firstList)
                {
                    if (list1.Value.IsNullOrDefault() || list1.Value as string == "‏‏‎ ‎")
                    {
                        list1.Value = weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                    else
                    {
                        list1.Value += "\n" + weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                }

                foreach (var weapon in secondList)
                {
                    if (list2.Value.IsNullOrDefault() || list2.Value as string == "‏‏‎ ‎")
                    {
                        list2.Value = weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                    else
                    {
                        list2.Value += "\n" + weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                }

                foreach (var weapon in lastList)
                {
                    if (list3.Value.IsNullOrDefault() || list3.Value as string == "‏‏‎ ‎")
                    {
                        list3.Value = weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                    else
                    {
                        list3.Value += "\n" + weapon.Value + "x " + weapon.Key.GetWeaponIdByHash()
                            .Replace("WEAPON_", string.Empty);
                    }
                }

                embed.Fields.Add(list1);
                embed.Fields.Add(list2);
                embed.Fields.Add(list3);

                await message.ModifyAsync(properties =>
                {
                    properties.Embed = embed.Build();
                });

                await mysqlConnection.CloseAsync();
            }
        }

        public async Task<string> GetDiscordNameByIdentifier(string identifier)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            using (var searchLogCommand = new MySqlCommand(
                "SELECT * FROM `user_logs` WHERE `identifier` = @identifier AND `discord` IS NOT NULL ORDER BY `date` DESC LIMIT 1",
                mysqlConnection))
            {
                searchLogCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var logReader = await searchLogCommand.ExecuteReaderAsync())
                {
                    if (!logReader.HasRows)
                    {
                        return "-";
                    }

                    while (await logReader.ReadAsync())
                    {
                        var discordId = await logReader.GetValue<string>("discord");
                        var userId = Convert.ToUInt64(discordId);

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

                        return discordUser.IsNullOrDefault()
                            ? "-"
                            : discordUser.Mention;
                    }
                }
            }

            await mysqlConnection.CloseAsync();

            return "-";
        }
    }
}
