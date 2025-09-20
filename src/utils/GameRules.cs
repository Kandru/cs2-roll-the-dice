using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    public static class GameRules
    {
        public static CCSGameRules? _gameRules;
        private static IEnumerable<CCSTeam>? _teamManager;

        private static CCSGameRules? GetGameRule(bool forceRefresh = false)
        {
            if (_gameRules == null || forceRefresh)
            {
                _gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                    .FirstOrDefault(static e => e != null && e.IsValid)?.GameRules;
            }
            return _gameRules;
        }

        public static void Refresh() => _ = GetGameRule(true);

        public static object? Get(string rule, bool forceRefresh = false)
        {
            _ = GetGameRule();
            System.Reflection.PropertyInfo? property = _gameRules?.GetType().GetProperty(rule);
            return property?.CanRead == true ? property.GetValue(_gameRules) : null;
        }

        public static void SetRoundTime(float minutes)
        {
            _ = GetGameRule();
            if (_gameRules != null)
            {
                _gameRules.RoundTime = (int)Math.Round(minutes * 60);
            }
        }

        public static void TerminateRound(RoundEndReason reason, float delay = 0f)
        {
            _ = GetGameRule();
            if (_gameRules != null)
            {
                _gameRules.RoundsPlayedThisPhase = 1;
                _gameRules.ITotalRoundsPlayed = 1;
                _gameRules.TotalRoundsPlayed = 1;
                _gameRules.TerminateRound(delay, reason);
            }
        }

        public static void SetTeamScore(int score, CsTeam team)
        {
            if (_teamManager == null)
            {
                _teamManager = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
                if (_teamManager == null)
                {
                    return;
                }
            }

            foreach (CCSTeam entry in _teamManager)
            {
                if (entry.TeamNum == (byte)team)
                {
                    entry.Score = score;
                    Utilities.SetStateChanged(entry, "CTeam", "m_iScore");
                    break;
                }
            }
        }
    }
}