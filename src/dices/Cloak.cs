using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Cloak : DiceBlueprint
    {
        public override string ClassName => "Cloak";
        public override List<string> Listeners { get; } = ["OnTick"];
        private readonly Dictionary<CCSPlayerController, int> _visibility = [];

        public Cloak(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player))
            {
                return;
            }
            _players.Add(player);
            _visibility[player] = 255;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            SetVisibility(player, 255);
            _ = _visibility.Remove(player);
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _visibility.Keys)
            {
                SetVisibility(player, 255);
            }
            _visibility.Clear();
            base.Reset();
        }

        public void OnTick()
        {
            if (_visibility.Count == 0)
            {
                return;
            }

            foreach ((CCSPlayerController player, int visibility) in _visibility)
            {
                if (!IsAlivePlayer(player))
                {
                    continue;
                }

                if (player.Buttons == 0 && visibility > 0)
                {
                    int next = visibility - 1;
                    _visibility[player] = next;
                    SetVisibility(player, next);
                    // Update banner every ~4% to avoid SetText spam every tick.
                    if (next % 10 == 0 || next == 0)
                    {
                        string status = _localizer["dice_Cloak_gui_status"].Value
                            .Replace("{percentage}", ((next / 255.0) * 100).ToString("0.#") + "%");
                        Banner.Show(player, status);
                    }
                    continue;
                }

                if (player.Buttons != 0 && visibility < 255)
                {
                    _visibility[player] = 255;
                    SetVisibility(player, 255);
                    Banner.Show(player, _localizer["dice_Cloak_player"].Value);
                }
            }
        }

        private static void SetVisibility(CCSPlayerController player, int alpha)
        {
            if (player.PlayerPawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }
            pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }
    }
}

namespace RollTheDice.Configs
{
    public class CloakConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
