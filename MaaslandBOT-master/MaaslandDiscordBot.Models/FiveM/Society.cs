namespace MaaslandDiscordBot.Models.FiveM
{
    using System;
    using System.Collections.Generic;

    public class Society
    {
        public string Name { get; set; } = string.Empty;

        public int Bank { get; set; } = 0;

        public int Wash { get; set; } = 0;

        public int Dirty { get; set; } = 0;

        public List<Tuple<string, string, string>> Members { get; } = new List<Tuple<string, string, string>>();

        public Dictionary<string, int> Weapons { get; } = new Dictionary<string, int>();
    }
}
