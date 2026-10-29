using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class QuickReload : DiceBlueprint
    {
        public override string ClassName => "QuickReload";
        public override List<string> Events { get; } = ["EventWeaponReload"];
        public override List<string> Listeners { get; } = ["OnTick"];

        // the engine's own reload keeps running after the early finish and moves ammo once more when its animation
        // ends (a whole magazine for guns that count magazines): watch the gun until then and undo that
        private const float GuardSeconds = 6f;
        // a reload whose length the engine does not tell (next attack tick not ahead) counts as this long
        private const float FallbackReloadSeconds = 2.5f;

        private readonly Dictionary<CCSPlayerController, Reload> _reloads = [];
        private readonly Dictionary<CCSPlayerController, Guard> _guards = [];

        /// <summary>A running reload; <see cref="DoneAt"/> (end of the sped up animation) is known from its first tick.</summary>
        private sealed class Reload(CCSWeaponBase weapon, float startedAt, int startTick, float originalRate)
        {
            public CCSWeaponBase Weapon { get; } = weapon;
            public float StartedAt { get; } = startedAt;
            public int StartTick { get; } = startTick;
            public float OriginalRate { get; } = originalRate;
            public float DoneAt { get; set; } = float.NaN;
        }

        /// <summary>The gun's ammo as of the last tick, kept while the engine's reload may still land.</summary>
        private sealed class Guard(CCSWeaponBase weapon, float until)
        {
            public CCSWeaponBase Weapon { get; } = weapon;
            public float Until { get; } = until;
            public int Clip { get; set; } = weapon.Clip1;
            public int Reserve { get; set; } = weapon.ReserveAmmo[0];
        }

        public QuickReload(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_reloads.Remove(player, out Reload? reload))
            {
                SetRate(reload.Weapon, reload.OriginalRate);
            }
            _ = _guards.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            foreach (Reload reload in _reloads.Values)
            {
                SetRate(reload.Weapon, reload.OriginalRate);
            }
            _reloads.Clear();
            _guards.Clear();
        }

        public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not { IsValid: true } active
                || !Weapons.IsGun(active.DesignerName))
            {
                return HookResult.Continue;
            }
            // the reload animation plays faster on the client; the ammo follows in OnTick when it ends
            CCSWeaponBase weapon = active.As<CCSWeaponBase>();
            if (!_reloads.ContainsKey(player))
            {
                _reloads[player] = new Reload(weapon, Server.CurrentTime, Server.TickCount, weapon.WeaponActionPlaybackRate);
            }
            SetRate(weapon, Speed);
            return HookResult.Continue;
        }

        private float Speed => Math.Max(1f, _config.Dices.QuickReload.ReloadSpeed);

        private static void AllowAttack(CCSWeaponBase weapon)
        {
            if (weapon.NextPrimaryAttackTick <= Server.TickCount)
            {
                return;
            }
            weapon.NextPrimaryAttackTick = Server.TickCount;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
        }

        private static void SetRate(CCSWeaponBase weapon, float rate)
        {
            if (!weapon.IsValid || weapon.WeaponActionPlaybackRate == rate)
            {
                return;
            }
            weapon.WeaponActionPlaybackRate = rate;
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_flWeaponActionPlaybackRate");
        }

        /// <summary>
        /// Finishes a running reload when its sped up animation ends: the magazine is filled from the reserve and the gun
        /// fires at once. A reload that was cancelled (weapon switch) only gets its normal animation rate back.
        /// </summary>
        public void OnTick()
        {
            if (_reloads.Count == 0 && _guards.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, Reload reload) in _reloads)
            {
                CCSWeaponBase weapon = reload.Weapon;
                // cancelled (weapon switch)
                if (!weapon.IsValid
                    || player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Handle != weapon.Handle)
                {
                    _ = _reloads.Remove(player);
                    SetRate(weapon, reload.OriginalRate);
                    continue;
                }
                // the engine loaded the gun itself when the fast animation ended (it moved the ammo), but its attack
                // block still waits for the normal reload time
                if (!weapon.InReload)
                {
                    _ = _reloads.Remove(player);
                    SetRate(weapon, reload.OriginalRate);
                    AllowAttack(weapon);
                    _guards[player] = new Guard(weapon, now + GuardSeconds);
                    Debug($"{player.PlayerName} quick reload: engine loaded {weapon.DesignerName} itself, clip {weapon.Clip1}, reserve {weapon.ReserveAmmo[0]}");
                    continue;
                }
                if (float.IsNaN(reload.DoneAt))
                {
                    // at its start the engine blocks attacks until its own (normal speed) reload ends
                    int ticks = weapon.NextPrimaryAttackTick - reload.StartTick;
                    float normal = ticks > 0 ? ticks * Server.TickInterval : FallbackReloadSeconds;
                    reload.DoneAt = reload.StartedAt + (normal / Speed);
                    Debug($"{player.PlayerName} quick reload of {weapon.DesignerName}: normal {normal:0.00} s, now {normal / Speed:0.00} s");
                }
                if (now < reload.DoneAt)
                {
                    continue;
                }
                _ = _reloads.Remove(player);
                SetRate(weapon, reload.OriginalRate);
                if (Finish(player, weapon))
                {
                    _guards[player] = new Guard(weapon, now + GuardSeconds);
                }
            }

            foreach ((CCSPlayerController player, Guard guard) in _guards)
            {
                if (now > guard.Until || !guard.Weapon.IsValid)
                {
                    _ = _guards.Remove(player);
                    continue;
                }
                Watch(player, guard);
            }
        }

        /// <summary>
        /// Only the engine's late reload lowers the reserve while guarded (shots use the clip, pickups raise it): it is
        /// put back together with the clip it refilled, so the reload costs one magazine and no shot comes back.
        /// </summary>
        private void Watch(CCSPlayerController player, Guard guard)
        {
            CCSWeaponBase weapon = guard.Weapon;
            int reserve = weapon.ReserveAmmo[0];
            if (reserve >= guard.Reserve)
            {
                guard.Reserve = reserve;
                guard.Clip = weapon.Clip1;
                return;
            }

            Debug($"{player.PlayerName} quick reload: engine reload landed late (clip {weapon.Clip1}, reserve {reserve}),"
                + $" put back to clip {guard.Clip}, reserve {guard.Reserve}");
            weapon.Clip1 = guard.Clip;
            weapon.ReserveAmmo[0] = guard.Reserve;
            weapon.InReload = false;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_bInReload");
        }

        /// <summary>
        /// Does what the engine's reload does at its end. Guns whose reserve counts magazines (ReserveAmmoAsClips, the
        /// HUD shows a magazine icon) take one full magazine out of the reserve; the rest move single rounds. False when
        /// there was nothing to load.
        /// </summary>
        private bool Finish(CCSPlayerController player, CCSWeaponBase weapon)
        {
            CBasePlayerWeaponVData? data = weapon.VData;
            int max = data?.MaxClip1 ?? 0;
            int reserve = weapon.ReserveAmmo[0];
            bool asClips = data?.ReserveAmmoAsClips == true;
            if (max <= 0 || reserve <= 0 || weapon.Clip1 >= max)
            {
                return false;
            }

            int rounds = asClips ? max - weapon.Clip1 : Math.Min(max - weapon.Clip1, reserve);
            weapon.Clip1 += rounds;
            weapon.ReserveAmmo[0] = reserve - (asClips ? 1 : rounds);
            weapon.InReload = false;
            weapon.NextPrimaryAttackTick = Server.TickCount;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_bInReload");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Debug($"{player.PlayerName} quick reload: +{rounds}, clip {weapon.Clip1}, reserve {weapon.ReserveAmmo[0]}"
                + $" ({(asClips ? "magazines" : "rounds")}, keep loaded ammo {data?.KeepLoadedAmmo == true})");
            return true;
        }
    }
}

namespace RollTheDice.Configs
{
    public class QuickReloadConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // reloads this many times faster: the animation plays at this rate and the magazine is full when it ends
        // (2.5 = 40 % of each gun's normal reload time)
        [JsonPropertyName("reload_speed")] public float ReloadSpeed { get; set; } = 2.5f;
    }
}
