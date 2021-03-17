namespace MaaslandDiscordBot.Models.FiveM
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class Player
    {
        [JsonProperty("id", Required = Required.Default)]
        public int Id { get; set; }

        [JsonProperty("endpoint", Required = Required.Default)]
        public string Endpoint { get; set; }

        [JsonProperty("identifiers", Required = Required.Default)]
        public List<string> Identifiers { get; set; }

        [JsonProperty("name", Required = Required.Default)]
        public string Name { get; set; }

        [JsonProperty("ping", Required = Required.Default)]
        public int Ping { get; set; }
    }
}
