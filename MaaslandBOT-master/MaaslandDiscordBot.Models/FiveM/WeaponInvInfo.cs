namespace MaaslandDiscordBot.Models.FiveM
{
    using Newtonsoft.Json;

    public class WeaponInvInfo
    {
        [JsonProperty("ammo", Required = Required.Default)]
        public int Ammo { get; set; }
    }
}
