using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playersWithChangedNames = new();
        private Dictionary<CCSPlayerController, string> _playersWithChangedNamesOldNames = new();

        private Dictionary<string, string> DiceChangeName(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceChangeName");
            // random names from users
            List<string> PlayerNames = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV)
                .Select(p => p.PlayerName)
                .ToList();
            // set random player name
            string randomName = "";
            // copy player names list
            List<string> PlayerNamesCopy = [.. PlayerNames];
            // remove own name from list
            PlayerNamesCopy.Remove(player.PlayerName);
            // check if we have at least X players on the server before we use player names instead of the predefined list
            if (PlayerNamesCopy.Count >= Convert.ToInt32(config["min_players_for_using_player_names"]))
                randomName = PlayerNamesCopy[_random.Next(PlayerNamesCopy.Count)];
            else
            {
                var namesList = ((List<object>)config["names"]).Cast<string>().ToList();
                randomName = namesList[_random.Next(namesList.Count)];
            }
            _playersWithChangedNames.Add(player);
            _playersWithChangedNamesOldNames[player] = player.PlayerName;
            player.PlayerName = randomName;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            return new Dictionary<string, string>
            {
                { "playerName", _playersWithChangedNamesOldNames[player] },
                { "randomName", randomName }
            };
        }

        private void DiceChangeNameUnload()
        {
            DiceChangeNameReset();
        }

        private void DiceChangeNameReset()
        {
            // iterate through all players
            List<CCSPlayerController> _playersWithChangedNamesCopy = new(_playersWithChangedNames);
            foreach (var player in _playersWithChangedNamesCopy)
            {
                try
                {
                    if (player == null
                        || player.PlayerPawn == null
                        || !player.PlayerPawn.IsValid
                        || player.PlayerPawn.Value == null
                        || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // reset player name
                    if (_playersWithChangedNamesOldNames.ContainsKey(player)) player.PlayerName = _playersWithChangedNamesOldNames[player];
                    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithChangedNames.Clear();
            _playersWithChangedNamesOldNames.Clear();
        }

        private void DiceChangeNameResetForPlayer(CCSPlayerController player)
        {
            if (!_playersWithChangedNames.Contains(player)) return;
            // reset player name
            player.PlayerName = _playersWithChangedNamesOldNames[player];
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            // remove player from list
            _playersWithChangedNames.Remove(player);
            _playersWithChangedNamesOldNames.Remove(player);
        }

        private Dictionary<string, object> DiceChangeNameConfig()
        {
            var config = new Dictionary<string, object>();
            config["names"] = new List<string>
                {
                    "Hans Wurst", "Fritz Frosch", "Klaus Kleber", "Otto Normalverbraucher", "Peter Lustig",
                    "Karl-Heinz Klammer", "Gustav Gans", "Heinz Erhardt", "Wolfgang Witzig", "Ludwig Lustig",
                    "Rudi Rüssel", "Siggi Sorglos", "Berti Bratwurst", "Dieter Dosenbier", "Erwin Einhorn",
                    "Franz Fuchs", "Günther Gans", "Horst Hering", "Ingo Igel", "Jürgen Jux",
                    "Kurt Ketchup", "Lars Lachs", "Manfred Möhre", "Norbert Nudel", "Olaf Oktopus",
                    "Paul Pinguin", "Quirin Qualle", "Ralf Rabe", "Stefan Seestern", "Thomas Tintenfisch",
                    "Uwe Uhu", "Volker Vogel", "Willi Wurm", "Xaver Xylophon", "Yannik Yak",
                    "Zacharias Zebra", "Albert Apfel", "Bernd Banane", "Claus Clown", "Detlef Dachs",
                    "Egon Eule", "Ferdinand Frosch", "Gerd Giraffe", "Helmut Hase", "Igor Igel",
                    "Jochen Jaguar", "Knut Känguru", "Lothar Löwe", "Martin Marder", "Norbert Nashorn",
                    "Egon Kowalski", "Fritz Fink", "Heinz Hering"
                };
            config["min_players_for_using_player_names"] = (int)4;
            return config;
        }
    }
}
