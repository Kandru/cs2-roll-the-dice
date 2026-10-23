using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
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
        // bumped per round: stops the "roll every x seconds" chain of an earlier round
        private int _rollEveryGeneration;
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
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            RegisterListener<Listeners.OnTick>(Hud.Banner.OnTick);
            RegisterListener<Listeners.OnTick>(Hud.Reel.OnTick);
            RegisterListener<Listeners.CheckTransmit>(Hud.CustomHud.OnCheckTransmit);
            // info menu ("!dice info"): own layout entity, mouse driven
            Hud.InfoMenu.Bind(this, this);
            RegisterListener<Listeners.OnTick>(Hud.InfoMenu.OnTick);
            RegisterListener<Listeners.CheckTransmit>(Hud.InfoMenu.FilterTransmit);
            RegisterListener<Listeners.OnCustomHudClicked>(Hud.InfoMenu.OnClicked);
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
                // set current map
                _currentMap = Server.MapName;
                // initialize configuration
                LoadMapConfig(_currentMap);
                _isDuringRound = true;
                // initialize dice modules
                InitializeModules();
                // restore menu languages and spawn the info menu entity
                foreach (CCSPlayerController player in Utilities.GetPlayers())
                {
                    ApplySavedLanguage(player);
                }
                Hud.InfoMenu.WarmUp();
            }
        }

        public override void Unload(bool hotReload)
        {
            // reset dice rolls on unload
            DestroyModules();
            Hud.InfoMenu.Shutdown();
            Hud.Reel.Shutdown();
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
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnMapEnd>(OnMapEnd);
            RemoveListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            RemoveListener<Listeners.OnTick>(Hud.Banner.OnTick);
            RemoveListener<Listeners.OnTick>(Hud.Reel.OnTick);
            RemoveListener<Listeners.CheckTransmit>(Hud.CustomHud.OnCheckTransmit);
            RemoveListener<Listeners.OnTick>(Hud.InfoMenu.OnTick);
            RemoveListener<Listeners.CheckTransmit>(Hud.InfoMenu.FilterTransmit);
            RemoveListener<Listeners.OnCustomHudClicked>(Hud.InfoMenu.OnClicked);
            Console.WriteLine(Localizer["core.unload"]);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // reset players that rolled the dice
            _playersThatRolledTheDice.Clear();
            _roundDice.Clear();
            // the client rebuilt every layout from XML: close the info menu and write its defaults back
            Hud.InfoMenu.ResetRound();
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
            // announce round start (in each player's language)
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.IsValid && !player.IsBot && !player.IsHLTV)
                {
                    player.PrintToChat(Text(player, "core.announcement"));
                }
            }
            // allow dice rolls
            _isDuringRound = true;
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
                        (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(entry, source: Hud.ReelSource.Auto);
                        // check if rolledDice is null or empty
                        if (rolledDice is null or "")
                        {
                            return;
                        }
                        // add player to the list of players that rolled the dice
                        _playersThatRolledTheDice[entry] = diceDescription ?? rolledDice;
                        // play sound
                        PlayDiceSoundForPlayer(entry, rolledDice);
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                }
            }
        }

        private void RollTheDiceEveryXSeconds(int seconds)
        {
            // one chain per round: a chain from an earlier round (or map) sees a newer generation and stops
            ScheduleRollEveryXSeconds(seconds, ++_rollEveryGeneration);
        }

        private void ScheduleRollEveryXSeconds(int seconds, int generation)
        {
            _ = AddTimer(seconds, () =>
            {
                if (!_isDuringRound || generation != _rollEveryGeneration)
                {
                    return;
                }

                foreach (CCSPlayerController entry in Utilities.GetPlayers()
                    .Where(p => !p.IsHLTV
                        && !p.IsBot
                        && p.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE))
                {
                    RemoveDiceForPlayer(entry, reason: DiceRemoveReason.NewDice);
                    _ = RollTheDiceForPlayer(entry, source: Hud.ReelSource.Auto);
                }
                ScheduleRollEveryXSeconds(seconds, generation);
            }, TimerFlags.STOP_ON_MAPCHANGE);
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
            _rollEveryGeneration++;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            Hud.Reel.Cancel(@event.Userid);
            RemoveDiceForPlayer(@event.Userid, DiceRemoveReason.Death);
            // dice Remove overrides often skip Banner.Hide — force the overlay off
            Hud.Banner.Hide(@event.Userid);
            // menus close on death; the round list of everyone else shows the new state
            Hud.InfoMenu.Close(@event.Userid);
            Hud.InfoMenu.MarkAllDirty();
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            RemoveDiceForPlayer(@event.Userid, DiceRemoveReason.Disconnect);
            Hud.Reel.Forget(@event.Userid);
            Hud.Banner.Release(@event.Userid);
            Hud.CustomHud.ReleasePlayer(@event.Userid);
            if (@event.Userid is { IsValid: true } player)
            {
                _ = _roundDice.Remove(player);
                Hud.InfoMenu.Forget(player.Slot);
                Hud.InfoMenu.MarkAllDirty();
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            ApplySavedLanguage(@event.Userid);
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
            PruneHistory();
            Hud.InfoMenu.OnMapStart();
        }

        private void OnMapEnd()
        {
            DestroyModules();
            Hud.Reel.Shutdown();
            Hud.Banner.Shutdown();
            Hud.CustomHud.Shutdown();
            // reset states
            _isDuringRound = false;
            _playersThatRolledTheDice.Clear();
            _roundDice.Clear();
            _PlayerCooldown.Clear();
            // reset dice usage counter for better distribution
            _diceUsageCount.Clear();
        }

        private (string?, string?) RollTheDiceForPlayer(CCSPlayerController? player, string? diceName = null, Hud.ReelSource source = Hud.ReelSource.Rtd)
        {
            if (player == null || !player.IsValid || _dices.Count == 0)
            {
                return (null, null);
            }

            RemoveDiceForPlayer(player, DiceRemoveReason.NewDice);

            // diceName is expected to be an exact ClassName when provided (resolved by !givedice)
            DiceBlueprint? dice = diceName != null
                ? _dices.FirstOrDefault(d => string.Equals(d.ClassName, diceName, StringComparison.OrdinalIgnoreCase))
                : GetWeightedRandomDice();
            if (dice == null)
            {
                return (null, null);
            }

            // visual only: the reel shows the dice chosen above and holds back its notifications until it stops
            _ = Hud.Reel.Begin(player, dice.ClassName, source);
            dice.Add(player);
            RecordRoll(player, dice);
            _diceUsageCount.TryGetValue(dice, out int usage);
            _diceUsageCount[dice] = usage + 1;
            return (dice.ClassName, dice.Description);
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
            Hud.Reel.SetPool([.. _dices.Select(static d => d.ClassName)], Localizer);
            _perkRows.Clear();
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
            Hud.Reel.SetPool([], Localizer);
            _perkRows.Clear();
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
            // while the reel spins the dice sound becomes its stop sound
            if (Hud.Reel.TryDeferSound(player, () => PlayDiceSoundForPlayer(player, rolledDice, command)))
            {
                return;
            }
            // play sound directly at player if it starts with "sounds/"
            if (Config.Sounds.DiceRollSound.StartsWith("sounds/"))
            {
                // simply play sound (will be played at 100% volume regardless of the player's volume settings)
                // "play <path>" is a client command: only plain paths, never a chained command from the config
                if (SoundPath.IsSafe(Config.Sounds.DiceRollSound))
                {
                    player.ExecuteClientCommand($"play {Config.Sounds.DiceRollSound}");
                }
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
