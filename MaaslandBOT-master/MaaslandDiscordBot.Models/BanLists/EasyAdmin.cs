namespace MaaslandDiscordBot.Models.BanLists
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class EasyAdmin
    {
        [JsonProperty("reason", Required = Required.Default)]
        public string Reason { get; set; }

        [JsonProperty("expire", Required = Required.Default)]
        public long Expire { get; set; }

        [JsonProperty("identifiers", Required = Required.Default)]
        public List<string> Identifiers { get; set; }

        [JsonProperty("banner", Required = Required.Default)]
        public string Banner { get; set; }

        [JsonProperty("1", Required = Required.Default)]
        public string Identifier { get; set; }
    }
}
