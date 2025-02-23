using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, Dictionary<string, string>> _playersDisguisedAsPlants = new();
        private readonly Dictionary<string, Dictionary<string, object>> _playersDisguisedAsPlantsModels = new()
        {
            { "Office/Plant", new Dictionary<string, object> { { "model", "models/props/cs_office/plant01.vmdl" } } },
            { "Trafficcone", new Dictionary<string, object> { { "model", "models/props/de_vertigo/trafficcone_clean.vmdl" }, { "offset_z", "15" } } },
            { "Barstool", new Dictionary<string, object> { { "model", "models/generic/barstool_01/barstool_01.vmdl" } } },
            { "Fireextinguisher", new Dictionary<string, object> { { "model", "models/generic/fire_extinguisher_01/fire_extinguisher_01.vmdl" } } },
            { "Hostage", new Dictionary<string, object> { { "model", "models/hostage/hostage.vmdl" } } },
            { "Pottery", new Dictionary<string, object> { { "model", "models/ar_shoots/shoots_pottery_02.vmdl" } } },
            { "AnubisInfoPanel", new Dictionary<string, object> { { "model", "models/anubis/signs/anubis_info_panel_01.vmdl" } } },
            { "Chicken", new Dictionary<string, object> { { "model", "models/chicken/chicken.vmdl" } } },
            { "Italy/Chair", new Dictionary<string, object> { { "model", "models/cs_italy/seating/chair/wood_chair_1.vmdl" }, { "offset_angle", "180" } } },
            { "FileCabinet", new Dictionary<string, object> { { "model", "models/props_office/file_cabinet_03.vmdl" } } },
            { "Airport/Plant", new Dictionary<string, object> { { "model", "models/props_plants/plantairport01.vmdl" } } },
            { "MailDropbox", new Dictionary<string, object> { { "model", "models/props_street/mail_dropbox.vmdl" } } },
        };
        private Dictionary<string, string> DicePlayerDisguiseAsPlant(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersDisguisedAsPlants.Count == 0)
            {
                RegisterEventHandler<EventPlayerDeath>(EventDicePlayerDisguiseAsPlantOnPlayerDeath);
                RegisterListener<Listeners.OnTick>(EventDicePlayerDisguiseAsPlantOnTick);
            }
            // add player to list
            _playersDisguisedAsPlants.Add(player, new Dictionary<string, string>());
            _playersDisguisedAsPlants[player]["status"] = "player";
            _playersDisguisedAsPlants[player]["use_button_down"] = "false";
            _playersDisguisedAsPlants[player]["last_model_change"] = ((int)Server.CurrentTime).ToString();
            var randomKey = _playersDisguisedAsPlantsModels.Keys.ElementAt(_random.Next(0, _playersDisguisedAsPlantsModels.Count));
            _playersDisguisedAsPlants[player]["prop_name"] = randomKey;
            _playersDisguisedAsPlants[player]["prop"] = SpawnProp(
                player,
                _playersDisguisedAsPlantsModels[randomKey]["model"].ToString()!
            ).ToString();
            _playersDisguisedAsPlants[player]["offset_z"] = _playersDisguisedAsPlantsModels[randomKey].ContainsKey("offset_z") ? (string)_playersDisguisedAsPlantsModels[randomKey]["offset_z"] : "0";
            _playersDisguisedAsPlants[player]["offset_angle"] = _playersDisguisedAsPlantsModels[randomKey].ContainsKey("offset_angle") ? (string)_playersDisguisedAsPlantsModels[randomKey]["offset_angle"] : "0";
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "model", randomKey }
            };
        }

        private void DicePlayerDisguiseAsPlantUnload()
        {
            DicePlayerDisguiseAsPlantReset();
        }

        private void DicePlayerDisguiseAsPlantReset()
        {
            DeregisterEventHandler<EventPlayerDeath>(EventDicePlayerDisguiseAsPlantOnPlayerDeath);
            RemoveListener<Listeners.OnTick>(EventDicePlayerDisguiseAsPlantOnTick);
            // iterate through all players
            Dictionary<CCSPlayerController, Dictionary<string, string>> _playersDisguisedAsPlantsCopy = new(_playersDisguisedAsPlants);
            foreach (CCSPlayerController player in _playersDisguisedAsPlantsCopy.Keys)
            {
                try
                {
                    if (player == null
                        || player.Pawn == null
                        || player.Pawn.Value == null) continue;
                    RemoveProp(int.Parse(_playersDisguisedAsPlantsCopy[player]["prop"]));
                    MakePlayerVisible(player);
                }
                catch
                {
                    // do nothing
                }
            }
            _playersDisguisedAsPlants.Clear();
        }

        private void DicePlayerDisguiseAsPlantResetForPlayer(CCSPlayerController player)
        {
            if (!_playersDisguisedAsPlants.ContainsKey(player)) return;
            // get prop
            int prop = int.Parse(_playersDisguisedAsPlants[player]["prop"]);
            // remove player first to avoid infinite loop
            _playersDisguisedAsPlants.Remove(player);
            // remove prop
            RemoveProp(prop);
            MakePlayerVisible(player);
            // remove event listener when no players have this dice
            if (_playersDisguisedAsPlants.Count == 0) DicePlayerDisguiseAsPlantReset();
        }

        private void EventDicePlayerDisguiseAsPlantOnTick()
        {
            if (_playersDisguisedAsPlants.Count == 0) return;
            // worker
            Dictionary<CCSPlayerController, Dictionary<string, string>> _playersDisguisedAsPlantsCopy = new(_playersDisguisedAsPlants);
            foreach (var (player, playerData) in _playersDisguisedAsPlantsCopy)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.Pawn == null
                    || player.Pawn.Value == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                    || !_playersDisguisedAsPlants.ContainsKey(player)) continue;
                    Dictionary<string, object> config = GetDiceConfig("DicePlayerDisguiseAsPlant");
                    // change player model if player is not pressing any buttons
                    if (player.Buttons == 0 && playerData["status"] == "player")
                    {
                        _playersDisguisedAsPlants[player]["status"] = "plant";
                        MakePlayerInvisible(player);
                        UpdateProp(
                            player,
                            int.Parse(playerData["prop"]),
                            int.Parse(playerData["offset_z"]),
                            int.Parse(playerData["offset_angle"])
                        );
                        // update gui if available
                        if (_playersThatRolledTheDice.ContainsKey(player)
                            && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                            && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
                        {
                            CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                            ChangeColor(worldText, Config.GUIPositions[Config.GUIPosition].StatusColorEnabled);
                            string message = playerData["prop_name"];
                            if ((bool)config["allow_model_change"])
                                message = Localizer["DicePlayerDisguiseAsPlant_status"].Value.Replace("{model}", playerData["prop_name"]);
                            worldText.AcceptInput("SetMessage", worldText, worldText, message);
                        }
                    }
                    else if ((bool)config["allow_model_change"]
                        && player.Buttons != 0
                        && player.Buttons == PlayerButtons.Use
                        && playerData["status"] == "plant"
                        && playerData["use_button_down"] == "false")
                    {
                        // disable button for a short time
                        if (Convert.ToInt32(playerData["last_model_change"]) > (int)Server.CurrentTime) continue;
                        _playersDisguisedAsPlants[player]["use_button_down"] = "true";
                        string currentProp = playerData["prop_name"];
                        // get next prop of list _playersDisguisedAsPlantsModels - start at beginning if end is reached
                        string nextProp = _playersDisguisedAsPlantsModels.Keys.ElementAt(
                            (_playersDisguisedAsPlantsModels.Keys.ToList().IndexOf(currentProp) + 1) % _playersDisguisedAsPlantsModels.Count
                        );
                        // update prop
                        CDynamicProp? prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)int.Parse(playerData["prop"]));
                        if (prop != null
                            && prop.IsValid)
                        {
                            // update model
                            prop.SetModel(_playersDisguisedAsPlantsModels[nextProp]["model"].ToString()!);
                            // update model string
                            _playersDisguisedAsPlants[player]["prop_name"] = nextProp;
                            // update model offsets
                            _playersDisguisedAsPlants[player]["offset_z"] = _playersDisguisedAsPlantsModels[nextProp].ContainsKey("offset_z") ? (string)_playersDisguisedAsPlantsModels[nextProp]["offset_z"] : "0";
                            _playersDisguisedAsPlants[player]["offset_angle"] = _playersDisguisedAsPlantsModels[nextProp].ContainsKey("offset_angle") ? (string)_playersDisguisedAsPlantsModels[nextProp]["offset_angle"] : "0";
                            // inform other players
                            SendGlobalChatMessage(Localizer["DicePlayerDisguiseAsPlant_other"].Value
                            .Replace("{playerName}", player.PlayerName)
                            .Replace("{model}", nextProp),
                            player: player);
                            // update status gui
                            if (_playersThatRolledTheDice.ContainsKey(player)
                                && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                                && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
                            {
                                CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                                worldText.AcceptInput("SetMessage", worldText, worldText, Localizer["DicePlayerDisguiseAsPlant_status"].Value.Replace(
                                    "{model}", nextProp
                                ));
                            }
                        }
                    }
                    else if ((bool)config["allow_model_change"]
                        && (player.Buttons == 0
                        || player.Buttons != PlayerButtons.Use)
                        && playerData["use_button_down"] == "true")
                    {
                        // reset button state
                        _playersDisguisedAsPlants[player]["use_button_down"] = "false";
                        _playersDisguisedAsPlants[player]["last_model_change"] = ((int)Server.CurrentTime + 1).ToString();
                        // update model state
                        UpdateProp(
                            player,
                            int.Parse(playerData["prop"]),
                            int.Parse(playerData["offset_z"]),
                            int.Parse(playerData["offset_angle"])
                        );
                    }
                    else if (player.Buttons != 0 && playerData["use_button_down"] == "false" && playerData["status"] == "plant")
                    {
                        _playersDisguisedAsPlants[player]["status"] = "player";
                        MakePlayerVisible(player);
                        RemoveProp(int.Parse(playerData["prop"]), true);
                        // update gui if available
                        if (_playersThatRolledTheDice.ContainsKey(player)
                            && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                            && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
                        {
                            CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                            ChangeColor(worldText, Config.GUIPositions[Config.GUIPosition].StatusColorDisabled);
                            worldText.AcceptInput("SetMessage", worldText, worldText, "Disabled");
                        }
                    }
                    else if (playerData["status"] == "plant")
                    {
                        UpdateProp(
                            player,
                            int.Parse(playerData["prop"]),
                            int.Parse(playerData["offset_z"]),
                            int.Parse(playerData["offset_angle"])
                        );
                    }
                }
                catch (Exception e)
                {
                    // remove player
                    _playersDisguisedAsPlants.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }

        private HookResult EventDicePlayerDisguiseAsPlantOnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            if (_playersDisguisedAsPlants.ContainsKey(player))
            {
                RemoveProp(int.Parse(_playersDisguisedAsPlants[player]["prop"]));
                MakePlayerVisible(player);
                _playersDisguisedAsPlants.Remove(player);
            }
            return HookResult.Continue;
        }

        private Dictionary<string, object> DicePlayerDisguiseAsPlantConfig()
        {
            var config = new Dictionary<string, object>();
            config["allow_model_change"] = (bool)true;
            return config;
        }
    }
}
