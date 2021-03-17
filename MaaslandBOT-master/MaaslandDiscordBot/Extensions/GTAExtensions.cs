namespace MaaslandDiscordBot.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    using MaaslandDiscordBot.Models.FiveM;

    public static class GTAExtensions
    {
        private static Dictionary<string, WeaponHash> WeaponHashes => new Dictionary<string, WeaponHash>
        {
            { "WEAPON_DAGGER", WeaponHash.Dagger },
            { "WEAPON_BAT", WeaponHash.Bat },
            { "WEAPON_BOTTLE", WeaponHash.Bottle },
            { "WEAPON_CROWBAR", WeaponHash.Crowbar },
            { "WEAPON_UNARMED", WeaponHash.Unarmed },
            { "WEAPON_FLASHLIGHT", WeaponHash.Flashlight },
            { "WEAPON_GOLFCLUB", WeaponHash.GolfClub },
            { "WEAPON_HAMMER", WeaponHash.Hammer },
            { "WEAPON_HATCHET", WeaponHash.Machete },
            { "WEAPON_KNUCKLE", WeaponHash.KnuckleDuster },
            { "WEAPON_KNIFE", WeaponHash.Knife },
            { "WEAPON_MACHETE", WeaponHash.Machete },
            { "WEAPON_SWITCHBLADE", WeaponHash.SwitchBlade },
            { "WEAPON_NIGHTSTICK", WeaponHash.Nightstick },
            { "WEAPON_WRENCH", WeaponHash.Wrench },
            { "WEAPON_BATTLEAXE", WeaponHash.BattleAxe },
            { "WEAPON_POOLCUE", WeaponHash.PoolCue },
            { "WEAPON_PISTOL", WeaponHash.Pistol },
            { "WEAPON_PISTOL_MK2", WeaponHash.PistolMk2 },
            { "WEAPON_COMBATPISTOL", WeaponHash.CombatPistol },
            { "WEAPON_APPISTOL", WeaponHash.APPistol },
            { "WEAPON_STUNGUN", WeaponHash.StunGun },
            { "WEAPON_PISTOL50", WeaponHash.Pistol50 },
            { "WEAPON_SNSPISTOL", WeaponHash.SNSPistol },
            { "WEAPON_HEAVYPISTOL", WeaponHash.HeavyPistol },
            { "WEAPON_VINTAGEPISTOL", WeaponHash.VintagePistol },
            { "WEAPON_FLAREGUN", WeaponHash.FlareGun },
            { "WEAPON_MARKSMANPISTOL", WeaponHash.MarksmanPistol },
            { "WEAPON_REVOLVER", WeaponHash.Revolver },
            { "WEAPON_CERAMICPISTOL", WeaponHash.Unarmed },
            { "WEAPON_NAVYREVOLVER", WeaponHash.Unarmed },
            { "WEAPON_MICROSMG", WeaponHash.MicroSMG },
            { "WEAPON_SMG", WeaponHash.SMG },
            { "WEAPON_SMG_MK2", WeaponHash.SMGMk2 },
            { "WEAPON_ASSAULTSMG", WeaponHash.AssaultSMG },
            { "WEAPON_COMBATPDW", WeaponHash.CombatPDW },
            { "WEAPON_MACHINEPISTOL", WeaponHash.MachinePistol },
            { "WEAPON_MINISMG", WeaponHash.MiniSMG },
            { "WEAPON_PUMPSHOTGUN", WeaponHash.PumpShotgun },
            { "WEAPON_SAWNOFFSHOTGUN", WeaponHash.SawnOffShotgun },
            { "WEAPON_ASSAULTSHOTGUN", WeaponHash.AssaultShotgun },
            { "WEAPON_BULLPUPSHOTGUN", WeaponHash.BullpupShotgun },
            { "WEAPON_MUSKET", WeaponHash.Musket },
            { "WEAPON_HEAVYSHOTGUN", WeaponHash.HeavyShotgun },
            { "WEAPON_DBSHOTGUN", WeaponHash.DoubleBarrelShotgun },
            { "WEAPON_AUTOSHOTGUN", WeaponHash.SweeperShotgun },
            { "WEAPON_ASSAULTRIFLE", WeaponHash.AssaultRifle },
            { "WEAPON_ASSAULTRIFLE_MK2", WeaponHash.AssaultRifleMk2 },
            { "WEAPON_CARBINERIFLE", WeaponHash.CarbineRifle },
            { "WEAPON_CARBINERIFLE_MK2", WeaponHash.CarbineRifleMk2 },
            { "WEAPON_ADVANCEDRIFLE", WeaponHash.AdvancedRifle },
            { "WEAPON_SPECIALCARBINE", WeaponHash.SpecialCarbine },
            { "WEAPON_COMPACTRIFLE", WeaponHash.CompactRifle },
            { "WEAPON_MG", WeaponHash.MG },
            { "WEAPON_COMBATMG", WeaponHash.CombatMG },
            { "WEAPON_COMBATMG_MK2", WeaponHash.CombatMGMk2 },
            { "WEAPON_GUSENBERG", WeaponHash.Gusenberg },
            { "WEAPON_SNIPERRIFLE", WeaponHash.SniperRifle },
            { "WEAPON_HEAVYSNIPER", WeaponHash.HeavySniper },
            { "WEAPON_HEAVYSNIPER_MK2", WeaponHash.HeavySniperMk2 },
            { "WEAPON_MARKSMANRIFLE", WeaponHash.MarksmanRifle },
            { "WEAPON_RPG", WeaponHash.RPG },
            { "WEAPON_GRENADELAUNCHER", WeaponHash.GrenadeLauncher },
            { "WEAPON_GRENADELAUNCHER_SMOKE", WeaponHash.GrenadeLauncherSmoke },
            { "WEAPON_MINIGUN", WeaponHash.Minigun },
            { "WEAPON_FIREWORK", WeaponHash.Firework },
            { "WEAPON_RAILGUN", WeaponHash.Railgun },
            { "WEAPON_HOMINGLAUNCHER", WeaponHash.HomingLauncher },
            { "WEAPON_GRENADE", WeaponHash.Grenade },
            { "WEAPON_BZGAS", WeaponHash.BZGas },
            { "WEAPON_MOLOTOV", WeaponHash.Molotov },
            { "WEAPON_STICKYBOMB", WeaponHash.StickyBomb },
            { "WEAPON_PROXMINE", WeaponHash.ProximityMine },
            { "WEAPON_SNOWBALL", WeaponHash.Snowball },
            { "WEAPON_PIPEBOMB", WeaponHash.PipeBomb },
            { "WEAPON_BALL", WeaponHash.Ball },
            { "WEAPON_SMOKEGRENADE", WeaponHash.SmokeGrenade },
            { "WEAPON_FLARE", WeaponHash.Flare },
            { "WEAPON_PETROLCAN", WeaponHash.PetrolCan },
            { "GADGET_PARACHUTE", WeaponHash.Parachute },
            { "WEAPON_FIREEXTINGUISHER", WeaponHash.FireExtinguisher },
            { "WEAPON_HAZARDCAN", WeaponHash.PetrolCan },
            { "WEAPON_BULLPUPRIFLE", WeaponHash.BullpupRifle },
            { "GADGET_NIGHTVISION", WeaponHash.NightVision },
            { "WEAPON_COMPACTLAUNCHER", WeaponHash.CompactGrenadeLauncher },
            { "WEAPON_REVOLVER_MK2", WeaponHash.RevolverMk2 },
            { "WEAPON_SPECIALCARBINE_MK2", WeaponHash.SpecialCarbineMk2 },
            { "WEAPON_MARKSMANRIFLE_MK2", WeaponHash.MarksmanRifleMk2 },
            { "WEAPON_PUMPSHOTGUN_MK2", WeaponHash.PumpShotgunMk2 },
            { "WEAPON_SNSPISTOL_MK2", WeaponHash.SNSPistolMk2 },
            { "WEAPON_BULLPUPRIFLE_MK2", WeaponHash.BullpupRifleMk2 },
            { "WEAPON_DOUBLEACTION", WeaponHash.DoubleAction },
            { "WEAPON_STONE_HATCHET", WeaponHash.StoneHatchet },
            { "WEAPON_RAYPISTOL", WeaponHash.RayPistol },
            { "WEAPON_RAYMINIGUN", WeaponHash.RayMinigun },
            { "WEAPON_RAYCARBINE", WeaponHash.RayCarbine }
        };

        public static WeaponHash GetWeaponHashByID(this string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return WeaponHash.Unarmed;
            }

            var weaponName = weaponId.ToUpper().Trim();

            if (WeaponHashes.ContainsKey(weaponName))
            {
                return WeaponHashes[weaponName];
            }

            return WeaponHash.Unarmed;
        }

        public static string GetWeaponIdByHash(this WeaponHash weaponHash)
        {
            if (weaponHash.IsNullOrDefault())
            {
                return "WEAPON_UNARMED";
            }

            var weapon = WeaponHashes.FirstOrDefault(wh => wh.Value == weaponHash);

            return weapon.IsNullOrDefault()
                ? "WEAPON_UNARMED"
                : weapon.Key;
        }
    }
}
