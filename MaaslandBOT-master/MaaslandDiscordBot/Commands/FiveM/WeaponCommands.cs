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
    using MaaslandDiscordBot.Models.Enums;
    using MaaslandDiscordBot.Models.FiveM;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;

    using Color = global::Discord.Color;

    public class WeaponsCommands : BaseCommands
    {
        public override string Function => "weapons";

        public WeaponsCommands()
        {
            RegisterCommand("-weapons", PlayerLookupCommand);
        }

        public override async Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index = messageStore.GetReaction() - 1;

            using (var mySqlCommand = new MySqlCommand("SELECT * FROM `users` WHERE `identifier` = @identifier LIMIT 1", mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@identifier", players.ElementAt(index).Key);

                using (var mysqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mysqlReader.HasRows)
                    {
                        await message.ModifyAsync(properties =>
                        {
                            properties.Content = $"{message.Author.Mention} Speler niet gevonden";
                            properties.Embed = null;
                        });

                        return;
                    }

                    while (await mysqlReader.ReadAsync())
                    {
                        var identifier = await mysqlReader.GetValue<string>("identifier");

                        var weapons = new List<WeaponStore>();
                        var avatar = await identifier.GetAvatarByIdentifier();
                        var layoutRaw = await mysqlReader.GetValue<string>("loadout");

                        mysqlReader.Close();

                        if (string.IsNullOrWhiteSpace(layoutRaw) || layoutRaw.Equals("[]", StringComparison.InvariantCultureIgnoreCase))
                        {
                            layoutRaw = "{}";
                        }

                        var layout = JsonConvert.DeserializeObject<Dictionary<string, WeaponInvInfo>>(layoutRaw);

                        if (layout.IsNullOrDefault())
                        {
                            layout = new Dictionary<string, WeaponInvInfo>();
                        }

                        var userBlacklistedWeapons = new Dictionary<WeaponHash, int>();
                        var blacklistedWeapons = BotConfiguration.BlacklistWeapons;

                        foreach (var weaponLayout in layout)
                        {
                            var hash = WeaponHash.Unarmed;

                            if (!string.IsNullOrWhiteSpace(weaponLayout.Key))
                            {
                                hash = weaponLayout.Key.GetWeaponHashByID();
                            }

                            var weapon = weapons.FirstOrDefault(_weapon => _weapon.Hash == hash && _weapon.Type == StoreType.Inventory);

                            if (weapon.IsNullOrDefault())
                            {
                                weapon = new WeaponStore(hash, StoreType.Inventory);

                                weapons.Add(weapon);
                            }
                            else
                            {
                                weapon.Count++;
                            }

                            if (!blacklistedWeapons.Any(wp => string.Equals(wp, weaponLayout.Key, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                continue;
                            }

                            if (userBlacklistedWeapons.ContainsKey(hash))
                            {
                                userBlacklistedWeapons[hash] = userBlacklistedWeapons[hash] + 1;
                            }
                            else
                            {
                                userBlacklistedWeapons.Add(hash, 1);
                            }
                        }

                        using (var aparmentCommand =
                            new MySqlCommand(
                                "SELECT * FROM `datastore_data` WHERE `owner` = @owner",
                                mysqlConnection))
                        {
                            aparmentCommand.Parameters.AddWithValue("@owner", identifier);

                            using (var aparmentReader = await aparmentCommand.ExecuteReaderAsync())
                            {
                                if (aparmentReader.HasRows)
                                {
                                    while (await aparmentReader.ReadAsync())
                                    {
                                        var apartmentData = await aparmentReader.GetValue<string>("data");
                                        var apartment = JsonConvert.DeserializeObject<WeaponData>(apartmentData);

                                        if (apartment.Weapons.IsNullOrDefault() || apartment.Weapons.Length <= 0)
                                        {
                                            apartment.Weapons = new Layout[] {};
                                        }

                                        foreach (var weaponLayout in apartment.Weapons)
                                        {
                                            var hash = WeaponHash.Unarmed;

                                            if (!string.IsNullOrWhiteSpace(weaponLayout.Name))
                                            {
                                                hash = weaponLayout.Name.GetWeaponHashByID();
                                            }

                                            var weapon = weapons.FirstOrDefault(_weapon => _weapon.Hash == hash && _weapon.Type == StoreType.Appartment);

                                            if (weapon.IsNullOrDefault())
                                            {
                                                weapon = new WeaponStore(hash, StoreType.Appartment);

                                                weapons.Add(weapon);
                                            }
                                            else
                                            {
                                                weapon.Count++;
                                            }

                                            if (!blacklistedWeapons.Any(wp => string.Equals(wp, weaponLayout.Name, StringComparison.InvariantCultureIgnoreCase)))
                                            {
                                                continue;
                                            }

                                            if (userBlacklistedWeapons.ContainsKey(hash))
                                            {
                                                userBlacklistedWeapons[hash] = userBlacklistedWeapons[hash] + 1;
                                            }
                                            else
                                            {
                                                userBlacklistedWeapons.Add(hash, 1);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        using (var trunkCommand =
                            new MySqlCommand(
                                "SELECT * FROM `ml_vehicle_trunk` WHERE `owner` = @owner AND `isweapon` = 1",
                                mysqlConnection))
                        {
                            trunkCommand.Parameters.AddWithValue("@owner", identifier);

                            using (var trunkReader = await trunkCommand.ExecuteReaderAsync())
                            {
                                if (trunkReader.HasRows)
                                {
                                    while (await trunkReader.ReadAsync())
                                    {
                                        var plate = await trunkReader.GetValue<string>("plate");
                                        var item = await trunkReader.GetValue<string>("item");

                                        var hash = WeaponHash.Unarmed;

                                        if (!string.IsNullOrWhiteSpace(item))
                                        {
                                            hash = item.GetWeaponHashByID();
                                        }

                                        var weapon = weapons.FirstOrDefault(_weapon => _weapon.Hash == hash && _weapon.Type == StoreType.Trunk && _weapon.TypeData.Equals(plate, StringComparison.InvariantCultureIgnoreCase));

                                        if (weapon.IsNullOrDefault())
                                        {
                                            weapon = new WeaponStore(hash, StoreType.Trunk, plate);

                                            weapons.Add(weapon);
                                        }
                                        else
                                        {
                                            weapon.Count++;
                                        }

                                        if (!blacklistedWeapons.Any(wp => string.Equals(wp, item, StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            continue;
                                        }

                                        if (userBlacklistedWeapons.ContainsKey(hash))
                                        {
                                            userBlacklistedWeapons[hash] = userBlacklistedWeapons[hash] + 1;
                                        }
                                        else
                                        {
                                            userBlacklistedWeapons.Add(hash, 1);
                                        }
                                    }
                                }
                            }
                        }

                        var embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = avatar,
                                Name = $"Wapens van {players.ElementAt(index).Value}"
                            },
                            Color = Color.DarkGrey,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "WEAPONS " + players.ElementAt(index).Value.ToUpper()
                            },
                            Fields = new List<EmbedFieldBuilder>(),
                            Timestamp = DateTimeOffset.Now,
                        };

                        embed.Fields.AddRange(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Aantal wapens",
                                Value = weapons.Count
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Aantal blacklisted",
                                Value = userBlacklistedWeapons.Count
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Identifier",
                                Value = identifier
                            }
                        });

                        var list1 = new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Wapen",
                            Value = "‏‏‎ ‎"
                        };

                        var list2 = new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "‏‏‎Aantal‎",
                            Value = "‏‏‎ ‎"
                        };

                        var list3 = new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = "Opbergplaats‏‏‎",
                            Value = "‏‏‎ ‎"
                        };

                        foreach (var inventoryWeapon in weapons.Where(weapon => weapon.Type == StoreType.Inventory))
                        {
                            if (list1.Value.IsNullOrDefault() || list1.Value as string == "‏‏‎ ‎")
                            {
                                list1.Value = inventoryWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }
                            else
                            {
                                list1.Value += "\n" + inventoryWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }

                            if (list2.Value.IsNullOrDefault() || list2.Value as string == "‏‏‎ ‎")
                            {
                                list2.Value = inventoryWeapon.Count.ToString();
                            }
                            else
                            {
                                list2.Value += "\n" + inventoryWeapon.Count;
                            }

                            if (list3.Value.IsNullOrDefault() || list3.Value as string == "‏‏‎ ‎")
                            {
                                list3.Value = "Op zak";
                            }
                            else
                            {
                                list3.Value += "\nOp zak";
                            }
                        }

                        foreach (var appartmentWeapon in weapons.Where(weapon => weapon.Type == StoreType.Appartment))
                        {
                            if (list1.Value.IsNullOrDefault() || list1.Value as string == "‏‏‎ ‎")
                            {
                                list1.Value = appartmentWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }
                            else
                            {
                                list1.Value += "\n" + appartmentWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }

                            if (list2.Value.IsNullOrDefault() || list2.Value as string == "‏‏‎ ‎")
                            {
                                list2.Value = appartmentWeapon.Count.ToString();
                            }
                            else
                            {
                                list2.Value += "\n" + appartmentWeapon.Count;
                            }

                            if (list3.Value.IsNullOrDefault() || list3.Value as string == "‏‏‎ ‎")
                            {
                                list3.Value = "Appartement";
                            }
                            else
                            {
                                list3.Value += "\nAppartement";
                            }
                        }

                        foreach (var trunkWeapon in weapons.Where(weapon => weapon.Type == StoreType.Trunk))
                        {
                            if (list1.Value.IsNullOrDefault() || list1.Value as string == "‏‏‎ ‎")
                            {
                                list1.Value = trunkWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }
                            else
                            {
                                list1.Value += "\n" + trunkWeapon.Hash.GetWeaponIdByHash()
                                    .Replace("WEAPON_", string.Empty);
                            }

                            if (list2.Value.IsNullOrDefault() || list2.Value as string == "‏‏‎ ‎")
                            {
                                list2.Value = trunkWeapon.Count.ToString();
                            }
                            else
                            {
                                list2.Value += "\n" + trunkWeapon.Count;
                            }

                            if (list3.Value.IsNullOrDefault() || list3.Value as string == "‏‏‎ ‎")
                            {
                                list3.Value = $"Kofferbak ({trunkWeapon.TypeData})";
                            }
                            else
                            {
                                list3.Value += $"\nKofferbak ({trunkWeapon.TypeData})";
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

                        return;
                    }
                }
            }

            await mysqlConnection.CloseAsync();
        }
    }
}
