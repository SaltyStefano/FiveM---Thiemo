namespace MaaslandDiscordBot.Models.FiveM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class WeaponInfo
    {
        public Dictionary<string, int> PlayerAmountOfWeapons = new Dictionary<string, int>();

        public string Name { get; set; }

        public int Count()
        {
            if (PlayerAmountOfWeapons == null || PlayerAmountOfWeapons.Count <= default(int))
            {
                return default(int);
            }

            return PlayerAmountOfWeapons.Sum(x => x.Value);
        }

        public void AddNumberOfWeaponsToPlayer(string identifier, int amount)
        {
            if (PlayerAmountOfWeapons == null || PlayerAmountOfWeapons.Count <= default(int))
            {
                PlayerAmountOfWeapons = new Dictionary<string, int>();
            }

            if (!PlayerAmountOfWeapons.ContainsKey(identifier))
            {
                PlayerAmountOfWeapons.Add(identifier, amount);
            }
            else
            {
                PlayerAmountOfWeapons[identifier] += amount;
            }
        }
    }
}
