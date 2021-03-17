namespace MaaslandDiscordBot.Base.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Discord;
    using Discord.WebSocket;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Interfaces.Commands;
    using MaaslandDiscordBot.Models.BOT;
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;

    public abstract class BaseCommands : ICommands
    {
        private Dictionary<string, Func<string[], SocketMessage, Task>> Commands { get; } = new Dictionary<string, Func<string[], SocketMessage, Task>>();

        public string[] GetCommands => Commands.Select(x => x.Key).ToArray();

        public abstract string Function { get; }

        protected DiscordSocketClient discordSocketClient;

        public void RegisterCommand(string command, Func<string[], SocketMessage, Task> action)
        {
            if (Commands.ContainsKey(command))
            {
                Commands[command] = action;
            }
            else
            {
                Commands.Add(command, action);
            }
        }

        public async Task ExecuteCommand(string command, string[] arguments, SocketMessage message, DiscordSocketClient discordClient)
        {
            discordSocketClient = discordClient;

            if (Commands.ContainsKey(command) && IsPlayerAllowedToExecuteCommand(message))
            {
                await Commands[command](arguments, message);
            } 
            else if (!IsPlayerAllowedToExecuteCommand(message))
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + $" Je moet een van de volgende rollen hebben: " + GetAllAllowedRoles());
            }
        }

        public bool IsPlayerAllowedToExecuteCommand(SocketMessage message)
        {
            var author = message.Author as SocketGuildUser;

            foreach (var role in author.Roles)
            {
                if (BotConfiguration.RequiredRanks.Any(rank => rank.ToLower().Trim() == role.Name.ToLower().Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPlayerDiscordModerator(SocketMessage message)
        {
            var author = message.Author as SocketGuildUser;

            foreach (var role in author.Roles)
            {
                if (BotConfiguration.ModRanks.Any(rank => rank.ToLower().Trim() == role.Name.ToLower().Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetAllAllowedRoles()
        {
            var returnString = string.Empty;

            foreach (var role in BotConfiguration.RequiredRanks)
            {
                if (string.IsNullOrWhiteSpace(returnString))
                {
                    returnString += "@" + role.Trim();
                }
                else
                {
                    returnString += ", @" + role.Trim();
                }
            }

            return returnString;
        }

        public string GetAllAllowedModRoles()
        {
            var returnString = string.Empty;

            foreach (var role in BotConfiguration.ModRanks)
            {
                if (string.IsNullOrWhiteSpace(returnString))
                {
                    returnString += "@" + role.Trim();
                }
                else
                {
                    returnString += ", @" + role.Trim();
                }
            }

            return returnString;
        }

        public bool IsFunction(string function)
        {
            return string.Equals(Function, function, StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task PlayerLookupCommand(
            string[] arguments, 
            SocketMessage message)
        {
            var function = Function;

            if (arguments.Length <= 1)
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + $" Je moet `-{function} [player]` gebruiken om mij aan het werk te zetten");
                return;
            }

            var players = new Dictionary<string, string>();
            var playerNameArgs = new List<string>(arguments);
            
            if (playerNameArgs.Any())
            {
                playerNameArgs.RemoveAt(0);
            }

            var player = string.Join(" ", playerNameArgs);

            var discordBotMessage = await message.Channel.SendMessageAsync(message.Author.Mention + $" Ik ben opzoek naar `{player}` voor je");

            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            using (var mySqlCommand = new MySqlCommand("SELECT `name`, `identifier` FROM `user_logs` WHERE LOWER(`name`) LIKE @name GROUP BY `identifier`, `name` ORDER BY `name` ASC LIMIT 9", mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@name", "%" + player.ToLower() + "%");

                using (var mysqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mysqlReader.HasRows)
                    {
                        await discordBotMessage.ModifyAsync(properties =>
                        {
                            properties.Content = $"{message.Author.Mention} Speler niet gevonden";
                            properties.Embed = null;
                        });

                        return;
                    }

                    while (await mysqlReader.ReadAsync())
                    {
                        if (!players.ContainsKey(await mysqlReader.GetValue<string>("identifier")))
                        {
                            players.Add(await mysqlReader.GetValue<string>("identifier"), await mysqlReader.GetValue<string>("name"));
                        }
                    }
                }
            }

            await mysqlConnection.CloseAsync();

            await discordBotMessage.ModifyAsync(properties =>
            {
                properties.Content = $"{message.Author.Mention} Ik heb de volgende spelers gevonden:";

                for (var i = 0; i < players.Count; i++)
                {
                    var playerName = players.ElementAt(i).Value;

                    properties.Content += $"\n{BotExtensions.IndexToIcon(i)} **" + playerName + "**";
                }
            });

            var emojies = new List<IEmote>();

            for (var i = 0; i < players.Count; i++)
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
                Players = players
            };

            await AddOrUpdateRecord(messageStore);
            await discordBotMessage.AddReactionsAsync(emojies.ToArray());
        }

        public async Task SocietyLookupCommand(
            string[] arguments,
            SocketMessage message)
        {
            var function = Function;

            if (arguments.Length <= 1)
            {
                await message.Channel.SendMessageAsync(message.Author.Mention + $" Je moet `-{function} [society]` gebruiken om mij aan het werk te zetten");
                return;
            }

            var societies = new Dictionary<string, string>();
            var society = arguments[1];
            var discordBotMessage = await message.Channel.SendMessageAsync(message.Author.Mention + $" Ik ben opzoek naar `{society}` voor je");

            var mysqlConnection = new MySqlConnection(BotConfiguration.MySQL);

            await mysqlConnection.OpenAsync();

            using (var mySqlCommand = new MySqlCommand("SELECT `name`, `label` FROM `jobs` WHERE LOWER(`name`) LIKE @name OR LOWER(`label`) LIKE @name ORDER BY `label` ASC LIMIT 9", mysqlConnection))
            {
                mySqlCommand.Parameters.AddWithValue("@name", "%" + society.ToLower() + "%");

                using (var mysqlReader = await mySqlCommand.ExecuteReaderAsync())
                {
                    if (!mysqlReader.HasRows)
                    {
                        await discordBotMessage.ModifyAsync(properties =>
                        {
                            properties.Content = $"{message.Author.Mention} Society niet gevonden";
                            properties.Embed = null;
                        });

                        return;
                    }

                    while (await mysqlReader.ReadAsync())
                    {
                        var label = await mysqlReader.GetValue<string>("name");

                        if (!societies.ContainsKey(label))
                        {
                            societies.Add(await mysqlReader.GetValue<string>("name"), await mysqlReader.GetValue<string>("label"));
                        }
                    }
                }
            }

            await mysqlConnection.CloseAsync();

            await discordBotMessage.ModifyAsync(properties =>
            {
                properties.Content = $"{message.Author.Mention} Ik heb de volgende bedrijven gevonden:";

                for (var i = 0; i < societies.Count; i++)
                {
                    var playerName = societies.ElementAt(i).Value;

                    properties.Content += $"\n{BotExtensions.IndexToIcon(i)} **" + playerName + "**";
                }
            });

            var emojies = new List<IEmote>();

            for (var i = 0; i < societies.Count; i++)
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
                Players = societies
            };

            await AddOrUpdateRecord(messageStore);
            await discordBotMessage.AddReactionsAsync(emojies.ToArray());
        }

        public abstract Task ActionHandler(
            Dictionary<string, string> players,
            IUserMessage message,
            MessageStore messageStore);

        public static async Task Save()
        {
            if (Program.MessageStores.IsNullOrDefault() || Program.MessageStores.Count <= 0)
            {
                Program.MessageStores.Clear();
            }

            var json = JsonConvert.SerializeObject(Program.MessageStores, Formatting.Indented);
            var filename = $"{BotConfiguration.BOT["MessageStore"]}.json";
            var programPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = $"{programPath}{filename}.json";

            if (File.Exists(filePath) && !IsFileLocked(filePath))
            {
                File.Delete($"{programPath}{filename}.json");
            }

            using (var sw = new StreamWriter(filePath))
            {
                await sw.WriteAsync(json);
            }
        }

        public static async Task Load()
        {
            if (Program.MessageStores.Count <= 0)
            {
                Program.MessageStores.Clear();
            }

            var filename = $"{BotConfiguration.BOT["MessageStore"]}.json";
            var programPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = $"{programPath}{filename}.json";

            if (File.Exists(filePath) && !IsFileLocked(filePath))
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var rawData = await reader.ReadToEndAsync();
                        var data = JsonConvert.DeserializeObject<List<MessageStore>>(rawData);

                        Program.MessageStores.AddRange(data);
                    }
                }
            }
        }

        public static async Task AddOrUpdateRecord(MessageStore messageStore)
        {
            if (messageStore.IsNullOrDefault() ||
                Program.MessageStores.IsNullOrDefault() ||
                Program.MessageStores.Count <= 0)
            {
                await Load();
            }

            if (Program.MessageStores.Any(y => y.MessageId == messageStore.MessageId))
            {
                foreach (var msgs in Program.MessageStores
                    .Where(msgs => msgs.MessageId == messageStore.MessageId && 
                                   msgs.ChannelId == messageStore.ChannelId))
                {
                    msgs.MessageId = messageStore.MessageId;
                    msgs.ChannelId = messageStore.ChannelId;
                    msgs.UserId = messageStore.UserId;
                    msgs.Command = messageStore.Command;
                    msgs.Players = messageStore.Players;
                    msgs.Reaction = messageStore.Reaction;

                    await Save();

                    return;
                }
            }

            Program.MessageStores.Add(messageStore);

            await Save();
        }

        private static bool IsFileLocked(string filePath)
        {
            var file = new FileInfo(filePath);

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        
    }
}
