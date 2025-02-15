using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, Dictionary<string, string>> _playersAsChicken = new();
        private readonly string _playersAsChickenModel = "models/chicken/chicken.vmdl";
        private readonly List<string> _chickenSounds = new List<string>
        {
            "Chicken.Idle",
            "Chicken.Panic",
        };

        private Dictionary<string, string> DicePlayerAsChicken(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersAsChicken.Count() == 0)
            {
                RegisterEventHandler<EventPlayerDeath>(EventDicePlayerAsChickenOnPlayerDeath);
                RegisterListener<Listeners.OnTick>(EventDicePlayerAsChickenOnTick);
                RegisterListener<Listeners.CheckTransmit>(EventDicePlayerAsChickenCheckTransmit);
            }
            // add player to list
            _playersAsChicken.Add(player, new Dictionary<string, string>());
            _playersAsChicken[player]["old_model"] = GetPlayerModel(playerPawn);
            _playersAsChicken[player]["next_sound"] = $"{(int)Server.CurrentTime + 2}";
            _playersAsChicken[player]["prop"] = SpawnProp(player, _playersAsChickenModel, 5.2f).ToString();
            MakePlayerInvisible(player);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerAsChickenUnload()
        {
            DicePlayerAsChickenReset();
        }

        private void DicePlayerAsChickenReset()
        {
            DeregisterEventHandler<EventPlayerDeath>(EventDicePlayerAsChickenOnPlayerDeath);
            RemoveListener<Listeners.OnTick>(EventDicePlayerAsChickenOnTick);
            RemoveListener<Listeners.CheckTransmit>(EventDicePlayerAsChickenCheckTransmit);
            // iterate through all players
            Dictionary<CCSPlayerController, Dictionary<string, string>> _playersAsChickenCopy = new(_playersAsChicken);
            foreach (CCSPlayerController player in _playersAsChickenCopy.Keys)
            {
                try
                {
                    if (player == null
                        || player.Pawn == null
                        || player.Pawn.Value == null) continue;
                    RemoveProp(int.Parse(_playersAsChicken[player]["prop"]));
                    MakePlayerVisible(player);
                }
                catch
                {
                    // do nothing
                }
            }
            _playersAsChicken.Clear();
        }

        private void DicePlayerAsChickenResetForPlayer(CCSPlayerController player)
        {
            if (!_playersAsChicken.ContainsKey(player)) return;
            // get prop
            int prop = int.Parse(_playersAsChicken[player]["prop"]);
            // remove player first to avoid infinite loop
            _playersAsChicken.Remove(player);
            // remove prop
            RemoveProp(prop);
            MakePlayerVisible(player);
        }

        private void EventDicePlayerAsChickenOnTick()
        {
            // remove listener if no players to save resources
            if (_playersAsChicken.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, Dictionary<string, string>> _playersAsChickenCopy = new(_playersAsChicken);
            foreach (var (player, playerData) in _playersAsChickenCopy)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.Pawn == null
                    || player.Pawn.Value == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                    || !playerData.ContainsKey("prop")) continue;
                    // update prop every tick to ensure synchroneity
                    UpdateProp(
                        player,
                        int.Parse(playerData["prop"]),
                        (player.Buttons & PlayerButtons.Duck) != 0 ? -18 : 0
                    );
                    // make sound if time
                    if (int.Parse(_playersAsChickenCopy[player]["next_sound"]) <= (int)Server.CurrentTime)
                    {
                        EmitSound(player, _chickenSounds[_random.Next(_chickenSounds.Count)]);
                        _playersAsChickenCopy[player]["next_sound"] = $"{(int)Server.CurrentTime + _random.Next(2, 5)}";
                    }
                }
                catch (Exception e)
                {
                    // remove player
                    _playersAsChicken.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }

        private void EventDicePlayerAsChickenCheckTransmit(CCheckTransmitInfoList infoList)
        {
            // remove listener if no players to save resources
            if (_playersAsChicken.Count() == 0) return;
            // worker
            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                if (player == null) continue;
                if (!_playersAsChicken.ContainsKey(player)) continue;
                var prop = Utilities.GetEntityFromIndex<CDynamicProp>(int.Parse(_playersAsChicken[player]["prop"]));
                if (prop == null) continue;
                info.TransmitEntities.Remove(prop);
            }
        }

        private HookResult EventDicePlayerAsChickenOnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            if (_playersAsChicken.ContainsKey(player))
            {
                RemoveProp(int.Parse(_playersAsChicken[player]["prop"]));
                MakePlayerVisible(player);
                _playersAsChicken.Remove(player);
            }
            return HookResult.Continue;
        }
    }
}
