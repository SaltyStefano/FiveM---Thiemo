namespace MaaslandDiscordBot.Models.FiveM
{
    using System;

    public class PlayerJoinHistory
    {
        public DateTime? FirstJoin { get; set; }

        public DateTime? LastJoin { get; set; }

        public int TotalNumberOfDaysPlayed { get; set; }

        public int NumberOfDaysPlayedLastThirtyDays { get; set; }
    }
}
