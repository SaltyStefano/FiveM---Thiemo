namespace MaaslandDiscordBot.Models.BOT
{
    using MaaslandDiscordBot.Models.Enums;
    using MaaslandDiscordBot.Models.FiveM;

    public class WeaponStore
    {
        public WeaponStore(WeaponHash hash, StoreType type)
        {
            Hash = hash;
            Count = 1;
            Type = type;
            TypeData = string.Empty;
        }

        public WeaponStore(WeaponHash hash, StoreType type, string data)
        {
            Hash = hash;
            Count = 1;
            Type = type;
            TypeData = data;
        }

        public WeaponStore(WeaponHash hash, StoreType type, int count)
        {
            Hash = hash;
            Count = count;
            Type = type;
            TypeData = string.Empty;
        }

        public WeaponStore(WeaponHash hash, StoreType type, int count, string data)
        {
            Hash = hash;
            Count = count;
            Type = type;
            TypeData = data;
        }

        public WeaponHash Hash { get; set; }

        public int Count { get; set; }

        public StoreType Type { get; set; }

        public string TypeData { get; set; }
    }
}
