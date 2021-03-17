namespace MaaslandDiscordBot.Models.BanLists
{
    using Newtonsoft.Json;

    public class AntiCheat
    {
        [JsonProperty("name", Required = Required.Default)]
        public string Name { get; set; }

        [JsonProperty("identifiers", Required = Required.Default)]
        public string[] Identifiers { get; set; }

        [JsonProperty("reason", Required = Required.Default)]
        public string Reason { get; set; }
    }
}
