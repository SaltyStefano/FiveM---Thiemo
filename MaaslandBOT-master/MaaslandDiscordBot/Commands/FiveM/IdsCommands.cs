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

    using MySql.Data.MySqlClient;

    public class IdsCommands : BaseCommands
    {
        public override string Function => "ids";

        public IdsCommands()
        {
            RegisterCommand("-ids", PlayerLookupCommand);
        }

        public override async Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            EmbedBuilder embed;

            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index = messageStore.GetReaction() - 1;
            var identifier = players.ElementAt(index).Key;
            var identifiers = new Dictionary<IdType, Dictionary<string, bool>>
            {
                { IdType.Identifier, new Dictionary<string, bool>() },
                { IdType.License, new Dictionary<string, bool>() },
                { IdType.XBOX, new Dictionary<string, bool>() },
                { IdType.Live, new Dictionary<string, bool>() },
                { IdType.Discord, new Dictionary<string, bool>() },
                { IdType.IP, new Dictionary<string, bool>() },
            };

            using (var searchLogCommand = new MySqlCommand(
                "SELECT * FROM `user_logs` WHERE `identifier` = @identifier GROUP BY `identifier`,`license`,`xbl`,`live`,`discord`,`ip`",
                mysqlConnection))
            {
                searchLogCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var mySqlReader = await searchLogCommand.ExecuteReaderAsync())
                {
                    if (!mySqlReader.HasRows)
                    {
                        embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                                Name = "Id's van Onbekend"
                            },
                            Color = Color.DarkGrey,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "USER LOGS ONBEKEND"
                            },
                            Fields = new List<EmbedFieldBuilder>(),
                            Timestamp = DateTimeOffset.Now,
                        };

                        embed.Fields.AddRange(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Identifier",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Naam",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "IP",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "FiveM",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "XBOX Live",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Microsoft",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Discord",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "Voor het laatst gejoined op",
                                Value = "-"
                            }
                        });

                        embed.Fields.Add(
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "RAW (FNS)",
                                Value = "[]"
                            });

                        await message.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });

                        await mysqlConnection.CloseAsync();

                        return;
                    }

                    while (await mySqlReader.ReadAsync())
                    {
                        var rowIdentifier = await mySqlReader.GetValue<string>("identifier");
                        var rowLicense = await mySqlReader.GetValue<string>("license");
                        var rowXBOX = await mySqlReader.GetValue<string>("xbl");
                        var rowLive = await mySqlReader.GetValue<string>("live");
                        var rowDiscord = await mySqlReader.GetValue<string>("discord");
                        var rowIP = await mySqlReader.GetValue<string>("ip");

                        if (!string.IsNullOrWhiteSpace(rowIdentifier)
                            && !identifiers[IdType.Identifier].ContainsKey(rowIdentifier))
                        {
                            identifiers[IdType.Identifier].Add(rowIdentifier, false);
                        }

                        if (!string.IsNullOrWhiteSpace(rowLicense)
                            && !identifiers[IdType.License].ContainsKey(rowLicense))
                        {
                            identifiers[IdType.License].Add(rowLicense, false);
                        }

                        if (!string.IsNullOrWhiteSpace(rowXBOX)
                            && !identifiers[IdType.XBOX].ContainsKey(rowXBOX))
                        {
                            identifiers[IdType.XBOX].Add(rowXBOX, false);
                        }

                        if (!string.IsNullOrWhiteSpace(rowLive)
                            && !identifiers[IdType.Live].ContainsKey(rowLive))
                        {
                            identifiers[IdType.Live].Add(rowLive, false);
                        }

                        if (!string.IsNullOrWhiteSpace(rowDiscord)
                            && !identifiers[IdType.Discord].ContainsKey(rowDiscord))
                        {
                            identifiers[IdType.Discord].Add(rowDiscord, false);
                        }

                        if (!string.IsNullOrWhiteSpace(rowIP)
                            && !identifiers[IdType.IP].ContainsKey(rowIP)
                            && !BotConfiguration.IgnoreIPs.Contains(rowIP))
                        {
                            identifiers[IdType.IP].Add(rowIP, false);
                        }
                    }
                }
            }

            await mysqlConnection.CloseAsync();

            var avatar = await identifier.GetAvatarByIdentifier();

            embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = avatar,
                    Name = $"Id's van {players.ElementAt(index).Value}"
                },
                Color = Color.DarkGrey,
                Footer = new EmbedFooterBuilder
                {
                    Text = "USER LOGS " + players.ElementAt(index).Value.ToUpper()
                },
                Fields = new List<EmbedFieldBuilder>(),
                Timestamp = DateTimeOffset.Now,
            };

            embed.Fields.AddRange(new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Identifier",
                    Value = identifiers[IdType.Identifier].GenerateString(string.Empty)
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "License",
                    Value = identifiers[IdType.License].GenerateString(IdType.License.GetName())
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "XBOX Live",
                    Value = identifiers[IdType.XBOX].GenerateString(IdType.XBOX.GetName())
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Microsoft",
                    Value = identifiers[IdType.Live].GenerateString(IdType.Live.GetName())
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Discord",
                    Value = identifiers[IdType.Discord].GenerateString(IdType.Discord.GetName())
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "IP",
                    Value = identifiers[IdType.IP].GenerateString(IdType.IP.GetName())
                },
            });

            await UpdateAllIdentifiers(identifiers);

            var rawIdentifiers = identifiers.GenerateToRawString();

            embed.Fields.Add(new EmbedFieldBuilder
            {
                IsInline = false,
                Name = "[RAW] Identifiers",
                Value = rawIdentifiers
            });

            await message.ModifyAsync(properties =>
            {
                properties.Embed = embed.Build();
            });
        }

        public async Task UpdateAllIdentifiers(Dictionary<IdType, Dictionary<string, bool>> identifiers)
        {
            var updatedIdentifiers = new Dictionary<IdType, Dictionary<string, bool>>
            {
                { IdType.Identifier, new Dictionary<string, bool>() },
                { IdType.License, new Dictionary<string, bool>() },
                { IdType.XBOX, new Dictionary<string, bool>() },
                { IdType.Live, new Dictionary<string, bool>() },
                { IdType.Discord, new Dictionary<string, bool>() },
                { IdType.IP, new Dictionary<string, bool>() },
            };
            var newIdentifiers = new Dictionary<IdType, Dictionary<string, bool>>
            {
                { IdType.Identifier, new Dictionary<string, bool>() },
                { IdType.License, new Dictionary<string, bool>() },
                { IdType.XBOX, new Dictionary<string, bool>() },
                { IdType.Live, new Dictionary<string, bool>() },
                { IdType.Discord, new Dictionary<string, bool>() },
                { IdType.IP, new Dictionary<string, bool>() },
            };

            foreach (var identifier in identifiers[IdType.IP])
            {
                if (!updatedIdentifiers[IdType.IP].ContainsKey(identifier.Key))
                {
                    updatedIdentifiers[IdType.IP].Add(identifier.Key, true);
                }
            }

            foreach (var identifier in updatedIdentifiers[IdType.IP])
            {
                if (identifiers[IdType.IP].ContainsKey(identifier.Key))
                {
                    identifiers[IdType.IP][identifier.Key] = true;
                }
            }

            while (identifiers.Any(type => type.Value.Any(identifier => !identifier.Value)))
            {
                foreach (var type in identifiers)
                {
                    var searchForValues = type.Value.Where(identifier => !identifier.Value);

                    foreach (var searchValue in searchForValues)
                    {
                        var results = await GetIdsByType(type.Key, searchValue.Key);

                        foreach (var resultType in results)
                        {
                            foreach (var resultValue in resultType.Value)
                            {
                                if (!string.IsNullOrWhiteSpace(resultValue.Key)
                                    && !identifiers[resultType.Key].ContainsKey(resultValue.Key)
                                    && !newIdentifiers[resultType.Key].ContainsKey(resultValue.Key))
                                {
                                    if (resultType.Key == IdType.IP && BotConfiguration.IgnoreIPs.Contains(resultValue.Key))
                                    {
                                        continue;
                                    }

                                    newIdentifiers[resultType.Key].Add(resultValue.Key, false);
                                }
                            }
                        }

                        if (updatedIdentifiers[type.Key].ContainsKey(searchValue.Key))
                        {
                            updatedIdentifiers[type.Key][searchValue.Key] = true;
                        }
                        else
                        {
                            if (type.Key == IdType.IP && BotConfiguration.IgnoreIPs.Contains(searchValue.Key))
                            {
                                continue;
                            }

                            updatedIdentifiers[type.Key].Add(searchValue.Key, true);
                        }
                    }
                }

                foreach (var updatedType in updatedIdentifiers)
                {
                    foreach (var updatedValue in updatedType.Value)
                    {
                        if (identifiers[updatedType.Key].ContainsKey(updatedValue.Key))
                        {
                            identifiers[updatedType.Key][updatedValue.Key] = true;
                        }
                    }
                }

                foreach (var addType in newIdentifiers)
                {
                    foreach (var addValue in addType.Value)
                    {
                        if (!identifiers[addType.Key].ContainsKey(addValue.Key))
                        {
                            identifiers[addType.Key].Add(addValue.Key, false);
                        }
                    }
                }
            }
        }

        public async Task<Dictionary<IdType, Dictionary<string, bool>>> GetIdsByType(IdType type, string value)
        {
            var returnList = new Dictionary<IdType, Dictionary<string, bool>>
            {
                { IdType.Identifier, new Dictionary<string, bool>() },
                { IdType.License, new Dictionary<string, bool>() },
                { IdType.XBOX, new Dictionary<string, bool>() },
                { IdType.Live, new Dictionary<string, bool>() },
                { IdType.Discord, new Dictionary<string, bool>() },
                { IdType.IP, new Dictionary<string, bool>() },
            };

            var key = type.GetName();
            var idList = new[] { IdType.Identifier, IdType.License, IdType.XBOX, IdType.Live, IdType.Discord };

            foreach (var id in idList)
            {
                var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

                await mysqlConnection.OpenAsync();

                var query = GetQueryStringByType(id, key);

                using (var idCommand = new MySqlCommand(query, mysqlConnection))
                {
                    idCommand.Parameters.AddWithValue("@identifier", value);

                    using (var mySqlReader = await idCommand.ExecuteReaderAsync())
                    {
                        if (!mySqlReader.HasRows)
                        {
                            continue;
                        }

                        while (await mySqlReader.ReadAsync())
                        {
                            var rowResult = await mySqlReader.GetValue<string>(id.GetName());

                            if (id == IdType.IP && BotConfiguration.IgnoreIPs.Contains(rowResult))
                            {
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(rowResult)
                                && !returnList[id].ContainsKey(rowResult))
                            {
                                returnList[id].Add(rowResult, false);
                            }
                        }
                    }
                }
            }

            return returnList;
        }

        public string GetQueryStringByType(IdType type, string key)
        {
            switch (type)
            {
                case IdType.Unknown:
                    return string.Empty;
                case IdType.Identifier:
                    return $"SELECT `identifier` FROM `user_logs` WHERE `{key}` = @identifier AND `identifier` IS NOT NULL GROUP BY `identifier`";
                case IdType.License:
                    return $"SELECT `license` FROM `user_logs` WHERE `{key}` = @identifier AND `license` IS NOT NULL GROUP BY `license`";
                case IdType.XBOX:
                    return $"SELECT `xbl` FROM `user_logs` WHERE `{key}` = @identifier AND `xbl` IS NOT NULL GROUP BY `xbl`";
                case IdType.Live:
                    return $"SELECT `live` FROM `user_logs` WHERE `{key}` = @identifier AND `live` IS NOT NULL GROUP BY `live`";
                case IdType.Discord:
                    return $"SELECT `discord` FROM `user_logs` WHERE `{key}` = @identifier AND `discord` IS NOT NULL GROUP BY `discord`";
                case IdType.IP:
                    return $"SELECT `ip` FROM `user_logs` WHERE `{key}` = @identifier AND `ip` IS NOT NULL GROUP BY `ip`";
                default:
                    return string.Empty;
            }
        }
    }
}
