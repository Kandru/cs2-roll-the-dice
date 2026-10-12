using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class FastMapAction : DiceBlueprint
    {
        public override string ClassName => "FastMapAction";
        public override List<string> Events { get; } = ["EventBombBegindefuse", "EventBombBeginplant"];

        private readonly HashSet<CCSPlayerController> _instantDefuse = [];
        private readonly HashSet<CCSPlayerController> _instantPlant = [];
        private readonly HashSet<CCSPlayerPawn> _instantHostage = [];

        public FastMapAction(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player) || player.PlayerPawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }

            string locale;
            if (Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_c4").Any()
                || Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("planted_c4").Any())
            {
                if (pawn.TeamNum == (int)CsTeam.Terrorist)
                {
                    _ = _instantPlant.Add(player);
                    locale = "FastBombActionT";
                }
                else if (pawn.TeamNum == (int)CsTeam.CounterTerrorist)
                {
                    _ = _instantDefuse.Add(player);
                    locale = "FastBombActionCT";
                }
                else
                {
                    return;
                }
            }
            else if (pawn.TeamNum == (int)CsTeam.CounterTerrorist
                && Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hostage_entity").Any())
            {
                _ = _instantHostage.Add(pawn);
                locale = "FastHostageActionCT";
            }
            else
            {
                locale = "FastHostageActionUnlucky";
            }

            _players.Add(player);
            NotifyPlayers(player, locale, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _instantDefuse.Remove(player);
            _ = _instantPlant.Remove(player);
            if (player.PlayerPawn?.Value is { IsValid: true } pawn)
            {
                _ = _instantHostage.Remove(pawn);
            }
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            _instantDefuse.Clear();
            _instantPlant.Clear();
            _instantHostage.Clear();
            base.Reset();
        }

        public HookResult EventBombBegindefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !_instantDefuse.Contains(player))
            {
                return HookResult.Continue;
            }

            CPlantedC4? c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
            Server.NextFrame(() =>
            {
                if (c4 is { IsValid: true })
                {
                    c4.DefuseCountDown = Server.CurrentTime;
                }
            });
            return HookResult.Continue;
        }

        public HookResult EventBombBeginplant(EventBombBeginplant @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player?.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value is not { IsValid: true } weapon
                || !_instantPlant.Contains(player)
                || !weapon.DesignerName.Contains("c4"))
            {
                return HookResult.Continue;
            }

            CC4 c4 = new(weapon.Handle);
            Server.NextFrame(() =>
            {
                if (c4.IsValid)
                {
                    c4.ArmedTime = Server.CurrentTime;
                }
            });
            return HookResult.Continue;
        }

        public bool TryInstantHostageGrab(CEntityInstance activator, CEntityInstance caller)
        {
            if (activator.DesignerName != "hostage_entity" || caller.DesignerName != "player")
            {
                return false;
            }

            CCSPlayerPawn pawn = new(caller.Handle);
            if (!pawn.IsValid || !_instantHostage.Contains(pawn))
            {
                return false;
            }

            Schema.SetSchemaValue(activator.Handle, "CHostage", "m_flGrabSuccessTime", Server.CurrentTime);
            return true;
        }
    }
}

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        [EntityOutputHook("*", "OnHostageBeginGrab")]
        public HookResult OnHostageBeginGrab(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {
            Dices.FastMapAction? dice = _dices.OfType<Dices.FastMapAction>().FirstOrDefault();
            _ = dice?.TryInstantHostageGrab(activator, caller);
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class FastMapActionConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
