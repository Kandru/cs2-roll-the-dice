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
        private readonly Dictionary<CCSPlayerController, Dictionary<string, object>> _playersThatRolledTheDice = [];
        private readonly Dictionary<CCSPlayerController, CPointWorldText> _playerGuis = [];
        private readonly Dictionary<string, int> _countRolledDices = [];
        private readonly Dictionary<CCSPlayerController, int> _PlayerCooldown = [];
        private readonly List<ParentDice> _dices = [];
        private bool _isDuringRound;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public override void Load(bool hotReload)
        {
            // initialize dices
            InitializeModules();
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
            //RemoveAllGUIs();
            // abort if warmup
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && !warmupPeriod)
            {
                return HookResult.Continue;
            }
            // announce round start
            //SendGlobalChatMessage(Localizer["core.announcement"]); TODO: announce round start
            // allow dice rolls
            _isDuringRound = true;
            // check if random dice should be rolled
            foreach (CCSPlayerController entry in Utilities.GetPlayers()
                .Where(p => p.IsValid
                    && !p.IsHLTV))
            {
                // rtd for everyone if enabled or for specific players if they have it enabled
                if (Config.RollTheDiceOnRoundStart
                    || (_playerConfigs.ContainsKey(entry.NetworkIDString)
                        && _playerConfigs[entry.NetworkIDString].RtdOnSpawn))
                {
                    _ = AddTimer(1f, () =>
                    {
                        if (entry == null || !entry.IsValid)
                        {
                            return;
                        }
                        RollTheDiceForPlayer(entry);
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
            // set current map
            _currentMap = mapName;
            // update configuration
            ReloadConfigFromDisk();
            // load map config
            LoadMapConfig(mapName);
        }

        private void OnMapEnd()
        {
            DestroyModules();
            RemoveDicesForPlayers();
            // disallow dice rolls
            _isDuringRound = false;
            // reset cooldown
            _PlayerCooldown.Clear();
        }

        private void RollTheDiceForPlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid)
            {
                return;
            }
            // remove old dice first
            RemoveDiceForPlayer(player);
            // roll the dice for the player - get random dice and add player
            if (_dices.Count > 0)
            {
                ParentDice randomDice = _dices[_random.Next(_dices.Count)];
                Console.WriteLine(randomDice.ClassName);
                randomDice.Add(player);
            }
        }

        private void RemoveDiceForPlayer(CCSPlayerController? player)
        {
            if (player == null
                || !player.IsValid)
            {
                return;
            }
            // remove the dice for the player (if any)
            foreach (ParentDice dice in _dices)
            {
                if (dice._players.Contains(player))
                {
                    dice.Remove(player);
                }
            }
        }

        private void RemoveDicesForPlayers()
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                RemoveDiceForPlayer(player);
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
            // register listeners
            RegisterListeners();
            RegisterEventHandlers();
            RegisterUserMessageHooks();
        }

        private void RegisterListeners()
        {
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
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
            foreach (ParentDice module in _dices)
            {
                module.Destroy();
            }
            _dices.Clear();
        }
    }
}
