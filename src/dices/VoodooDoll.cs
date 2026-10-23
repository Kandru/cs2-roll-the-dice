using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class VoodooDoll : DiceBlueprint
    {
        public override string ClassName => "VoodooDoll";
        public override List<string> Listeners { get; } = ["OnPlayerTakeDamagePre"];
        private readonly Random _random = new();
        // holder -> the enemy that shares the holder's pain
        private readonly Dictionary<CCSPlayerController, CCSPlayerController> _dolls = [];

        public VoodooDoll(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player))
            {
                return;
            }

            List<CCSPlayerController> enemies = [.. Utilities.GetPlayers().Where(p => Targets.IsAlive(p) && Targets.IsEnemy(player, p))];
            CCSPlayerController? doll = enemies.Count > 0 ? enemies[_random.Next(enemies.Count)] : null;
            _players.Add(player);
            if (doll != null)
            {
                _dolls[player] = doll;
                Tell(doll, "dice_VoodooDoll_doll", new() { { "playerName", player.PlayerName } });
            }
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "doll", doll?.PlayerName ?? "-" },
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _dolls.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _dolls.Clear();
        }

        /// <summary>Part of every damage the holder takes also hits the doll, dealt by the holder (kill feed, score).</summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_dolls.Count == 0
                || Damage.IsFromDice(info)
                || info.Damage <= 0f
                || HolderOf(pawn) is not { } holder
                || !_dolls.TryGetValue(holder, out CCSPlayerController? doll)
                || !Targets.IsAlive(doll))
            {
                return HookResult.Continue;
            }

            float share = info.Damage * _config.Dices.VoodooDoll.DamageShare;
            Server.NextFrame(() =>
            {
                if (Targets.IsAlive(doll) && doll.PlayerPawn.Value is { } dollPawn)
                {
                    Damage.Deal(dollPawn, pawn.IsValid ? pawn : null, share);
                }
            });
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class VoodooDollConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // part of the holder's damage the doll takes (0.0 - 1.0)
        [JsonPropertyName("damage_share")] public float DamageShare { get; set; } = 0.5f;
    }
}
