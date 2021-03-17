namespace MaaslandDiscordBot
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.ServiceProcess;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.WebSocket;
    using MaaslandDiscordBot.Base.Commands;
    using MaaslandDiscordBot.Commands.Discord;
    using MaaslandDiscordBot.Commands.FiveM;
    using MaaslandDiscordBot.Commands.General;
    using MaaslandDiscordBot.Extensions;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Interfaces.Commands;
    using MaaslandDiscordBot.Models.BOT;

    public class Program
    {
        public static List<ICommands> Commands { get; } = new List<ICommands>();

        public static List<MessageStore> MessageStores { get; } = new List<MessageStore>();

        private readonly DiscordSocketClient discordClient;

        public static void Main(string[] args)
        {
            var culture = new CultureInfo("nl-NL");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (!Environment.UserInteractive)
            {
                var botService = new BOTService();

                ServiceBase.Run(botService);
            }
            else
            {
                if (args != null && args.Length > 0)
                {
                    if (args[0].Equals("-i", StringComparison.OrdinalIgnoreCase))
                    {
                        AutoInstaller.InstallMe();
                    }
                    else
                    {
                        if (args[0].Equals("-u", StringComparison.OrdinalIgnoreCase))
                        {
                            AutoInstaller.UninstallMe();
                        }
                        else
                        {
                            Console.WriteLine("Invalid argument!");
                        }
                    }
                }
                else
                {
                    var program = new Program();
                    var start = program.Start();
                    var awaiter = start.GetAwaiter();

                    awaiter.GetResult();
                }
            }
        }

        public Program()
        {
            discordClient = new DiscordSocketClient();

            LoadCommands();

            discordClient.Log += LogAsync;
            discordClient.Ready += ReadyAsync;
            discordClient.MessageReceived += MessageReceivedAsync;
            discordClient.ReactionAdded += ReactionReceivedAsync;
        }

        public async Task Start()
        {
            BotConfiguration.Build();

            await BaseCommands.Load();

            await discordClient.LoginAsync(TokenType.Bot, BotConfiguration.Token);
            await discordClient.StartAsync();

            await Task.Delay(-1);
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{discordClient.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == discordClient.CurrentUser.Id || string.IsNullOrEmpty(message.Content))
            {
                return Task.CompletedTask;
            }

            var content = message.Content.Trim();
            var arguments = content.Split(' ');

            if (arguments.Length <= 0)
            {
                return Task.CompletedTask;
            }

            var command = arguments[0];

            foreach (var commandHandler in Commands)
            {
                if (commandHandler.GetCommands.Any(cmd =>
                    cmd.Equals(command, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var thread = new Thread(() => commandHandler.ExecuteCommand(command, arguments, message, discordClient))
                    {
                        Priority = ThreadPriority.Highest,
                        IsBackground = true
                    };

                    thread.Start();

                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private static async Task ReactionReceivedAsync(
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (MessageStores.IsNullOrDefault() || MessageStores.Count <= 0)
            {
                await BaseCommands.Load();
            }

            var messageStore = MessageStores.FirstOrDefault(
                ms => ms.MessageId == message.Id &&
                      ms.UserId == reaction.UserId &&
                      ms.ChannelId == channel.Id);

            if (messageStore.IsNullOrDefault())
            {
                return;
            }

            foreach (var command in Commands.Where(command => command.IsFunction(messageStore.Command)))
            {
                messageStore.Reaction = reaction.Emote.Name;

                await BaseCommands.AddOrUpdateRecord(messageStore);

                var msg = await message.GetOrDownloadAsync();
                var thread = new Thread(() => command.ActionHandler(messageStore.Players, msg, messageStore))
                {
                    Priority = ThreadPriority.Highest,
                    IsBackground = true
                };

                thread.Start();
            }
        }

        private static void LoadCommands()
        {
            var commands = new List<ICommands>
            {
                new PlayerCommands(),
                new IdsCommands(),
                new WeaponsCommands(),
                new HistoryCommands(),
                new DiscordCommands(),
                new SocietyCommands(),
                new InvCommands(),
                new HelpCommands(),
                new LogCommands()
            };

            Commands.Clear();
            Commands.AddRange(commands);
        }
    }
}
