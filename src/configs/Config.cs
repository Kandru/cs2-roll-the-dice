using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Configs;
using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RollTheDice
{
    public class DicesConfig
    {
        [JsonPropertyName("big_taser_battery")] public BigTaserBatteryConfig BigTaserBattery { get; set; } = new BigTaserBatteryConfig();
        [JsonPropertyName("change_name")] public ChangeNameConfig ChangeName { get; set; } = new ChangeNameConfig();
        [JsonPropertyName("change_player_model")] public ChangePlayerModelConfig ChangePlayerModel { get; set; } = new ChangePlayerModelConfig();
        [JsonPropertyName("change_player_size")] public ChangePlayerSizeConfig ChangePlayerSize { get; set; } = new ChangePlayerSizeConfig();
        [JsonPropertyName("chicken_leader")] public ChickenLeaderConfig ChickenLeader { get; set; } = new ChickenLeaderConfig();
        [JsonPropertyName("decrease_health")] public DecreaseHealthConfig DecreaseHealth { get; set; } = new DecreaseHealthConfig();
        [JsonPropertyName("increase_health")] public IncreaseHealthConfig IncreaseHealth { get; set; } = new IncreaseHealthConfig();
        [JsonPropertyName("decrease_money")] public DecreaseMoneyConfig DecreaseMoney { get; set; } = new DecreaseMoneyConfig();
        [JsonPropertyName("increase_money")] public IncreaseMoneyConfig IncreaseMoney { get; set; } = new IncreaseMoneyConfig();
        [JsonPropertyName("give_health_shot")] public GiveHealthShotConfig GiveHealthShot { get; set; } = new GiveHealthShotConfig();
        [JsonPropertyName("one_hp")] public OneHPConfig OneHP { get; set; } = new OneHPConfig();
        [JsonPropertyName("low_gravity")] public LowGravityConfig LowGravity { get; set; } = new LowGravityConfig();
        [JsonPropertyName("high_gravity")] public HighGravityConfig HighGravity { get; set; } = new HighGravityConfig();
        [JsonPropertyName("suicide")] public SuicideConfig Suicide { get; set; } = new SuicideConfig();
        [JsonPropertyName("increase_speed")] public IncreaseSpeedConfig IncreaseSpeed { get; set; } = new IncreaseSpeedConfig();
        [JsonPropertyName("no_recoil")] public NoRecoilConfig NoRecoil { get; set; } = new NoRecoilConfig();
        [JsonPropertyName("respawn")] public RespawnConfig Respawn { get; set; } = new RespawnConfig();
    }

    public class MapConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // dices configuration
        [JsonPropertyName("dices")] public DicesConfig Dices { get; set; } = new DicesConfig();
    }

    public class PlayerConfig
    {
        // automatically roll the dice on spawn
        [JsonPropertyName("rtd_on_spawn")] public bool RtdOnSpawn { get; set; } = false;
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // allow !rtd during warmup
        [JsonPropertyName("allow_rtd_during_warmup")] public bool AllowRtdDuringWarmup { get; set; } = false;
        // automatically roll the dice on spawn
        [JsonPropertyName("roll_the_dice_on_round_start")] public bool RollTheDiceOnRoundStart { get; set; } = false;
        // fun: roll the dice every x seconds (0 to disable)
        [JsonPropertyName("roll_the_dice_every_x_seconds")] public int RollTheDiceEveryXSeconds { get; set; } = 0;
        // limit !rtd usage to every X rounds (only set one of both)
        [JsonPropertyName("cooldown_rounds")] public int CooldownRounds { get; set; } = 0;
        // limit !rtd usage to every X seconds (only set one of both)
        [JsonPropertyName("cooldown_seconds")] public int CooldownSeconds { get; set; } = 0;
        // sound to play on command usage
        [JsonPropertyName("sound_command")] public string CommandSound { get; set; } = "sounds/ui/coin_pickup_01.vsnd";
        // price to charge on command usage
        [JsonPropertyName("price_to_dice")] public int PriceToDice { get; set; } = 0;
        // allow re-dice after respawn
        [JsonPropertyName("allow_dice_after_respawn")] public bool AllowDiceAfterRespawn { get; set; } = false;
        // dices configuration
        [JsonPropertyName("dices")] public DicesConfig Dices { get; set; } = new DicesConfig();
        // map configurations
        [JsonPropertyName("maps")] public Dictionary<string, MapConfig> MapConfigs { get; set; } = [];
        // player configuration
        [JsonPropertyName("players")] public Dictionary<string, PlayerConfig> PlayerConfigs { get; set; } = [];
    }

    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }
        private MapConfig _currentMapConfig = new();
        private Dictionary<string, PlayerConfig> _playerConfigs = [];

        private void ReloadConfigFromDisk()
        {
            try
            {
                // load config from disk
                Config.Reload();
                // update GUI config
                //CheckGUIConfig();
                // update player config
                UpdatePlayerConfig();
                // save config to disk
                Config.Update();
            }
            catch (Exception e)
            {
                string message = Localizer["core.error"].Value.Replace("{error}", e.Message);
                // log error
                Console.WriteLine(message);
                // show error to users for transparency (admin needs to notice somehow)
                //SendGlobalChatMessage(message);
            }
        }

        private void LoadMapConfig(string mapName)
        {
            // select map config whose regexes (keys) match against the map name
            // use default global config if no map config matches
            _currentMapConfig = Config.MapConfigs
                .Where(mapConfig => FileSystemName.MatchesSimpleExpression(mapConfig.Key, mapName))
                .Select(mapConfig => mapConfig.Value)
                .FirstOrDefault() ?? new MapConfig
                {
                    Dices = Config.Dices
                };
            Console.WriteLine(Localizer["core.mapconfig"].Value.Replace("{mapName}", mapName));
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Console.WriteLine("OVERWRITTEN!");
            Console.WriteLine(Localizer["core.config"]);
        }

        private void UpdatePlayerConfig()
        {
            // load player config if empty
            if (_playerConfigs.Count == 0)
            {
                // load player configs
                _playerConfigs = Config.PlayerConfigs;
            }
            else
            {
                // save player configs
                Config.PlayerConfigs = _playerConfigs;
            }
        }

        private static object ConvertJsonElement(object element)
        {
            return element is JsonElement jsonElement
                ? jsonElement.ValueKind switch
                {
                    JsonValueKind.String => (object?)jsonElement.GetString() ?? string.Empty,
                    JsonValueKind.Number => jsonElement.TryGetSingle(out float number) ? number : 0.0f,
                    JsonValueKind.True => jsonElement.GetBoolean(),
                    JsonValueKind.False => jsonElement.GetBoolean(),
                    JsonValueKind.Object => jsonElement.EnumerateObject().ToDictionary(static property => property.Name, static property => ConvertJsonElement(property.Value)),
                    JsonValueKind.Array => jsonElement.EnumerateArray().Select(static element => ConvertJsonElement(element)).ToList(),
                    JsonValueKind.Undefined => string.Empty,
                    _ => string.Empty
                }
                : element;
        }
    }
}
