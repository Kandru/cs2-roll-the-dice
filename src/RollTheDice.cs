using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Enums;
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
        private readonly Dictionary<DiceBlueprint, int> _diceUsageCount = [];
        private bool _isDuringRound;
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());

        public override void Load(bool hotReload)
        {
            // update configuration
            ReloadConfigFromDisk();
            // register listeners
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventCsIntermission>(OnCsIntermission);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            RegisterListener<Listeners.OnTick>(Hud.Banner.SyncViewers);
            RegisterListener<Listeners.CheckTransmit>(Hud.CustomHud.OnCheckTransmit);
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
                // set current map
                _currentMap = Server.MapName;
                // initialize configuration
                LoadMapConfig(_currentMap);
                _isDuringRound = true;
                Hud.Banner.Resume();
                // initialize dice modules
                InitializeModules();
            }
        }

        public override void Unload(bool hotReload)
        {
            // reset dice rolls on unload
            DestroyModules();
            Hud.Banner.Shutdown();
            Hud.CustomHud.Shutdown();
            // update configuration
            ReloadConfigFromDisk();
            // unregister listeners
            DeregisterEventHandler<EventRoundStart>(OnRoundStart);
            DeregisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            DeregisterEventHandler<EventCsIntermission>(OnCsIntermission);
            DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            RemoveListener<Listeners.OnTick>(Hud.Banner.SyncViewers);
            RemoveListener<Listeners.CheckTransmit>(Hud.CustomHud.OnCheckTransmit);
            Console.WriteLine(Localizer["core.unload"]);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // reset players that rolled the dice
            _playersThatRolledTheDice.Clear();
            // hide banner before dice reset so Remove/Reset cannot re-show it
            Hud.Banner.Suspend();
            // reset dices (necessary after warmup)
            RemoveDicesForPlayers();
            // refresh game rules (to avoid caching warmup value)
            GameRules.Refresh();
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
            Hud.Banner.Resume();
            // roll the dice on round start if enabled
            if (Config.DiceTrigger.TriggerEvent == DiceTriggerEvent.RoundStart)
            {
                RollTheDiceOnRoundStart(force: Config.DiceTrigger.ForceAllPlayers);
                // optionally roll the dice every X seconds
                if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
                {
                    RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            // abort if warmup
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && warmupPeriod)
            {
                return HookResult.Continue;
            }
            if (Config.DiceTrigger.TriggerEvent != DiceTriggerEvent.RoundFreezeEnd)
            {
                return HookResult.Continue;
            }
            RollTheDiceOnRoundStart(force: Config.DiceTrigger.ForceAllPlayers);
            // optionally roll the dice every X seconds
            if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
            {
                RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
            }
            return HookResult.Continue;
        }

        private void RollTheDiceOnRoundStart(bool force = false)
        {
            // roll the dice for all players who requested it via "!rtd auto"
            foreach (CCSPlayerController entry in Utilities.GetPlayers()
                .Where(p => !p.IsHLTV
                    && !p.IsBot
                    && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
            {
                // rtd for everyone if enabled or for specific players if they have it enabled
                if (force
                    || (Config.DiceTrigger.AllowPlayerAutoRtd
                        && _playerConfigs.ContainsKey(entry.SteamID)
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
                        // play sound
                        PlayDiceSoundForPlayer(entry, rolledDice);
                    });
                }
            }
        }

        private void RollTheDiceEveryXSeconds(int seconds)
        {
            _ = AddTimer(seconds, () =>
            {
                if (!_isDuringRound)
                {
                    return;
                }

                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV
                        && !p.IsBot
                        && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    RemoveDiceForPlayer(entry, reason: DiceRemoveReason.NewDice);
                    _ = RollTheDiceForPlayer(entry);
                }
                RollTheDiceEveryXSeconds(seconds);
            });
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            EndRoundHud();
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
            return HookResult.Continue;
        }

        private HookResult OnCsIntermission(EventCsIntermission @event, GameEventInfo info)
        {
            EndRoundHud();
            return HookResult.Continue;
        }

        private void EndRoundHud()
        {
            Hud.Banner.Suspend();
            RemoveDicesForPlayers();
            _isDuringRound = false;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            RemoveDiceForPlayer(@event.Userid, DiceRemoveReason.Death);
            // dice Remove overrides often skip Banner.Hide — force the overlay off
            Hud.Banner.Hide(@event.Userid);
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            RemoveDiceForPlayer(@event.Userid, DiceRemoveReason.Disconnect);
            Hud.Banner.Release(@event.Userid);
            Hud.CustomHud.ReleasePlayer(@event.Userid);
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
            Hud.Banner.Shutdown();
            Hud.CustomHud.Shutdown();
            // reset states
            _isDuringRound = false;
            _playersThatRolledTheDice.Clear();
            _PlayerCooldown.Clear();
            // reset dice usage counter for better distribution
            _diceUsageCount.Clear();
        }

        private (string?, string?) RollTheDiceForPlayer(CCSPlayerController? player, string? diceName = null)
        {
            if (player == null || !player.IsValid)
            {
                return (null, null);
            }
            // remove old dice first
            RemoveDiceForPlayer(player, DiceRemoveReason.NewDice);
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
                        // track usage
                        if (!_diceUsageCount.ContainsKey(foundDice))
                        {
                            _diceUsageCount[foundDice] = 0;
                        }
                        _diceUsageCount[foundDice]++;
                        return (foundDice.ClassName, foundDice.Description);
                    }
                }
                else
                {
                    // use weighted random selection for better distribution
                    DiceBlueprint randomDice = GetWeightedRandomDice();
                    // execute dice
                    randomDice.Add(player);
                    // track usage
                    if (!_diceUsageCount.ContainsKey(randomDice))
                    {
                        _diceUsageCount[randomDice] = 0;
                    }
                    _diceUsageCount[randomDice]++;
                    return (randomDice.ClassName, randomDice.Description);
                }
            }
            return (null, null);
        }

        private DiceBlueprint GetWeightedRandomDice()
        {
            // find the minimum usage count
            int minUsage = _diceUsageCount.Count > 0 ? _diceUsageCount.Values.Min() : 0;

            // create a pool of least-used dices
            List<DiceBlueprint> leastUsedDices = [];
            foreach (DiceBlueprint dice in _dices)
            {
                int usage = _diceUsageCount.ContainsKey(dice) ? _diceUsageCount[dice] : 0;
                // only add dices that have been used the minimum amount or less
                if (usage <= minUsage)
                {
                    leastUsedDices.Add(dice);
                }
            }

            // if we have least-used dices, pick randomly from them
            if (leastUsedDices.Count > 0)
            {
                return leastUsedDices[_random.Next(leastUsedDices.Count)];
            }

            // fallback to pure random (should rarely happen)
            return _dices[_random.Next(_dices.Count)];
        }

        private void RemoveDiceForPlayer(CCSPlayerController? player, DiceRemoveReason reason)
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
                    dice.Remove(player, reason);
                }
            }
            if (Config.AllowDiceAfterRespawn)
            {
                _ = _playersThatRolledTheDice.Remove(player);
            }
        }

        private void RemoveDicesForPlayers()
        {
            // reset all dices for all players
            foreach (DiceBlueprint dice in _dices)
            {
                dice.Reset();
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

            Dictionary<string, Type> diceTypes = typeof(DiceBlueprint).Assembly.GetTypes()
                .Where(static t => t.IsSubclassOf(typeof(DiceBlueprint)) && !t.IsAbstract)
                .ToDictionary(static t => t.Name, static t => t);

            foreach (PropertyInfo property in typeof(DicesConfig).GetProperties())
            {
                if (!diceTypes.TryGetValue(property.Name, out Type? diceType))
                {
                    continue;
                }

                object? configValue = property.GetValue(_currentMapConfig.Dices);
                if (configValue?.GetType().GetProperty("Enabled")?.GetValue(configValue) is not bool enabled || !enabled)
                {
                    continue;
                }

                DiceBlueprint instance = (DiceBlueprint)Activator.CreateInstance(diceType, Config, _currentMapConfig, Localizer)!;
                _dices.Add(instance);
                _diceUsageCount[instance] = 0;
            }

            BindModuleHandlers(register: true);
        }

        private void DestroyModules()
        {
            BindModuleHandlers(register: false);
            foreach (DiceBlueprint module in _dices)
            {
                module.Destroy();
            }
            _dices.Clear();
            _diceUsageCount.Clear();
        }

        private void BindModuleHandlers(bool register)
        {
            foreach (DiceBlueprint module in _dices)
            {
                foreach (string listenerName in module.Listeners)
                {
                    Bindings.BindModuleListener(this, listenerName, module, register);
                }

                foreach (string eventName in module.Events)
                {
                    Bindings.BindModuleEventHandler(this, eventName, module, register);
                }

                foreach ((int userMessageId, HookMode hookMode) in module.UserMessages)
                {
                    Bindings.BindUserMessage(this, userMessageId, module, hookMode, register);
                }
            }
        }

        private void PlayDiceSoundForPlayer(CCSPlayerController? player, string rolledDice, bool command = false)
        {
            if (player == null
                || !player.IsValid
                || (!command && Config.Sounds.PlayOnCommandOnly)
                || string.IsNullOrEmpty(Config.Sounds.DiceRollSound))
            {
                return;
            }
            // play sound directly at player if it starts with "sounds/"
            if (Config.Sounds.DiceRollSound.StartsWith("sounds/"))
            {
                // simply play sound (will be played at 100% volume regardless of the player's volume settings)
                player.ExecuteClientCommand($"play {Config.Sounds.DiceRollSound}");
            }
            else
            {
                // only players that rolled the dice will hear the sound
                RecipientFilter filter = [player];
                // will be played at the player's volume settings
                _ = player.EmitSound(Config.Sounds.DiceRollSound.Replace("{dice}", rolledDice), filter);
            }
        }
    }
}
