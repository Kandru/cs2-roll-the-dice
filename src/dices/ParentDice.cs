using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class ParentDice(PluginConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly List<CCSPlayerController> _players = [];
        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual List<string> Precache => [];

        public virtual void Add(CCSPlayerController player)
        {
            // This method can be overridden in derived classes to handle giving the dice
            if (player == null || !player.IsValid)
            {
                return;
            }
            _players.Add(player);
        }

        public virtual void Remove(CCSPlayerController player)
        {
            // This method can be overridden in derived classes to handle giving the dice
            _players.Remove(player);
        }

        public virtual void Destroy()
        {
            // This method can be overridden in derived classes to handle destruction logic
        }
    }
}
