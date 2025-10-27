using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class ChangePlayerSize : DiceBlueprint
    {
        public override string ClassName => "ChangePlayerSize";
        public readonly Random _random = new();

        public ChangePlayerSize(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid
                || player.Pawn?.Value?.CBodyComponent?.SceneNode?.GetSkeletonInstance() == null)
            {
                return;
            }
            // get random player size, avoiding sizes too close to 1.0 (100%)
            float minSize = (float)_config.Dices.ChangePlayerSize.MinSize;
            float maxSize = (float)_config.Dices.ChangePlayerSize.MaxSize;
            float minChangeAmount = (float)_config.Dices.ChangePlayerSize.MinChangeAmount;

            // determine valid ranges: [minSize, 1.0 - minChangeAmount] and [1.0 + minChangeAmount, maxSize]
            float lowerRangeMax = Math.Min(1.0f - minChangeAmount, maxSize);
            float upperRangeMin = Math.Max(1.0f + minChangeAmount, minSize);

            float playerSize;
            // check if both ranges are valid
            bool hasLowerRange = minSize < lowerRangeMax;
            bool hasUpperRange = upperRangeMin < maxSize;

            if (hasLowerRange && hasUpperRange)
            {
                // randomly choose between lower and upper range
                if (_random.Next(2) == 0)
                {
                    // generate in lower range [minSize, 1.0 - minChangeAmount]
                    playerSize = (float)(_random.NextDouble() * (lowerRangeMax - minSize) + minSize);
                }
                else
                {
                    // generate in upper range [1.0 + minChangeAmount, maxSize]
                    playerSize = (float)(_random.NextDouble() * (maxSize - upperRangeMin) + upperRangeMin);
                }
            }
            else if (hasLowerRange)
            {
                // only lower range is valid
                playerSize = (float)(_random.NextDouble() * (lowerRangeMax - minSize) + minSize);
            }
            else if (hasUpperRange)
            {
                // only upper range is valid
                playerSize = (float)(_random.NextDouble() * (maxSize - upperRangeMin) + upperRangeMin);
            }
            else
            {
                // no valid range (shouldn't happen with proper config), fallback logic
                playerSize = (float)(_random.NextDouble() * (maxSize - minSize) + minSize);
            }

            playerSize = float.Round(playerSize, 2);

            // change player size
            ChangeSize(player, playerSize);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "playerSize", playerSize.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            ChangeSize(player, 1f);
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private void ChangeSize(CCSPlayerController? player, float size)
        {
            if (player?.Pawn?.Value?.CBodyComponent?.SceneNode == null
                || !player.Pawn.IsValid)
            {
                return;
            }
            player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = size;
            player.Pawn.Value.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_CBodyComponent");
            // adjust health if enabled
            if (_config.Dices.ChangePlayerSize.AdjustHealth)
            {
                player.Pawn.Value.Health = (int)(100 * size);
                player.Pawn.Value.MaxHealth = (int)(100 * size);
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
            }
        }
    }
}
