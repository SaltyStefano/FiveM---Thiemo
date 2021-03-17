namespace MaaslandDiscordBot.Models.FiveM
{
    using Newtonsoft.Json;

    public class Layout
    {
        [JsonProperty("name", Required = Required.Default)]
        public string Name { get; set; }

        [JsonProperty("ammo", Required = Required.Default)]
        public int Ammo { get; set; }

        [JsonProperty("count", Required = Required.Default)]
        public int Count { get; set; }

        [JsonProperty("label", Required = Required.Default)]
        public string Label { get; set; }
    }
}
