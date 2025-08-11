using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class ParentDice(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly Dictionary<CCSPlayerController, Dictionary<string, CPointWorldText?>> _players = [];
        public virtual string ClassName => "ParentDice";
        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual List<string> Precache => [];

        public virtual void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player, []);
        }

        public virtual void Remove(CCSPlayerController player)
        {
            _ = _players.Remove(player);
        }

        public virtual void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", GetType().Name));
            _players.Clear();
        }
    }
}