using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Hud;

namespace RollTheDice
{
    /// <summary>
    /// Mirror mode (switch in the admin tab, config <c>admin.mirror_teams</c>): both teams get the same perks each round.
    /// A random perk rolled by one team is owed to the other team, whose next roll takes the oldest owed perk instead
    /// of a random one. Two rolls in a row on one side leave two perks owed, so the order does not matter; what a
    /// smaller team cannot take stays owed until the round ends. Admin hand-outs are not mirrored.
    /// </summary>
    public partial class RollTheDice
    {
        private readonly Queue<string> _owedToT = new();
        private readonly Queue<string> _owedToCt = new();

        private void ClearMirror()
        {
            _owedToT.Clear();
            _owedToCt.Clear();
        }

        /// <summary>The dice of a roll without a chosen dice: an owed one in mirror mode, else a random one.</summary>
        private DiceBlueprint? PickRolledDice(CCSPlayerController player, ReelSource source)
        {
            (Queue<string> own, Queue<string> other)? queues = player.Team switch
            {
                CsTeam.Terrorist => (_owedToT, _owedToCt),
                CsTeam.CounterTerrorist => (_owedToCt, _owedToT),
                _ => null,
            };
            if (!Config.Admin.MirrorTeams || source == ReelSource.Admin || queues is not { } q)
            {
                return GetWeightedRandomDice();
            }

            while (q.own.TryDequeue(out string? className))
            {
                // an owed perk switched off in the admin tab since is dropped
                if (_dices.Find(d => d.ClassName == className) is { } owed && IsRollable(owed))
                {
                    DebugMirror($"{player.PlayerName} ({player.Team}) gets the owed {className}");
                    return owed;
                }
            }

            DiceBlueprint? dice = GetWeightedRandomDice();
            if (dice != null)
            {
                q.other.Enqueue(dice.ClassName);
                DebugMirror($"{player.PlayerName} ({player.Team}) rolls {dice.ClassName}, now owed to the other team ({q.other.Count})");
            }
            return dice;
        }

        private void DebugMirror(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine($"[RollTheDice][Mirror] {message}");
            }
        }
    }
}
