using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class Cutter : DiceBlueprint
    {
        public override string ClassName => "Cutter";
        public override List<string> Events => [
            "EventPlayerHurt"
        ];
        private readonly HashSet<string> _knifes = new(StringComparer.OrdinalIgnoreCase)
        {
            "knife",
            CsItem.Knife.ToString(),
            CsItem.KnifeT.ToString(),
            CsItem.KnifeCT.ToString(),
            CsItem.DefaultKnifeT.ToString(),
            CsItem.DefaultKnifeCT.ToString()
        };

        public Cutter(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            if (_knifes.Any(item => @event.Weapon.Contains(item, StringComparison.OrdinalIgnoreCase)))
            {
                player.Pawn.Value.Health -= 9999;
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            }
            return HookResult.Continue;
        }
    }
}
