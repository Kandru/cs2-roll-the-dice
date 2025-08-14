using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Roll The Dice";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private string _currentMap = "";
        private readonly Dictionary<CCSPlayerController, string> _playersThatRolledTheDice = [];
        private readonly Dictionary<CCSPlayerController, int> _PlayerCooldown = [];
        private readonly List<DiceBlueprint> _dices = [];
        private bool _isDuringRound;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public override void Load(bool hotReload)
        {
            // update configuration
            ReloadConfigFromDisk();
            // register listeners
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            // print message if hot reload
            if (hotReload)
            {
                // set current map
                _currentMap = Server.MapName;
                // initialize configuration
                LoadMapConfig(_currentMap);
                Console.WriteLine(Localizer["core.hotreload"]);
                _isDuringRound = true;
            }
            // initialize dice modules
            InitializeModules();
        }

        public override void Unload(bool hotReload)
        {
            // reset dice rolls on unload
            DestroyModules();
            // update configuration
            ReloadConfigFromDisk();
            // unregister listeners
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            Console.WriteLine(Localizer["core.unload"]);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // reset players that rolled the dice
            _playersThatRolledTheDice.Clear();
            // reset dices (necessary after warmup)
            RemoveDicesForPlayers();
            // abort if warmup
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && warmupPeriod)
            {
                return HookResult.Continue;
            }
            // announce round start
            Server.PrintToChatAll(Localizer["core.announcement"]);
            // allow dice rolls
            _isDuringRound = true;
            // check if random dice should be rolled
            foreach (CCSPlayerController entry in Utilities.GetPlayers()
                .Where(p => p.IsValid
                    && !p.IsHLTV
                    && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                // rtd for everyone if enabled or for specific players if they have it enabled
                if (Config.RollTheDiceOnRoundStart
                    || (_playerConfigs.ContainsKey(entry.SteamID)
                        && _playerConfigs[entry.SteamID].RtdOnSpawn))
                {
                    _ = AddTimer(1f, () =>
                    {
                        if (entry == null
                            || !entry.IsValid
                            || _playersThatRolledTheDice.ContainsKey(entry))
                        {
                            return;
                        }
                        // roll the dice
                        (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(entry);
                        // check if rolledDice is null or empty
                        if (rolledDice is null or "")
                        {
                            return;
                        }
                        // add player to the list of players that rolled the dice
                        _playersThatRolledTheDice[entry] = diceDescription ?? rolledDice;
                    });
                }
            }
            // check if random dice should be rolled every X seconds
            // TODO: re-implement rolling every X seconds
            //if (Config.RollTheDiceEveryXSeconds > 0)
            //{
            //RollTheDiceEveryXSeconds(Config.RollTheDiceEveryXSeconds);
            //}
            // continue event
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            RemoveDicesForPlayers();
            // disallow dice rolls
            _isDuringRound = false;
            // reduct cooldown if applicable
            if (Config.CooldownRounds > 0)
            {
                foreach (KeyValuePair<CCSPlayerController, int> kvp in _PlayerCooldown)
                {
                    // remove one round per player
                    if (_PlayerCooldown[kvp.Key] > 0)
                    {
                        _PlayerCooldown[kvp.Key] -= 1;
                    }
                }
            }
            // continue event
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            RemoveDiceForPlayer(@event.Userid);
            return HookResult.Continue;
        }

        private void OnMapStart(string mapName)
        {
            // update configuration
            ReloadConfigFromDisk();
            // load map config
            LoadMapConfig(mapName);
            // initialize dice modules
            InitializeModules();
            // set current map
            _currentMap = mapName;
        }

        private void OnMapEnd()
        {
            DestroyModules();
            // reset states
            _isDuringRound = false;
            _playersThatRolledTheDice.Clear();
            _PlayerCooldown.Clear();
        }

        private (string?, string?) RollTheDiceForPlayer(CCSPlayerController? player, string? diceName = null)
        {
            if (player == null || !player.IsValid)
            {
                return (null, null);
            }
            // remove old dice first
            RemoveDiceForPlayer(player);
            // roll the dice for the player - get random dice and add player
            if (_dices.Count > 0)
            {
                if (diceName != null)
                {
                    // find dice by name
                    List<DiceBlueprint> matchingDices = [.. _dices.Where(d => d.ClassName.Contains(diceName, StringComparison.OrdinalIgnoreCase))];
                    DiceBlueprint? foundDice = matchingDices.Count == 1 ? matchingDices.First() : _dices.FirstOrDefault(d => string.Equals(d.ClassName, diceName, StringComparison.OrdinalIgnoreCase));
                    if (foundDice != null)
                    {
                        // execute dice
                        foundDice.Add(player);
                        return (foundDice.ClassName, foundDice.Description);
                    }
                }
                else
                {
                    DiceBlueprint randomDice = _dices[_random.Next(_dices.Count)];
                    // execute dice
                    randomDice.Add(player);
                    return (randomDice.ClassName, randomDice.Description);
                }
            }
            return (null, null);
        }

        private void RemoveDiceForPlayer(CCSPlayerController? player)
        {
            if (player == null
                || !player.IsValid)
            {
                return;
            }
            // remove the dice for the player (if any)
            foreach (DiceBlueprint dice in _dices)
            {
                if (dice._players.Contains(player))
                {
                    dice.Remove(player);
                }
            }
        }

        private void RemoveDicesForPlayers()
        {
            // reset all dices for all players
            foreach (DiceBlueprint dice in _dices)
            {
                dice.Destroy();
            }
        }

        private void InitializeModules()
        {
            if (_dices.Count > 0)
            {
                return;
            }
            // skip if globally disabled
            if (!_currentMapConfig.Enabled)
            {
                return;
            }
            // initialize BigTaserBattery module
            if (_currentMapConfig.Dices.BigTaserBattery.Enabled)
            {
                _dices.Add(new BigTaserBattery(Config, _currentMapConfig, Localizer));
            }
            // initialize ChangeName module
            if (_currentMapConfig.Dices.ChangeName.Enabled)
            {
                _dices.Add(new ChangeName(Config, _currentMapConfig, Localizer));
            }
            // initialize ChangePlayerModel module
            if (_currentMapConfig.Dices.ChangePlayerModel.Enabled)
            {
                _dices.Add(new ChangePlayerModel(Config, _currentMapConfig, Localizer));
            }
            // initialize ChangePlayerSize module
            if (_currentMapConfig.Dices.ChangePlayerSize.Enabled)
            {
                _dices.Add(new ChangePlayerSize(Config, _currentMapConfig, Localizer));
            }
            // initialize ChickenLeader module
            if (_currentMapConfig.Dices.ChickenLeader.Enabled)
            {
                _dices.Add(new ChickenLeader(Config, _currentMapConfig, Localizer));
            }
            // initialize DecreaseHealth module
            if (_currentMapConfig.Dices.DecreaseHealth.Enabled)
            {
                _dices.Add(new DecreaseHealth(Config, _currentMapConfig, Localizer));
            }
            // initialize IncreaseHealth module
            if (_currentMapConfig.Dices.IncreaseHealth.Enabled)
            {
                _dices.Add(new IncreaseHealth(Config, _currentMapConfig, Localizer));
            }
            // initialize DecreaseMoney module
            if (_currentMapConfig.Dices.DecreaseMoney.Enabled)
            {
                _dices.Add(new DecreaseMoney(Config, _currentMapConfig, Localizer));
            }
            // initialize IncreaseMoney module
            if (_currentMapConfig.Dices.IncreaseMoney.Enabled)
            {
                _dices.Add(new IncreaseMoney(Config, _currentMapConfig, Localizer));
            }
            // initialize GiveHealthShot module
            if (_currentMapConfig.Dices.GiveHealthShot.Enabled)
            {
                _dices.Add(new GiveHealthShot(Config, _currentMapConfig, Localizer));
            }
            // initialize OneHP module
            if (_currentMapConfig.Dices.OneHP.Enabled)
            {
                _dices.Add(new OneHP(Config, _currentMapConfig, Localizer));
            }
            // initialize LowGravity module
            if (_currentMapConfig.Dices.LowGravity.Enabled)
            {
                _dices.Add(new LowGravity(Config, _currentMapConfig, Localizer));
            }
            // initialize HighGravity module
            if (_currentMapConfig.Dices.HighGravity.Enabled)
            {
                _dices.Add(new HighGravity(Config, _currentMapConfig, Localizer));
            }
            // initialize Suicide module
            if (_currentMapConfig.Dices.Suicide.Enabled)
            {
                _dices.Add(new Suicide(Config, _currentMapConfig, Localizer));
            }
            // initialize IncreaseSpeed module
            if (_currentMapConfig.Dices.IncreaseSpeed.Enabled)
            {
                _dices.Add(new IncreaseSpeed(Config, _currentMapConfig, Localizer));
            }
            // initialize NoRecoil module
            if (_currentMapConfig.Dices.NoRecoil.Enabled)
            {
                _dices.Add(new NoRecoil(Config, _currentMapConfig, Localizer));
            }
            // initialize Respawn module
            if (_currentMapConfig.Dices.Respawn.Enabled)
            {
                _dices.Add(new Respawn(Config, _currentMapConfig, Localizer));
            }
            // initialize NoExplosives module
            if (_currentMapConfig.Dices.NoExplosives.Enabled)
            {
                _dices.Add(new NoExplosives(Config, _currentMapConfig, Localizer));
            }
            // initialize StripWeapons module
            if (_currentMapConfig.Dices.StripWeapons.Enabled)
            {
                _dices.Add(new StripWeapons(Config, _currentMapConfig, Localizer));
            }
            // initialize Vampire module
            if (_currentMapConfig.Dices.Vampire.Enabled)
            {
                _dices.Add(new Vampire(Config, _currentMapConfig, Localizer));
            }
            // initialize Invisible module
            if (_currentMapConfig.Dices.Invisible.Enabled)
            {
                _dices.Add(new Invisible(Config, _currentMapConfig, Localizer));
            }
            // register listeners
            RegisterListeners();
            RegisterEventHandlers();
            RegisterUserMessageHooks();
        }

        private void RegisterListeners()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Initializing listener for module {module.GetType().Name}");
                foreach (string listenerName in module.Listeners)
                {
                    //DebugPrint($"- {listenerName}");
                    DynamicHandlers.RegisterModuleListener(this, listenerName, module);
                }
            }
        }

        private void DeregisterListeners()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Destroying listener for module {module.GetType().Name}");
                foreach (string listenerName in module.Listeners)
                {
                    //DebugPrint($"- {listenerName}");
                    DynamicHandlers.DeregisterModuleListener(this, listenerName, module);
                }
            }
        }

        private void RegisterEventHandlers()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Initializing event handlers for module {module.GetType().Name}");
                foreach (string eventName in module.Events)
                {
                    //DebugPrint($"- {eventName}");
                    DynamicHandlers.RegisterModuleEventHandler(this, eventName, module);
                }
            }
        }

        private void DeregisterEventHandlers()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Destroying event handlers for module {module.GetType().Name}");
                foreach (string eventName in module.Events)
                {
                    //DebugPrint($"- {eventName}");
                    DynamicHandlers.DeregisterModuleEventHandler(this, eventName, module);
                }
            }
        }

        private void RegisterUserMessageHooks()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Registering user messages for module {module.GetType().Name}");
                foreach ((int userMessageId, HookMode hookMode) in module.UserMessages)
                {
                    //DebugPrint($"- UserMessage ID: {userMessageId}, HookMode: {hookMode}");
                    DynamicHandlers.RegisterUserMessageHook(this, userMessageId, module, hookMode);
                }
            }
        }

        private void DeregisterUserMessageHooks()
        {
            foreach (DiceBlueprint module in _dices)
            {
                //DebugPrint($"Deregistering user messages for module {module.GetType().Name}");
                foreach ((int userMessageId, HookMode hookMode) in module.UserMessages)
                {
                    //DebugPrint($"- UserMessage ID: {userMessageId}, HookMode: {hookMode}");
                    DynamicHandlers.DeregisterUserMessageHook(this, userMessageId, module, hookMode);
                }
            }
        }

        private void DestroyModules()
        {
            //DebugPrint("Destroying all modules...");
            // deregister listeners
            DeregisterListeners();
            DeregisterEventHandlers();
            DeregisterUserMessageHooks();
            // destroy all cosmetics modules
            foreach (DiceBlueprint module in _dices)
            {
                module.Destroy();
            }
            _dices.Clear();
        }
    }
}
