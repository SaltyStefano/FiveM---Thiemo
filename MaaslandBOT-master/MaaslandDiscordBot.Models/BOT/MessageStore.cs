namespace MaaslandDiscordBot.Models.BOT
{
    using System.Collections.Generic;

    public class MessageStore
    {
        public ulong MessageId { get; set; }

        public ulong UserId { get; set; }

        public ulong ChannelId { get; set; }

        public string Reaction { get; set; }

        public string Command { get; set; }

        public Dictionary<string, string> Players { get; set; }
    }
}
