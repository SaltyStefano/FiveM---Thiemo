namespace MaaslandDiscordBot.Commands.FiveM
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    using global::Discord;

    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BanLists;
    using MaaslandDiscordBot.Models.BOT;
    using MaaslandDiscordBot.Models.FiveM;

    using MySql.Data.MySqlClient;

    using Newtonsoft.Json;

    public class PlayerCommands : BaseCommands
    {
        public override string Function => "player";

        public PlayerCommands()
        {
            RegisterCommand("-player", PlayerLookupCommand);
        }

        public Tuple<bool, string, string, long> IsUserBanned(string identifier, string username)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new Tuple<bool, string, string, long>(false, "", "", 0);
            }

            var baseDirectory = BotConfiguration.FiveM["Location"];
            var easyAdmin = baseDirectory + BotConfiguration.BanLists["EasyAdmin"];
            var antiCheat = baseDirectory + BotConfiguration.BanLists["AntiCheat"];

            if (File.Exists(easyAdmin))
            {
                var easyAdminJsonData = File.ReadAllText(easyAdmin, Encoding.UTF8);
                var easyAdminResults = JsonConvert.DeserializeObject<List<EasyAdmin>>(easyAdminJsonData);

                foreach (var easyAdminItem in easyAdminResults)
                {
                    if (!string.IsNullOrWhiteSpace(easyAdminItem.Identifier) &&
                        string.Equals(easyAdminItem.Identifier.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(easyAdminItem.Reason))
                        {
                            easyAdminItem.Reason = "Onbekend";
                        }

                        if (!string.IsNullOrWhiteSpace(easyAdminItem.Banner))
                        {
                            easyAdminItem.Reason = easyAdminItem.Reason.Replace($"( Nickname: {username} ), Banned by: {easyAdminItem.Banner}", string.Empty).Trim();
                            easyAdminItem.Reason = easyAdminItem.Reason.Replace($"( Gebruikersnaam: {username} ), Verbannen door: {easyAdminItem.Banner}", string.Empty).Trim();
                        }

                        return new Tuple<bool, string, string, long>(true, 
                            string.IsNullOrWhiteSpace(easyAdminItem.Banner) ? "MaaslandRP" : easyAdminItem.Banner,
                            string.IsNullOrWhiteSpace(easyAdminItem.Reason) ? "Onbekend" : easyAdminItem.Reason,
                            easyAdminItem.Expire);
                    }

                    var identifiers = easyAdminItem.Identifiers;

                    if (!identifiers.IsNullOrDefault() && identifiers
                        .Any(id => string.Equals(id.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                    {
                        if (string.IsNullOrWhiteSpace(easyAdminItem.Reason))
                        {
                            easyAdminItem.Reason = "Onbekend";
                        }

                        if (!string.IsNullOrWhiteSpace(easyAdminItem.Banner))
                        {
                            easyAdminItem.Reason = easyAdminItem.Reason.Replace($"( Nickname: {username} ), Banned by: {easyAdminItem.Banner}", string.Empty).Trim();
                            easyAdminItem.Reason = easyAdminItem.Reason.Replace($"( Gebruikersnaam: {username} ), Verbannen door: {easyAdminItem.Banner}", string.Empty).Trim();
                        }

                        return new Tuple<bool, string, string, long>(true, 
                            string.IsNullOrWhiteSpace(easyAdminItem.Banner) ? "MaaslandRP" : easyAdminItem.Banner,
                            string.IsNullOrWhiteSpace(easyAdminItem.Reason) ? "Onbekend" : easyAdminItem.Reason,
                            easyAdminItem.Expire);
                    }
                }
            }

            if (File.Exists(antiCheat))
            {
                var antiCheatJsonData = File.ReadAllText(antiCheat, Encoding.UTF8);
                var antiCheatResults = JsonConvert.DeserializeObject<List<AntiCheat>>(antiCheatJsonData);

                foreach (var antiCheatItem in antiCheatResults)
                {
                    var identifiers = antiCheatItem.Identifiers;

                    if (!identifiers.IsNullOrDefault() && identifiers
                            .Any(id => string.Equals(id.Trim(), identifier.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                    {
                        if (string.IsNullOrWhiteSpace(antiCheatItem.Reason))
                        {
                            antiCheatItem.Reason = "Onbekend";
                        }

                        return new Tuple<bool, string, string, long>(true,
                            "TigoAntiCheat",
                            string.IsNullOrWhiteSpace(antiCheatItem.Reason) ? "Onbekend" : antiCheatItem.Reason,
                            10444633200);
                    }
                }
            }

            return new Tuple<bool, string, string, long>(false, "", "", 0);
        }

        public async Task<bool> IsUserOnline(string identifier)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri($"http://{BotConfiguration.FiveM["URL"]}");
                    client.DefaultRequestHeaders.Add("User-Agent", "MaaslandBOT");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync("players.json");

                    response.EnsureSuccessStatusCode();

                    if (!response.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    var playersRaw = await response.Content.ReadAsStringAsync();
                    var players = JsonConvert.DeserializeObject<List<Player>>(playersRaw);

                    if (players.IsNullOrDefault() || players.Count <= 0)
                    {
                        return false;
                    }

                    foreach (var player in players)
                    {
                        if (!player.Identifiers.IsNullOrDefault() && player.Identifiers.Any(
                                id => !string.IsNullOrWhiteSpace(id) && string.Equals(
                                          id.Trim(),
                                          identifier.Trim(),
                                          StringComparison.CurrentCultureIgnoreCase)))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int> GetBillingAmount(string identifier)
        {
            var mySqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mySqlConnection.OpenAsync();

            using (var mySqlCommand =
                new MySqlCommand("SELECT SUM(`amount`) AS `total` FROM `billing` WHERE `identifier` = @identifier",
                    mySqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@identifier", identifier);

                using (var mySqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mySqlReader.HasRows)
                    {
                        await mySqlConnection.CloseAsync();

                        return default;
                    }

                    while (mySqlReader.Read())
                    {
                        var total = await mySqlReader.IsDBNullAsync(0) ? default : mySqlReader.GetInt32(0);

                        await mySqlConnection.CloseAsync();

                        return total;
                    }
                }
            }

            await mySqlConnection.CloseAsync();

            return default;
        }

        public override async Task ActionHandler(
            Dictionary<string, string> players,
            IUserMessage message,
            MessageStore messageStore)
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
                        var embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png",
                                Name = $"Gegevens van Onbekend"
                            },
                            Color = Color.DarkRed,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "PLAYER REPORT ONBEKEND"
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
                                Name = "License",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Online",
                                Value = "Nee"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Contant",
                                Value = $"{default(int):C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Bank",
                                Value = $"{default(int):C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Zwartgeld",
                                Value = $"{default(int):C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Boetes",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Hoofdbaan",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Bijbaan",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Verbannen",
                                Value = "Nee"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Reden",
                                Value = "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Banner",
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

                    while (await mysqlReader.ReadAsync())
                    {
                        var identifier = await mysqlReader.GetValue<string>("identifier");
                        var avatar = await identifier.GetAvatarByIdentifier();
                        var (isBanned, bannedBy, banReason, banExpire) = IsUserBanned(identifier, await mysqlReader.GetValue<string>("name"));
                        var isPlayerOnline = await IsUserOnline(identifier);
                        var billing = await GetBillingAmount(identifier);
                        var blackMoney = (int)0;
                        var accounts = JsonConvert.DeserializeObject<Dictionary<string, int>>(
                            await mysqlReader.GetValue<string>("accounts") ?? "{}");

                        foreach (var account in accounts)
                        {
                            if (account.Key.Equals("black_money", StringComparison.InvariantCultureIgnoreCase))
                            {
                                blackMoney = account.Value;
                            }
                        }

                        var embed = new EmbedBuilder
                        {
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = avatar,
                                Name = $"Gegevens van {players.ElementAt(index).Value}"
                            },
                            Color = isPlayerOnline ? Color.Green : Color.DarkRed,
                            Footer = new EmbedFooterBuilder
                            {
                                Text = "PLAYER REPORT " + players.ElementAt(index).Value.ToUpper()
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
                                Value = identifier ?? "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "License",
                                Value = await mysqlReader.GetValue<string>("license") ?? "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Online",
                                Value = isPlayerOnline ? "Ja" : "Nee"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Contant",
                                Value = $"{await mysqlReader.GetValue<int>("money"):C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Bank",
                                Value = $"{await mysqlReader.GetValue<int>("bank"):C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Zwartgeld",
                                Value = $"{blackMoney:C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Boetes",
                                Value = $"{billing:C0}"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Hoofdbaan",
                                Value = $"{await mysqlReader.GetValue<string>("job")} ({await mysqlReader.GetValue<int>("job_grade")})"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Bijbaan",
                                Value = $"{await mysqlReader.GetValue<string>("job2")} ({await mysqlReader.GetValue<int>("job2_grade")})"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Verbannen",
                                Value = isBanned ? "Ja" : "Nee"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Reden",
                                Value = isBanned ? banReason : "-"
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Banner",
                                Value = isBanned ? bannedBy : "-"
                            },
                        });

                        if (isBanned)
                        {
                            var date = banExpire.UnixTimeStampToDateTime();
                            var dateString = date.ToLongDateString() + " " + date.ToLongTimeString();

                            embed.Fields.Add(
                                new EmbedFieldBuilder
                                {
                                    IsInline = true,
                                    Name = "Ban loopt af op",
                                    Value = dateString
                                });
                        }

                        await message.ModifyAsync(properties =>
                        {
                            properties.Embed = embed.Build();
                        });
                    }
                }
            }

            await mysqlConnection.CloseAsync();
        }
    }
}
