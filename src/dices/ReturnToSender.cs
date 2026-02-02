using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ReturnToSender : DiceBlueprint
    {
        public override string ClassName => "ReturnToSender";
        public override List<string> Events => [
            "EventPlayerHurt"
        ];
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        public CBaseEntity[] playerSpawnEntities = [];
        public CBaseEntity[] ctSpawnEntities = [];
        public CBaseEntity[] tSpawnEntities = [];

        public ReturnToSender(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }


        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (player?.IsValid != true
                || attacker?.IsValid != true
                || !_players.Contains(attacker)
                || player.Pawn.Value == null)
            {
                return HookResult.Continue;
            }
            // update spawn entities if not done already
            GetSpawnEntities();
            // get spawn for player and teleport them there
            if (player.Team is CsTeam.CounterTerrorist or CsTeam.Terrorist)
            {
                CBaseEntity[] teamSpawns = player.Team == CsTeam.CounterTerrorist ? ctSpawnEntities : tSpawnEntities;
                List<CBaseEntity> allSpawns = [.. teamSpawns.Concat(playerSpawnEntities).OrderBy(_ => _random.Next())];
                CBaseEntity? spawn = allSpawns.FirstOrDefault(s => !IsPlayerNearby(s.AbsOrigin!));
                if (spawn != null)
                {
                    player.Pawn.Value.Teleport(spawn.AbsOrigin);
                    NotifyStatus(player, ClassName, []);
                }
            }
            return HookResult.Continue;
        }

        private void GetSpawnEntities()
        {
            if (playerSpawnEntities.Length > 0
                || ctSpawnEntities.Length > 0
                || tSpawnEntities.Length > 0)
            {
                return;
            }
            playerSpawnEntities = [.. Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_start")];
            ctSpawnEntities = [.. Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist")];
            tSpawnEntities = [.. Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist")];
        }

        private bool IsPlayerNearby(Vector position)
        {
            foreach (CCSPlayerController entry in Utilities.GetPlayers()
                .Where(static p => p.Pawn?.Value != null && p.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                if (Vectors.GetDistance(position, entry.Pawn!.Value!.AbsOrigin!) < 100f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
