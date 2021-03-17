namespace MaaslandDiscordBot.Models.FiveM
{
    using Newtonsoft.Json;

    public class WeaponData
    {
        [JsonProperty("weapons", Required = Required.DisallowNull)]
        public Layout[] Weapons { get; set; }
    }
}
