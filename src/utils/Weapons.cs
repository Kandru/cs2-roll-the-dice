namespace RollTheDice.Utils
{
    /// <summary>Weapon classes by designer name ("weapon_ak47", "weapon_knife_t" ...).</summary>
    public static class Weapons
    {
        private static readonly string[] NonGuns =
        [
            "knife", "bayonet", "taser", "c4", "grenade", "flashbang", "molotov", "decoy", "healthshot", "fists", "melee",
            "axe", "hammer", "spanner", "tablet", "shield", "breachcharge", "bumpmine",
            // damage that is no bullet: explosions, fire, the world and dice damage (weapon "player")
            "env_", "inferno", "world", "player", "point_hurt", "prop_",
        ];

        /// <summary>True for firearms: per-shot effects (jams, blood magic) leave knives, grenades and the Zeus out.</summary>
        public static bool IsGun(string? weapon)
        {
            if (string.IsNullOrEmpty(weapon))
            {
                return false;
            }
            foreach (string part in NonGuns)
            {
                if (weapon.Contains(part, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>True when the player carries a weapon with this designer name ("weapon_decoy").</summary>
        public static bool Has(CounterStrikeSharp.API.Core.CCSPlayerController player, string designerName)
        {
            if (player.PlayerPawn.Value?.WeaponServices?.MyWeapons is not { } weapons)
            {
                return false;
            }
            foreach (var handle in weapons)
            {
                if (handle.Value is { IsValid: true } weapon && weapon.DesignerName == designerName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gives the item next frame unless the player already has one (a second grenade would fall to the floor).</summary>
        public static void GiveOnce(CounterStrikeSharp.API.Core.CCSPlayerController player, string designerName) =>
            CounterStrikeSharp.API.Server.NextFrame(() =>
            {
                if (Targets.IsAlive(player) && !Has(player, designerName))
                {
                    _ = player.GiveNamedItem(designerName);
                }
            });

        /// <summary>
        /// Drops the active gun and throws it away along the view (forward and up): a gun dropped at the feet is
        /// picked up again at once, so the player has to go after it. False when the active weapon is no gun.
        /// </summary>
        public static bool DropActiveGun(CounterStrikeSharp.API.Core.CCSPlayerController player, float throwSpeed)
        {
            if (player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.WeaponServices?.ActiveWeapon.Value is not { IsValid: true } weapon
                || !IsGun(weapon.DesignerName))
            {
                return false;
            }

            player.DropActiveWeapon();
            float yaw = pawn.EyeAngles.Y * MathF.PI / 180f;
            CounterStrikeSharp.API.Modules.Utils.Vector velocity = new(MathF.Cos(yaw) * throwSpeed, MathF.Sin(yaw) * throwSpeed, throwSpeed * 0.6f);
            CounterStrikeSharp.API.Server.NextFrame(() =>
            {
                if (weapon.IsValid)
                {
                    weapon.Teleport(null, null, velocity);
                }
            });
            return true;
        }

        /// <summary>
        /// Removes the player's guns (primary and secondary), like RandomWeapon: each one is made active, dropped and
        /// killed, so the client never keeps a ghost weapon. Knife, grenades and the Zeus stay.
        /// </summary>
        public static void RemoveGuns(CounterStrikeSharp.API.Core.CCSPlayerController player)
        {
            if (player.PlayerPawn.Value is not { IsValid: true, WeaponServices: { } services })
            {
                return;
            }
            List<CounterStrikeSharp.API.Core.CBasePlayerWeapon> guns = [];
            foreach (var handle in services.MyWeapons)
            {
                if (handle.Value is { IsValid: true } weapon && IsGun(weapon.DesignerName))
                {
                    guns.Add(weapon);
                }
            }
            foreach (CounterStrikeSharp.API.Core.CBasePlayerWeapon gun in guns)
            {
                services.ActiveWeapon.Raw = gun.EntityHandle.Raw;
                player.DropActiveWeapon();
                gun.AcceptInput("Kill");
            }
        }

        public static bool IsKnife(string? weapon) =>
            weapon != null && (weapon.Contains("knife", StringComparison.OrdinalIgnoreCase) || weapon.Contains("bayonet", StringComparison.OrdinalIgnoreCase));
    }
}
