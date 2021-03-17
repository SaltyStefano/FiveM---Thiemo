namespace MaaslandDiscordBot.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.Extensions.Configuration;
    using SteamWebAPI2.Utilities;

    public static class BotConfiguration
    {
        public static string Token { get; private set; }

        public static string MySQL { get; private set; }

        public static Dictionary<string, string> FiveM { get; private set; }

        public static Dictionary<string, string> BanLists { get; private set; }

        public static Dictionary<string, string> Steam { get; private set; }

        public static Dictionary<string, string> BOT { get; private set; }

        public static List<string> RequiredRanks { get; private set; }

        public static List<string> ModRanks { get; private set; }

        public static List<string> BlacklistWeapons { get; private set; }

        public static List<string> IgnoreIPs { get; private set; }

        private static SteamWebInterfaceFactory SteamWebInterfaceFactory { get; set; }

        public static void Build()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", false)
                .Build();

            Token = configuration[nameof(Token)];
            MySQL = configuration[nameof(MySQL)];

            FiveM = configuration.GetSection(nameof(FiveM)).GetChildren()
                .Select(list => new KeyValuePair<string, string>(list.Key, list.Value))
                .ToDictionary(list => list.Key, list => list.Value);

            BanLists = configuration.GetSection(nameof(BanLists)).GetChildren()
                .Select(list => new KeyValuePair<string, string>(list.Key, list.Value))
                .ToDictionary(list => list.Key, list => list.Value);

            Steam = configuration.GetSection(nameof(Steam)).GetChildren()
                .Select(list => new KeyValuePair<string, string>(list.Key, list.Value))
                .ToDictionary(list => list.Key, list => list.Value);

            BOT = configuration.GetSection(nameof(BOT)).GetChildren()
                .Select(list => new KeyValuePair<string, string>(list.Key, list.Value))
                .ToDictionary(list => list.Key, list => list.Value);

            RequiredRanks = configuration.GetSection(nameof(RequiredRanks))
                .GetChildren()
                .Select(x => x.Value)
                .ToList();

            ModRanks = configuration.GetSection(nameof(ModRanks))
                .GetChildren()
                .Select(x => x.Value)
                .ToList();

            BlacklistWeapons = configuration.GetSection(nameof(BlacklistWeapons))
                .GetChildren()
                .Select(x => x.Value)
                .ToList();

            IgnoreIPs = configuration.GetSection(nameof(IgnoreIPs))
                .GetChildren()
                .Select(x => x.Value)
                .ToList();

            GenerateSteamLayer();
        }

        private static void GenerateSteamLayer()
        {
            SteamWebInterfaceFactory = new SteamWebInterfaceFactory(Steam["Key"]);
        }

        public static TInterface GetSteamInterface<TInterface>()
            where TInterface : class
        {
            return SteamWebInterfaceFactory.CreateSteamWebInterface<TInterface>(new HttpClient());
        }
    }
}
