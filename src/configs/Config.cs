using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Hud;
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
        [JsonPropertyName("no_explosives")] public NoExplosivesConfig NoExplosives { get; set; } = new NoExplosivesConfig();
        [JsonPropertyName("strip_weapons")] public StripWeaponsConfig StripWeapons { get; set; } = new StripWeaponsConfig();
        [JsonPropertyName("vampire")] public VampireConfig Vampire { get; set; } = new VampireConfig();
        [JsonPropertyName("invisible")] public InvisibleConfig Invisible { get; set; } = new InvisibleConfig();
        [JsonPropertyName("glow")] public GlowConfig Glow { get; set; } = new GlowConfig();
        [JsonPropertyName("player_health_bar")] public PlayerHealthBarConfig PlayerHealthBar { get; set; } = new PlayerHealthBarConfig();
        [JsonPropertyName("fog_of_war")] public FogOfWarConfig FogOfWar { get; set; } = new FogOfWarConfig();
        [JsonPropertyName("play_as_chicken")] public PlayAsChickenConfig PlayAsChicken { get; set; } = new PlayAsChickenConfig();
        [JsonPropertyName("unlimited_ammo")] public InfiniteAmmoConfig InfiniteAmmo { get; set; } = new InfiniteAmmoConfig();
        [JsonPropertyName("random_weapon")] public RandomWeaponConfig RandomWeapon { get; set; } = new RandomWeaponConfig();
        [JsonPropertyName("damage_multiplier")] public DamageMultiplierConfig DamageMultiplier { get; set; } = new DamageMultiplierConfig();
        [JsonPropertyName("headshot_only")] public HeadshotOnlyConfig HeadshotOnly { get; set; } = new HeadshotOnlyConfig();
        [JsonPropertyName("no_headshot")] public NoHeadshotConfig NoHeadshot { get; set; } = new NoHeadshotConfig();
        [JsonPropertyName("weird_grenades")] public WeirdGrenadesConfig WeirdGrenades { get; set; } = new WeirdGrenadesConfig();
        [JsonPropertyName("loud_steps")] public LoudStepsConfig LoudSteps { get; set; } = new LoudStepsConfig();
        [JsonPropertyName("longer_flashes")] public LongerFlashesConfig LongerFlashes { get; set; } = new LongerFlashesConfig();
        [JsonPropertyName("reset_on_reload")] public ResetOnReloadConfig ResetOnReload { get; set; } = new ResetOnReloadConfig();
        [JsonPropertyName("bankrupt")] public BankruptConfig Bankrupt { get; set; } = new BankruptConfig();
        [JsonPropertyName("millionaire")] public MillionaireConfig Millionaire { get; set; } = new MillionaireConfig();
        [JsonPropertyName("cutter")] public CutterConfig Cutter { get; set; } = new CutterConfig();
        [JsonPropertyName("return_to_sender")] public ReturnToSenderConfig ReturnToSender { get; set; } = new ReturnToSenderConfig();
        [JsonPropertyName("jammer")] public JammerConfig Jammer { get; set; } = new JammerConfig();
        [JsonPropertyName("deaf")] public DeafConfig Deaf { get; set; } = new DeafConfig();
        [JsonPropertyName("zoom_in")] public ZoomInConfig ZoomIn { get; set; } = new ZoomInConfig();
        [JsonPropertyName("zoom_out")] public ZoomOutConfig ZoomOut { get; set; } = new ZoomOutConfig();
        [JsonPropertyName("cloak")] public CloakConfig Cloak { get; set; } = new CloakConfig();
        [JsonPropertyName("fast_map_action")] public FastMapActionConfig FastMapAction { get; set; } = new FastMapActionConfig();
        [JsonPropertyName("disguise_as_prop")] public DisguiseAsPropConfig DisguiseAsProp { get; set; } = new DisguiseAsPropConfig();
        [JsonPropertyName("make_fake_gun_sounds")] public MakeFakeGunSoundsConfig MakeFakeGunSounds { get; set; } = new MakeFakeGunSoundsConfig();
        [JsonPropertyName("make_hostage_sounds")] public MakeHostageSoundsConfig MakeHostageSounds { get; set; } = new MakeHostageSoundsConfig();
        [JsonPropertyName("third_person_view")] public ThirdPersonViewConfig ThirdPersonView { get; set; } = new ThirdPersonViewConfig();
        [JsonPropertyName("blood_magic")] public BloodMagicConfig BloodMagic { get; set; } = new BloodMagicConfig();
        [JsonPropertyName("bouncy")] public BouncyConfig Bouncy { get; set; } = new BouncyConfig();
        [JsonPropertyName("brittle_bones")] public BrittleBonesConfig BrittleBones { get; set; } = new BrittleBonesConfig();
        [JsonPropertyName("butterfingers")] public ButterfingersConfig Butterfingers { get; set; } = new ButterfingersConfig();
        [JsonPropertyName("disarm")] public DisarmConfig Disarm { get; set; } = new DisarmConfig();
        [JsonPropertyName("double_jump")] public DoubleJumpConfig DoubleJump { get; set; } = new DoubleJumpConfig();
        [JsonPropertyName("drunk")] public DrunkConfig Drunk { get; set; } = new DrunkConfig();
        [JsonPropertyName("ender_pearl")] public EnderPearlConfig EnderPearl { get; set; } = new EnderPearlConfig();
        [JsonPropertyName("flash_rounds")] public FlashRoundsConfig FlashRounds { get; set; } = new FlashRoundsConfig();
        [JsonPropertyName("glider")] public GliderConfig Glider { get; set; } = new GliderConfig();
        [JsonPropertyName("healing_smoke")] public HealingSmokeConfig HealingSmoke { get; set; } = new HealingSmokeConfig();
        [JsonPropertyName("health_regen")] public HealthRegenConfig HealthRegen { get; set; } = new HealthRegenConfig();
        [JsonPropertyName("heartbeat_sensor")] public HeartbeatSensorConfig HeartbeatSensor { get; set; } = new HeartbeatSensorConfig();
        [JsonPropertyName("martyrdom")] public MartyrdomConfig Martyrdom { get; set; } = new MartyrdomConfig();
        [JsonPropertyName("slide")] public SlideConfig Slide { get; set; } = new SlideConfig();
        [JsonPropertyName("solar_flare")] public SolarFlareConfig SolarFlare { get; set; } = new SolarFlareConfig();
        [JsonPropertyName("toxic_smoke")] public ToxicSmokeConfig ToxicSmoke { get; set; } = new ToxicSmokeConfig();
        [JsonPropertyName("voodoo_doll")] public VoodooDollConfig VoodooDoll { get; set; } = new VoodooDollConfig();
        [JsonPropertyName("weapon_jam")] public WeaponJamConfig WeaponJam { get; set; } = new WeaponJamConfig();
        [JsonPropertyName("airstrike")] public AirstrikeConfig Airstrike { get; set; } = new AirstrikeConfig();
        [JsonPropertyName("blink")] public BlinkConfig Blink { get; set; } = new BlinkConfig();
        [JsonPropertyName("cluster_grenade")] public ClusterGrenadeConfig ClusterGrenade { get; set; } = new ClusterGrenadeConfig();
        [JsonPropertyName("infinite_zeus")] public InfiniteZeusConfig InfiniteZeus { get; set; } = new InfiniteZeusConfig();
        [JsonPropertyName("jetpack")] public JetpackConfig Jetpack { get; set; } = new JetpackConfig();
        [JsonPropertyName("marking_rounds")] public MarkingRoundsConfig MarkingRounds { get; set; } = new MarkingRoundsConfig();
        [JsonPropertyName("overshield")] public OvershieldConfig Overshield { get; set; } = new OvershieldConfig();
        [JsonPropertyName("throwing_knives")] public ThrowingKnivesConfig ThrowingKnives { get; set; } = new ThrowingKnivesConfig();
        [JsonPropertyName("black_hole")] public BlackHoleConfig BlackHole { get; set; } = new BlackHoleConfig();
        [JsonPropertyName("chain_lightning")] public ChainLightningConfig ChainLightning { get; set; } = new ChainLightningConfig();
        [JsonPropertyName("confetti_kills")] public ConfettiKillsConfig ConfettiKills { get; set; } = new ConfettiKillsConfig();
        [JsonPropertyName("explosive_rounds")] public ExplosiveRoundsConfig ExplosiveRounds { get; set; } = new ExplosiveRoundsConfig();
        [JsonPropertyName("frost_rounds")] public FrostRoundsConfig FrostRounds { get; set; } = new FrostRoundsConfig();
        [JsonPropertyName("gun_game")] public GunGameConfig GunGame { get; set; } = new GunGameConfig();
        [JsonPropertyName("homing_grenades")] public HomingGrenadesConfig HomingGrenades { get; set; } = new HomingGrenadesConfig();
        [JsonPropertyName("ice_skating")] public IceSkatingConfig IceSkating { get; set; } = new IceSkatingConfig();
        [JsonPropertyName("incendiary_rounds")] public IncendiaryRoundsConfig IncendiaryRounds { get; set; } = new IncendiaryRoundsConfig();
        [JsonPropertyName("joker")] public JokerConfig Joker { get; set; } = new JokerConfig();
        [JsonPropertyName("knockback_rounds")] public KnockbackRoundsConfig KnockbackRounds { get; set; } = new KnockbackRoundsConfig();
        [JsonPropertyName("laser_pointer")] public LaserPointerConfig LaserPointer { get; set; } = new LaserPointerConfig();
        [JsonPropertyName("ninja_smoke")] public NinjaSmokeConfig NinjaSmoke { get; set; } = new NinjaSmokeConfig();
        [JsonPropertyName("play_dead")] public PlayDeadConfig PlayDead { get; set; } = new PlayDeadConfig();
        [JsonPropertyName("proximity_mine")] public ProximityMineConfig ProximityMine { get; set; } = new ProximityMineConfig();
        [JsonPropertyName("quick_reload")] public QuickReloadConfig QuickReload { get; set; } = new QuickReloadConfig();
        [JsonPropertyName("radar_scan")] public RadarScanConfig RadarScan { get; set; } = new RadarScanConfig();
        [JsonPropertyName("recoil_propulsion")] public RecoilPropulsionConfig RecoilPropulsion { get; set; } = new RecoilPropulsionConfig();
        [JsonPropertyName("thorns")] public ThornsConfig Thorns { get; set; } = new ThornsConfig();
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
        // language chosen in the info menu ("de", "en"; empty = server default)
        [JsonPropertyName("language")] public string Language { get; set; } = "";
    }

    public class TriggerConfig
    {
        // event to trigger RTD
        [JsonPropertyName("event")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DiceTriggerEvent TriggerEvent { get; set; } = DiceTriggerEvent.RoundFreezeEnd;
        // Force all players to RTD regardless of their personal setting
        [JsonPropertyName("force_all_players")] public bool ForceAllPlayers { get; set; } = false;
        // Allow players to opt-in for automatic RTD (only used when ForceAllPlayers is false)
        [JsonPropertyName("allow_player_auto_rtd")] public bool AllowPlayerAutoRtd { get; set; } = true;
        // fun: roll the dice every x seconds (0 to disable)
        [JsonPropertyName("roll_the_dice_every_x_seconds")] public int RollTheDiceEveryXSeconds { get; set; } = 0;
    }

    public class SoundConfig
    {
        // sound to play on dice roll
        [JsonPropertyName("dice_sound")] public string DiceRollSound { get; set; } = "sounds/ui/coin_pickup_01.vsnd";
        // whether or not to play only on command usage or also on event trigger
        [JsonPropertyName("play_on_command_only")] public bool PlayOnCommandOnly { get; set; } = false;
    }

    public class ReelConfig
    {
        // show the slot-machine reel when a dice is rolled (needs notify_player_via_panorama)
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // spin duration in seconds (1.0 - 5.0, steps of 0.5)
        [JsonPropertyName("spin_seconds")] public float SpinSeconds { get; set; } = 2.0f;
        // how long the result stays before the reel fades out (0.5 - 5.0, steps of 0.5)
        [JsonPropertyName("hold_seconds")] public float HoldSeconds { get; set; } = 1.5f;
        // sound per row passing the win line (sound event or "sounds/..." path, empty = off)
        [JsonPropertyName("tick_sound")] public string TickSound { get; set; } = "UIPanorama.container_weapon_ticker";
        // category per dice: "very_good", "good", "neutral", "bad" or "very_bad" (missing dices use the built-in default)
        [JsonPropertyName("tones")] public Dictionary<string, string> Tones { get; set; } = DefaultTones();
        // set by the plugin: 2 = tones migrated to the five categories (missing in older configs = 0)
        [JsonPropertyName("tones_version")] public int TonesVersion { get; set; }

        private static Dictionary<string, string> DefaultTones()
        {
            Dictionary<string, string> tones = [];
            foreach ((string dice, ReelTone tone) in Reel.DefaultTones)
            {
                tones[dice] = Reel.ToneName(tone);
            }
            return tones;
        }
    }

    /// <summary>Set from the admin tab of the info menu (saved at once); can also be edited here.</summary>
    public class RollAdminConfig
    {
        // nobody can roll (command, auto roll, timed rolls); admins can still hand out dices
        [JsonPropertyName("rolls_locked")] public bool RollsLocked { get; set; } = false;
        // SteamID64s that cannot roll
        [JsonPropertyName("locked_players")] public List<ulong> LockedPlayers { get; set; } = [];
        // mirror mode: a random perk rolled by one team is owed to the other team's next roll (per round);
        // everyone rolls at the trigger event of the round
        [JsonPropertyName("mirror_teams")] public bool MirrorTeams { get; set; } = false;
        // categories a roll never draws: "very_good", "good", "neutral", "bad", "very_bad"
        [JsonPropertyName("disabled_categories")] public List<string> DisabledCategories { get; set; } = [];
        // dice class names a roll never draws (e.g. "Suicide")
        [JsonPropertyName("disabled_dices")] public List<string> DisabledDices { get; set; } = [];
    }

    public class PrecacheConfig
    {
        [JsonPropertyName("soundevent_file")] public string SoundEventFile { get; set; } = "soundevents/soundevents_rollthedice.vsndevts";
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // allow !rtd during warmup
        [JsonPropertyName("allow_rtd_during_warmup")] public bool AllowRtdDuringWarmup { get; set; } = false;
        // event to trigger RTD
        [JsonPropertyName("trigger")] public TriggerConfig DiceTrigger { get; set; } = new TriggerConfig();
        // limit !rtd usage to every X rounds (only set one of both)
        [JsonPropertyName("cooldown_rounds")] public int CooldownRounds { get; set; } = 0;
        // limit !rtd usage to every X seconds (only set one of both)
        [JsonPropertyName("cooldown_seconds")] public int CooldownSeconds { get; set; } = 0;
        // price to charge on command usage
        [JsonPropertyName("price_to_dice")] public int PriceToDice { get; set; } = 0;
        // allow re-dice after respawn
        [JsonPropertyName("allow_dice_after_respawn")] public bool AllowDiceAfterRespawn { get; set; } = false;
        // notify other players when a player rolls the dice
        [JsonPropertyName("notify_other_players_about_dices_rolled")] public bool NotifyOtherPlayers { get; set; } = true;
        // notify player via chat
        [JsonPropertyName("notify_player_via_chatmsg")] public bool NotifyPlayerViaChatMsg { get; set; } = true;
        // notify player via center message
        [JsonPropertyName("notify_player_via_centermsg")] public bool NotifyPlayerViaCenterMsg { get; set; } = true;
        // notify player via panorama banner HUD
        [JsonPropertyName("notify_player_via_panorama")] public bool NotifyPlayerViaPanorama { get; set; } = true;
        // banner accent theme (theme-* classes; see HudTheme)
        [JsonPropertyName("panorama_theme")] public string PanoramaTheme { get; set; } = "gold";
        // slot-machine reel shown while the dice rolls (visual only)
        [JsonPropertyName("reel")] public ReelConfig Reel { get; set; } = new ReelConfig();
        // roll locks and which dices a roll can draw (admin tab of "!dice info")
        [JsonPropertyName("admin")] public RollAdminConfig Admin { get; set; } = new RollAdminConfig();

        // dices configuration
        [JsonPropertyName("dices")] public DicesConfig Dices { get; set; } = new DicesConfig();
        // sound configuration
        [JsonPropertyName("sounds")] public SoundConfig Sounds { get; set; } = new SoundConfig();
        // precache configuration
        [JsonPropertyName("precache")] public PrecacheConfig Precache { get; set; } = new PrecacheConfig();
        // map configurations
        [JsonPropertyName("maps")] public Dictionary<string, MapConfig> MapConfigs { get; set; } = [];
        // player configuration
        [JsonPropertyName("player_configs")] public Dictionary<ulong, PlayerConfig> PlayerConfigs { get; set; } = [];
    }

    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }
        private MapConfig _currentMapConfig = new();
        private Dictionary<ulong, PlayerConfig> _playerConfigs = [];

        private void ReloadConfigFromDisk()
        {
            try
            {
                // load config from disk
                Config.Reload();
                HudTheme.BindConfig(Config);
                Banner.BindConfig(Config);
                Reel.BindConfig(Config);
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
                // show the error to admins (they need to notice somehow); the text can contain server paths
                foreach (CCSPlayerController player in Utilities.GetPlayers())
                {
                    if (player is { IsValid: true, IsBot: false, IsHLTV: false }
                        && AdminManager.PlayerHasPermissions(player, AdminPermission))
                    {
                        player.PrintToChat(message);
                    }
                }
            }
        }

        private void LoadMapConfig(string mapName)
        {
            // select map config whose regexes (keys) match against the map name
            // use default global config if no map config matches
            _currentMapConfig = Config.MapConfigs
                .Where(mapConfig => FileSystemName.MatchesSimpleExpression(mapConfig.Key, mapName, ignoreCase: true))
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
            HudTheme.BindConfig(config);
            Banner.BindConfig(config);
            Reel.BindConfig(config);
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
