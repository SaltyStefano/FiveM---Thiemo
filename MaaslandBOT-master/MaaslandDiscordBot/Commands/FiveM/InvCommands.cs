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

    using Newtonsoft.Json;

    public class InvCommands : BaseCommands
    {
        public override string Function => "inv";

        public InvCommands()
        {
            RegisterCommand("-inv", PlayerLookupCommand);
        }

        public override async Task ActionHandler(Dictionary<string, string> players, IUserMessage message, MessageStore messageStore)
        {
            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            var index = messageStore.GetReaction() - 1;
            var name = players.ElementAt(index).Value;
            var identifier = players.ElementAt(index).Key;
            var items = new Dictionary<string, Item>();

            using (var mySqlCommand = new MySqlCommand("SELECT * FROM `items`", mysqlConnection))
            {
                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (mySqlReader.HasRows)
                    {
                        while (await mySqlReader.ReadAsync())
                        {
                            var itemName = await mySqlReader.GetValue<string>("name");
                            var itemLabel = await mySqlReader.GetValue<string>("label");
                            var itemLimit = await mySqlReader.GetValue<int>("limit");
                            var itemRare = await mySqlReader.GetValue<int>("rare") == 1;
                            var itemCanRemove = await mySqlReader.GetValue<int>("can_remove") == 1;
                            var itemWeight = await mySqlReader.GetValue<int>("weight");

                            var item = new Item
                            {
                                Name = itemName,
                                Label = itemLabel,
                                Count = default,
                                Limit = itemLimit,
                                Rare = itemRare,
                                CanRemove = itemCanRemove,
                                Weight = itemWeight
                            };

                            if (items.ContainsKey(itemName))
                            {
                                items[itemName] = item;
                            }
                            else
                            {
                                items.Add(itemName, item);
                            }
                        }
                    }
                }
            }

            using (var mySqlCommand = new MySqlCommand(@"SELECT `inventory` FROM `users` WHERE `identifier` = @identifier", mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var mysqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (mysqlReader.HasRows)
                    {
                        while (await mysqlReader.ReadAsync())
                        {
                            var inventory = await mysqlReader.GetValue<string>("inventory") ?? "{}";

                            if (inventory == "[]")
                            {
                                inventory = "{}";
                            }

                            var inventoryItems = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventory);

                            foreach (var inventoryItem in inventoryItems)
                            {
                                if (items.ContainsKey(inventoryItem.Key))
                                {
                                    items[inventoryItem.Key].Count = inventoryItem.Value;
                                }
                            }
                        }
                    }

                    var embed = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                            Name = $"Inventory van {name}"
                        },
                        Color = Color.DarkRed,
                        Footer = new EmbedFooterBuilder
                        {
                            Text = $"PLAYER INVENTORY {name.ToUpper()}"
                        },
                        Fields = new List<EmbedFieldBuilder>(),
                        Timestamp = DateTimeOffset.Now,
                    };

                    embed.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = false,
                        Name = "Aantal items",
                        Value = items.Count(item => item.Value.Count > default(int))
                    });

                    if (!items.Any(item => item.Value.Count > default(int)))
                    {
                        embed.Fields.AddRange(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Item",
                                Value = "‏‏‎ ‎"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Aantal in inv.",
                                Value = "‏‏‎ ‎"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Inventory max.",
                                Value = "‏‏‎ ‎"
                            }
                        });

                        await message.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });

                        await mysqlConnection.CloseAsync();

                        return;
                    }

                    var list1 = new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Item",
                        Value = "‏‏‎ ‎"
                    };

                    var list2 = new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "‏‏‎Aantal in inv.",
                        Value = "‏‏‎ ‎"
                    };

                    var list3 = new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "‏‏‎Inventory max.",
                        Value = "‏‏‎ ‎"
                    };

                    var sortedList = items
                        .Where(item => item.Value.Count > default(int))
                        .OrderBy(item => item.Value.Label)
                        .ToList();

                    foreach (var item in sortedList)
                    {
                        if (list1.Value.IsNullOrDefault() || list1.Value as string == "‏‏‎ ‎")
                        {
                            list1.Value = item.Value.Label;
                        }
                        else
                        {
                            list1.Value += "\n" + item.Value.Label;
                        }

                        if (list2.Value.IsNullOrDefault() || list2.Value as string == "‏‏‎ ‎")
                        {
                            list2.Value = item.Value.Count.ToString();
                        }
                        else
                        {
                            list2.Value += "\n" + item.Value.Count;
                        }

                        if (list3.Value.IsNullOrDefault() || list3.Value as string == "‏‏‎ ‎")
                        {
                            list3.Value = item.Value.Limit == 0 || item.Value.Limit == -1 ? "-" : item.Value.Limit.ToString();
                        }
                        else
                        {
                            list3.Value += "\n" + (item.Value.Limit == 0 || item.Value.Limit == -1 ? "-" : item.Value.Limit.ToString());
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
        }
    }
}
