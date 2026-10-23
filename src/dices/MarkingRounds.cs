using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class MarkingRounds : DiceBlueprint
    {
        public override string ClassName => "MarkingRounds";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        public override List<string> Listeners { get; } = ["OnTick"];

        private sealed class Mark(CDynamicProp? proxy, CDynamicProp? glow, float until)
        {
            public readonly CDynamicProp? Proxy = proxy;
            public readonly CDynamicProp? Glow = glow;
            public float Until = until;
        }

        // victim and the team that sees the mark (free-for-all: every holder's team marks on its own)
        private readonly Dictionary<(CCSPlayerController Victim, byte Team), Mark> _marks = [];

        public MarkingRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Reset()
        {
            base.Reset();
            foreach (Mark mark in _marks.Values)
            {
                Glow.RemoveGlow(mark.Proxy, mark.Glow);
            }
            _marks.Clear();
        }

        /// <summary>A bullet hit of the holder marks the enemy: they glow through walls for the holder's team.</summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || Damage.IsDealing
                || attacker is not { IsValid: true }
                || !_players.Contains(attacker)
                || victim is not { IsValid: true }
                || !Targets.IsEnemy(attacker, victim)
                || !Weapons.IsGun(@event.Weapon)
                || !Targets.IsAlive(victim)
                || victim.PlayerPawn.Value is not { IsValid: true } victimPawn)
            {
                return HookResult.Continue;
            }

            float until = Server.CurrentTime + _config.Dices.MarkingRounds.Seconds;
            (CCSPlayerController, byte) key = (victim, attacker.TeamNum);
            if (_marks.TryGetValue(key, out Mark? mark))
            {
                mark.Until = until;
                return HookResult.Continue;
            }

            (CDynamicProp? proxy, CDynamicProp? glow) = Glow.CreateGlow(victimPawn, Color.FromArgb(255, 255, 140, 0));
            if (glow != null)
            {
                // only the shooter's team sees the outline
                glow.Glow.GlowTeam = attacker.TeamNum;
            }
            _marks[key] = new Mark(proxy, glow, until);
            Debug($"{attacker.PlayerName} marked {victim.PlayerName}");
            return HookResult.Continue;
        }

        /// <summary>Ends marks that ran out or whose player died.</summary>
        public void OnTick()
        {
            if (_marks.Count == 0 || Server.TickCount % 8 != 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach (((CCSPlayerController victim, byte team), Mark mark) in _marks)
            {
                if (now >= mark.Until || !Targets.IsAlive(victim))
                {
                    Glow.RemoveGlow(mark.Proxy, mark.Glow);
                    _ = _marks.Remove((victim, team));
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class MarkingRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // how long a hit enemy stays marked (a new hit starts it again)
        [JsonPropertyName("seconds")] public float Seconds { get; set; } = 5f;
    }
}
