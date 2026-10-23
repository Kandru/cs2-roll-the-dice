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

        private readonly Dictionary<CCSPlayerController, (CCSWeaponBase Weapon, float DoneAt)> _reloads = [];

        public QuickReload(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _reloads.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _reloads.Clear();
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
            _reloads[player] = (active.As<CCSWeaponBase>(), Server.CurrentTime + _config.Dices.QuickReload.ReloadSeconds);
            return HookResult.Continue;
        }

        /// <summary>
        /// Finishes a running reload early: the magazine is filled from the reserve and the gun fires at once. A reload
        /// that was cancelled (weapon switch) is left alone.
        /// </summary>
        public void OnTick()
        {
            if (_reloads.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, (CCSWeaponBase weapon, float doneAt)) in _reloads)
            {
                if (now < doneAt)
                {
                    continue;
                }
                _ = _reloads.Remove(player);
                if (!weapon.IsValid
                    || !weapon.InReload
                    || player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Handle != weapon.Handle)
                {
                    continue;
                }
                Finish(player, weapon);
            }
        }

        private void Finish(CCSPlayerController player, CCSWeaponBase weapon)
        {
            int max = weapon.VData?.MaxClip1 ?? 0;
            int reserve = weapon.ReserveAmmo[0];
            int rounds = Math.Min(max - weapon.Clip1, reserve);
            if (rounds <= 0)
            {
                return;
            }

            weapon.Clip1 += rounds;
            weapon.ReserveAmmo[0] = reserve - rounds;
            weapon.InReload = false;
            weapon.NextPrimaryAttackTick = Server.TickCount;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            Utilities.SetStateChanged(weapon, "CCSWeaponBase", "m_bInReload");
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
            Debug($"{player.PlayerName} quick reload: +{rounds}, clip {weapon.Clip1}, reserve {weapon.ReserveAmmo[0]}");
        }
    }
}

namespace RollTheDice.Configs
{
    public class QuickReloadConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // a reload is done after this long (a normal one takes 2 - 3 s)
        [JsonPropertyName("reload_seconds")] public float ReloadSeconds { get; set; } = 0.9f;
    }
}
