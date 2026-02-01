using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class WeirdGrenades : DiceBlueprint
    {
        public override string ClassName => "WeirdGrenades";
        public readonly Random _random = new();
        public override List<string> Listeners => [
            "OnEntitySpawned",
        ];
        public override List<string> Events => [
            "EventSmokegrenadeDetonate",
            "EventPlayerBlind",
        ];

        public WeirdGrenades(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }


        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_players.Count == 0)
            {
                return;
            }
            switch (entity.DesignerName)
            {
                case "flashbang_projectile":
                case "smokegrenade_projectile":
                case "hegrenade_projectile":
                case "decoy_projectile":
                    CBaseCSGrenadeProjectile grenade = entity.As<CBaseCSGrenadeProjectile>();
                    CCSPlayerPawn? owner = grenade.OriginalThrower?.Value;
                    if (owner == null
                        || !owner.IsValid)
                    {
                        return;
                    }
                    if (owner.Controller?.Value == null
                        || !_players.Contains(owner.Controller.Value))
                    {
                        return;
                    }
                    Server.NextFrame(() =>
                    {
                        float min = _config.Dices.WeirdGrenades.MinDetonateTime;
                        float max = _config.Dices.WeirdGrenades.MaxDetonateTime;
                        float detonateOffset = (float)((_random.NextDouble() * (max - min)) + min);
                        grenade.DetonateTime = Server.CurrentTime + detonateOffset;
                        Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
                    });
                    break;
                case "molotov_projectile":
                    break;
                default:
                    break;
            }
        }

        public HookResult EventSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player?.IsValid != true
                || !_players.Contains(player))
            {
                return HookResult.Continue;
            }
            CSmokeGrenadeProjectile? grenade = Utilities.GetEntityFromIndex<CSmokeGrenadeProjectile>(@event.Entityid);
            if (grenade == null
            || !grenade.IsValid)
            {
                return HookResult.Continue;
            }
            int min = _config.Dices.WeirdGrenades.MinSmokeduration;
            int max = _config.Dices.WeirdGrenades.MaxSmokeduration;
            int smokeDuration = _random.Next(min, max + 1);
            grenade.SmokeEffectTickBegin = Server.TickCount - ((19 - smokeDuration) * 64);
            Utilities.SetStateChanged(grenade, "CSmokeGrenadeProjectile", "m_nSmokeEffectTickBegin");
            return HookResult.Continue;
        }

        public HookResult EventPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (player?.IsValid != true
                || attacker?.IsValid != true
                || !_players.Contains(attacker)
                || player.PlayerPawn.Value == null)
            {
                return HookResult.Continue;
            }
            float min = _config.Dices.WeirdGrenades.MinBlindduration;
            float max = _config.Dices.WeirdGrenades.MaxBlindduration;
            float blindDuration = (float)((_random.NextDouble() * (max - min)) + min);
            @event.BlindDuration = blindDuration;
            player.PlayerPawn.Value.FlashDuration = blindDuration;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawnBase", "m_flFlashDuration");
            player.PlayerPawn.Value.BlindUntilTime = Server.CurrentTime + player.PlayerPawn.Value.FlashDuration;
            return HookResult.Continue;
        }
    }
}
