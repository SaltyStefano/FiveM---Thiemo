namespace MaaslandDiscordBot.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using MaaslandDiscordBot.Helpers;
    using MaaslandDiscordBot.Models.BOT;
    using SteamWebAPI2.Interfaces;

    public static class BotExtensions
    {
        public static bool Contains(this string source, string toCheck, bool bCaseInsensitive)
        {
            return source.IndexOf(toCheck, bCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0;
        }

        public static string IndexToIcon(int index)
        {
            var icons = new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

            if (index <= 0)
            {
                return ":" + icons[0] + ":";
            }

            if (icons.Length >= (index + 1))
            {
                return ":" + icons[index] + ":";
            }

            return ":" + icons[8] + ":";
        }

        public static string IndexToEmoji(int index)
        {
            return UnicodeHelper.GetEmoji(IndexToIcon(index));
        }

        public static IEmote[] GetEmojis(int numberOfResults)
        {
            var emojies = new List<IEmote>();

            for (var i = 0; i < numberOfResults; i++)
            {
                emojies.Add(new Emoji(IndexToEmoji(i)));
            }

            return emojies.ToArray();
        }

        private static decimal ParseHexString(this string hexNumber)
        {
            hexNumber = hexNumber.Replace("x", string.Empty);

            long.TryParse(hexNumber, System.Globalization.NumberStyles.HexNumber, null, out var result);

            return result;
        }

        public static async Task<string> GetAvatarByIdentifier(this string identifier)
        {
            try
            {
                if (identifier.Contains("steam:", true))
                {
                    identifier = identifier.Replace("steam:", string.Empty);
                }

                var steamId = identifier.ParseHexString();
                var userInterface = BotConfiguration.GetSteamInterface<SteamUser>();
                var convertedSteamId = Convert.ToUInt64(steamId);

                var playerResponse = await userInterface.GetPlayerSummaryAsync(convertedSteamId);
                var playerData = playerResponse.Data;

                return playerData.IsNullOrDefault()
                    ? "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png"
                    : playerData.AvatarFullUrl;
            }
            catch (Exception)
            {
                return "https://maaslandrp.nl/assets/favicons/android-chrome-192x192.png";
            }
        }

        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();

            return dtDateTime;
        }

        public static string GenerateFilename(string name, string extension)
        {
            return $"{name}{DateTime.Now:yyyyMMddHHmmSS}.{extension}";
        }

        public static string GenerateFilePath(string filename, params string[] dirs)
        {
            var programPath = AppDomain.CurrentDomain.BaseDirectory;

            if (dirs.Any())
            {
                var filePath = $"{programPath}";

                filePath = dirs.Aggregate(filePath, (current, dir) => 
                    current + $"{dir}{Path.DirectorySeparatorChar}");

                return $"{filePath}{filename}";
            }

            return $"{programPath}{filename}";
        }

        public static int GetReaction(this MessageStore messageStore)
        {
            if (string.IsNullOrWhiteSpace(messageStore.Reaction))
            {
                return 0;
            }

            if (messageStore.Reaction == "1️⃣" || messageStore.Reaction == "1" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(0)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (messageStore.Reaction == "2️⃣" || messageStore.Reaction == "2" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(1)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (messageStore.Reaction == "3️⃣" || messageStore.Reaction == "3" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(2)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (messageStore.Reaction == "4️⃣" || messageStore.Reaction == "4" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(3)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (messageStore.Reaction == "5️⃣" || messageStore.Reaction == "5" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(4)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }

            if (messageStore.Reaction == "6️⃣" || messageStore.Reaction == "6" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(5)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            if (messageStore.Reaction == "7️⃣" || messageStore.Reaction == "7" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(6)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 7;
            }

            if (messageStore.Reaction == "8️⃣" || messageStore.Reaction == "8" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(7)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 8;
            }

            if (messageStore.Reaction == "9️⃣" || messageStore.Reaction == "9" ||
                string.Equals(UnicodeHelper.GetEmoji(IndexToIcon(8)), messageStore.Reaction, StringComparison.OrdinalIgnoreCase))
            {
                return 9;
            }

            return 0;
        }
    }
}
