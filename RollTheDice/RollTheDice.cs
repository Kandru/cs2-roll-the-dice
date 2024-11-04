﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "Map Modifiers Plugin";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.0.11";

        private string _currentMap = "";
        private List<CCSPlayerController> _playersThatRolledTheDice = new();
        private List<Func<CCSPlayerController, CCSPlayerPawn, string>> _dices = new();
        private Random _random = new Random();

        public override void Load(bool hotReload)
        {
            // initialize configuration
            LoadConfig();
            // initialize dices
            InitializeDices();
            // register listeners
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            CreateDiceFastBombActionListener();
            CreateDicePlayerVampireListener();
            CreateDicePlayerDisguiseAsPlantListener();
            // print message if hot reload
            if (hotReload)
            {
                // set current map
                _currentMap = Server.MapName;
                // initialize configuration
                InitializeConfig(_currentMap);
                Console.WriteLine(Localizer["core.hotreload"]);
            }
        }

        public override void Unload(bool hotReload)
        {
            // reset dice rolls on unload
            ResetDices();
            Console.WriteLine(Localizer["core.unload"]);
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // reset players that rolled the dice
            _playersThatRolledTheDice.Clear();
            // reset dices (necessary after warmup)
            ResetDices();
            // announce round start
            SendGlobalChatMessage(Localizer["core.announcement"]);
            // continue event
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            ResetDices();
            // continue event
            return HookResult.Continue;
        }

        private void InitializeDices()
        {
            // create dynamic list containing functions to execute for each dice
            _dices = new List<Func<CCSPlayerController, CCSPlayerPawn, string>>
            {
                // add functions for each dice
                (player, playerPawn) => DiceIncreaseHealth(player, playerPawn),
                (player, playerPawn) => DiceDecreaseHealth(player, playerPawn),
                (player, playerPawn) => DiceIncreaseSpeed(player, playerPawn),
                (player, playerPawn) => DiceChangeName(player, playerPawn),
                (player, playerPawn) => DicePlayerInvisible(player, playerPawn),
                (player, playerPawn) => DicePlayerSuicide(player, playerPawn),
                (player, playerPawn) => DicePlayerRespawn(player, playerPawn),
                (player, playerPawn) => DiceStripWeapons(player, playerPawn),
                (player, playerPawn) => DiceChickenLeader(player, playerPawn),
                (player, playerPawn) => DiceFastBombAction(player, playerPawn),
                (player, playerPawn) => DicePlayerVampire(player, playerPawn),
                (player, playerPawn) => DicePlayerLowGravity(player, playerPawn),
                (player, playerPawn) => DicePlayerHighGravity(player, playerPawn),
                (player, playerPawn) => DicePlayerOneHP(player, playerPawn),
                (player, playerPawn) => DicePlayerDisguiseAsPlant(player, playerPawn),
            };
        }

        private void ResetDices()
        {
            ResetDicePlayerInvisible();
            ResetDiceIncreaseSpeed();
            ResetDiceChangeName();
            ResetDiceFastBombAction();
            ResetDicePlayerVampire();
            ResetDicePlayerDisguiseAsPlant();
        }

        private int GetRandomDice()
        {
            // get random dice
            return _random.Next(0, _dices.Count);
        }
    }
}
