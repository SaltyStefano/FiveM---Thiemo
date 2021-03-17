namespace MaaslandDiscordBot.Models.FiveM
{
    public class Item
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public int Count { get; set; }

        public int Limit { get; set; }

        public bool Rare { get; set; }

        public bool CanRemove { get; set; }

        public int Weight { get; set; }
    }
}
