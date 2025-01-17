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
            // random names from users
            List<string> PlayerNames = Utilities.GetPlayers()
                .Where(p => !p.IsBot)
                .Select(p => p.PlayerName)
                .ToList();
            // random names from list
            var RandomNames = new List<string>
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
            // set random player name
            string randomName = "";
            // copy player names list
            List<string> PlayerNamesCopy = new List<string>(PlayerNames);
            // remove own name from list
            PlayerNamesCopy.Remove(player.PlayerName);
            // check if we have more then 4 players on the server
            if (PlayerNames.Count > 3)
                randomName = PlayerNamesCopy[_random.Next(PlayerNamesCopy.Count)];
            else
                randomName = RandomNames[_random.Next(RandomNames.Count)];
            _playersWithChangedNames.Add(player);
            _playersWithChangedNamesOldNames[player] = player.PlayerName;
            player.PlayerName = randomName;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            return new Dictionary<string, string>
            {
                {"_translation_player", "DiceChangeNamePlayer"},
                {"_translation_other", "DiceChangeName"},
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
            foreach (var player in _playersWithChangedNames)
            {
                if (player == null || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || player.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                // reset player name
                player.PlayerName = _playersWithChangedNamesOldNames[player];
            }
            _playersWithChangedNames.Clear();
            _playersWithChangedNamesOldNames.Clear();
        }
    }
}
