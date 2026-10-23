using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class WeaponJam : DiceBlueprint
    {
        public override string ClassName => "WeaponJam";
        public override List<string> Events { get; } = ["EventWeaponFire"];
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];
        private readonly Random _random = new();

        // A jam blocks the trigger while the jammed gun is drawn; the magazine stays as it is and nothing reloads,
        // another weapon fires normally. m_bWaitForNoAttack (the flag after a round restart) alone lets a quick tap
        // through, because the engine clears it whenever attack is up: the gun's next attack tick and the pawn's next
        // attack time are pushed past the jam as well, and a shot that still gets out is harmless and refunded.
        private sealed record Jam(CBasePlayerWeapon Weapon, float Until);

        private readonly Dictionary<CCSPlayerController, Jam> _jams = [];

        public WeaponJam(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_jams.Remove(player, out Jam? jam))
            {
                Release(player, jam);
            }
        }

        public override void Reset()
        {
            base.Reset();
            foreach ((CCSPlayerController player, Jam jam) in _jams)
            {
                Release(player, jam);
            }
            _jams.Clear();
        }

        /// <summary>A shot can jam the gun: it cannot fire for a moment. A shot during a jam is given back.</summary>
        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || !Weapons.IsGun(@event.Weapon))
            {
                return HookResult.Continue;
            }

            if (_jams.TryGetValue(player, out Jam? jammed))
            {
                Server.NextFrame(() =>
                {
                    if (jammed.Weapon.IsValid)
                    {
                        jammed.Weapon.Clip1++;
                        Utilities.SetStateChanged(jammed.Weapon, "CBasePlayerWeapon", "m_iClip1");
                    }
                });
                Debug($"{player.PlayerName} shot through the jam (refunded, no damage)");
                return HookResult.Continue;
            }

            WeaponJamConfig config = _config.Dices.WeaponJam;
            if (_random.NextDouble() >= config.Chance)
            {
                return HookResult.Continue;
            }

            Server.NextFrame(() =>
            {
                if (!Targets.IsAlive(player)
                    || _jams.ContainsKey(player)
                    || player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not { IsValid: true } weapon
                    || !Weapons.IsGun(weapon.DesignerName))
                {
                    return;
                }

                Jam jam = new(weapon, Server.CurrentTime + config.JamSeconds);
                _jams[player] = jam;
                Hold(player, jam);
                Alert(player, "dice_WeaponJam_jammed");
                if (!string.IsNullOrEmpty(config.Sound))
                {
                    Reel.PlaySound(player, config.Sound);
                }
            });
            return HookResult.Continue;
        }

        /// <summary>A bullet that leaves a jammed gun deals no damage.</summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_jams.Count == 0
                || (info.BitsDamageType & DamageTypes_t.DMG_BULLET) == 0
                || info.Attacker.Value is not { } attacker
                || HolderOf(attacker) is not { } holder
                || !_jams.TryGetValue(holder, out Jam? jam)
                || holder.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Handle != jam.Weapon.Handle)
            {
                return HookResult.Continue;
            }
            return HookResult.Stop;
        }

        /// <summary>Keeps the jammed gun blocked while it is drawn and frees it when the jam is over.</summary>
        public void OnTick()
        {
            if (_jams.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, Jam jam) in _jams)
            {
                if (now >= jam.Until || !jam.Weapon.IsValid || !Targets.IsAlive(player))
                {
                    _ = _jams.Remove(player);
                    Release(player, jam);
                    Debug($"{player.PlayerName} jam over");
                    continue;
                }

                if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Handle == jam.Weapon.Handle)
                {
                    Hold(player, jam);
                }
                else
                {
                    SetWaitForNoAttack(player, false);
                }
            }
        }

        private static void Hold(CCSPlayerController player, Jam jam)
        {
            SetWaitForNoAttack(player, true);
            int untilTick = Server.TickCount + (int)MathF.Ceiling((jam.Until - Server.CurrentTime) / Server.TickInterval) + 1;
            if (jam.Weapon.NextPrimaryAttackTick < untilTick)
            {
                jam.Weapon.NextPrimaryAttackTick = untilTick;
                Utilities.SetStateChanged(jam.Weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            }
            if (Services(player) is { } services && services.NextAttack < jam.Until)
            {
                services.NextAttack = jam.Until;
            }
        }

        /// <summary>The gun fires again at once (the next attack tick goes back to now).</summary>
        private static void Release(CCSPlayerController player, Jam jam)
        {
            SetWaitForNoAttack(player, false);
            if (jam.Weapon.IsValid)
            {
                jam.Weapon.NextPrimaryAttackTick = Server.TickCount;
                Utilities.SetStateChanged(jam.Weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            }
            if (Services(player) is { } services)
            {
                services.NextAttack = Server.CurrentTime;
            }
        }

        private static CCSPlayer_WeaponServices? Services(CCSPlayerController player) =>
            player.IsValid && player.PlayerPawn.Value is { IsValid: true, WeaponServices: { } services }
                ? new CCSPlayer_WeaponServices(services.Handle)
                : null;

        private static void SetWaitForNoAttack(CCSPlayerController player, bool blocked)
        {
            if (player.IsValid && player.PlayerPawn.Value is { IsValid: true } pawn && pawn.WaitForNoAttack != blocked)
            {
                pawn.WaitForNoAttack = blocked;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_bWaitForNoAttack");
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class WeaponJamConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // chance per shot (0.0 - 1.0)
        [JsonPropertyName("chance")] public float Chance { get; set; } = 0.08f;
        [JsonPropertyName("jam_seconds")] public float JamSeconds { get; set; } = 2f;
        // played to the player on a jam (sound event or sounds/... path, empty = off)
        [JsonPropertyName("sound")] public string Sound { get; set; } = "sounds/weapons/clipempty_rifle.vsnd";
    }
}
