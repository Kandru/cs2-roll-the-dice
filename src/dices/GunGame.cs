using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class GunGame : DiceBlueprint
    {
        public override string ClassName => "GunGame";
        public override List<string> Events { get; } = ["EventPlayerDeath"];
        // weapon level per holder (index into the list)
        private readonly Dictionary<CCSPlayerController, int> _level = [];
        private static readonly string[] Pistols = ["glock", "hkp2000", "usp", "p250", "fiveseven", "tec9", "cz75", "deagle", "revolver", "elite"];

        public GunGame(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player) && _config.Dices.GunGame.Weapons.Count > 0)
            {
                _level[player] = 0;
                Server.NextFrame(() => Arm(player, 0));
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _level.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _level.Clear();
        }

        /// <summary>Every kill of the holder moves on to the next weapon of the list; the last one stays.</summary>
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? killer = @event.Attacker;
            if (_level.Count == 0
                || killer is not { IsValid: true }
                || killer == @event.Userid
                || !_level.TryGetValue(killer, out int level))
            {
                return HookResult.Continue;
            }

            int last = _config.Dices.GunGame.Weapons.Count - 1;
            if (level >= last)
            {
                return HookResult.Continue;
            }
            _level[killer] = ++level;
            // after the kill is through: the weapon that killed must not vanish inside the death event
            Server.NextFrame(() => Arm(killer, level));
            return HookResult.Continue;
        }

        private void Arm(CCSPlayerController player, int level)
        {
            List<string> list = _config.Dices.GunGame.Weapons;
            if (!IsAlivePlayer(player) || level < 0 || level >= list.Count)
            {
                return;
            }

            string weapon = list[level];
            Weapons.RemoveGuns(player);
            if (Weapons.IsGun(weapon))
            {
                _ = player.GiveNamedItem(weapon);
            }
            // draw it (a given weapon is not drawn by itself); the last level leaves the knife
            player.ExecuteClientCommand(!Weapons.IsGun(weapon) ? "slot3" : Pistols.Any(p => weapon.Contains(p, StringComparison.Ordinal)) ? "slot2" : "slot1");
            ShowStatus(player, "dice_GunGame_status", new()
            {
                { "level", (level + 1).ToString(CultureInfo.InvariantCulture) },
                { "max", list.Count.ToString(CultureInfo.InvariantCulture) },
                { "weapon", weapon.Replace("weapon_", string.Empty, StringComparison.Ordinal).ToUpperInvariant() },
            });
        }
    }
}

namespace RollTheDice.Configs
{
    public class GunGameConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // one weapon per kill; a non-gun entry (weapon_knife) as the last level leaves only the knife
        [JsonPropertyName("weapons")]
        public List<string> Weapons { get; set; } =
        [
            "weapon_ak47", "weapon_m4a1", "weapon_famas", "weapon_mp9", "weapon_p90", "weapon_nova",
            "weapon_ssg08", "weapon_deagle", "weapon_p250", "weapon_knife",
        ];
    }
}
