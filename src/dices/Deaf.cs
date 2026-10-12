using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Deaf : DiceBlueprint
    {
        public override string ClassName => "Deaf";
        public override Dictionary<int, HookMode> UserMessages { get; } = new() { { 208, HookMode.Pre } };

        public Deaf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult HookUserMessage208(UserMessage um)
        {
            if (_players.Count == 0)
            {
                return HookResult.Continue;
            }

            foreach (CCSPlayerController entry in _players)
            {
                _ = um.Recipients.Remove(entry);
            }
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class DeafConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
