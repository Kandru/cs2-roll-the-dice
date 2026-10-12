using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class PlayAsChicken : DiceBlueprint
    {
        public override string ClassName => "PlayAsChicken";
        public override List<string> Listeners { get; } = ["OnTick", "CheckTransmit", "OnPlayerButtonsChanged", "OnEntityTakeDamagePre"];
        public override Dictionary<int, HookMode> UserMessages { get; } = new() { { 208, HookMode.Pre } };

        private const int RedirectMarker = 1337;
        private static readonly string[] SoundNames = ["Chicken.Idle", "Chicken.Panic"];
        private static readonly HashSet<uint> SoundHashes =
        [
            SoundEventUtils.GenerateSoundHash("Chicken.Idle"),
            SoundEventUtils.GenerateSoundHash("Chicken.Panic"),
        ];

        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, ChickenState> _chickens = [];
        private readonly Dictionary<uint, CCSPlayerController> _chickenIndexToPlayer = [];

        private sealed class ChickenState
        {
            public required CChicken Chicken { get; init; }
            public int NextSound { get; set; }
            public byte PreviousCollisionGroup { get; init; }
            public SolidType_t PreviousSolidType { get; init; }
            public bool HadCollision { get; init; }
        }

        public PlayAsChicken(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player) || player.Pawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }

            CChicken? chicken = Utilities.CreateEntityByName<CChicken>("chicken");
            if (chicken is not { IsValid: true })
            {
                return;
            }

            chicken.DispatchSpawn();
            chicken.Teleport(pawn.AbsOrigin!, new QAngle(0, pawn.V_angle.Y, 0), pawn.AbsVelocity);
            if (chicken.CBodyComponent?.SceneNode != null)
            {
                chicken.CBodyComponent.SceneNode.Scale = _config.Dices.PlayAsChicken.ChickenSize;
            }
            chicken.AcceptInput("SetParent", pawn, chicken, "!activator");

            bool hadCollision = pawn.Collision != null;
            byte previousGroup = 0;
            SolidType_t previousSolid = SolidType_t.SOLID_NONE;
            if (hadCollision)
            {
                previousGroup = pawn.Collision!.CollisionGroup;
                previousSolid = pawn.Collision.SolidType;
                pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DEBRIS;
                pawn.Collision.SolidType = SolidType_t.SOLID_NONE;
                Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_nSolidType");
            }

            SetPlayerVisibility(player, 0);
            _players.Add(player);
            _chickens[player] = new ChickenState
            {
                Chicken = chicken,
                NextSound = (int)Server.CurrentTime + NextWait(),
                PreviousCollisionGroup = previousGroup,
                PreviousSolidType = previousSolid,
                HadCollision = hadCollision,
            };
            _chickenIndexToPlayer[chicken.Index] = player;
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_chickens.Remove(player, out ChickenState? state))
            {
                _ = _chickenIndexToPlayer.Remove(state.Chicken.Index);
                Entities.RemoveEntity(state.Chicken);
                if (state.HadCollision
                    && player.Pawn?.Value is { IsValid: true } pawn
                    && pawn.Collision != null)
                {
                    pawn.Collision.CollisionGroup = state.PreviousCollisionGroup;
                    pawn.Collision.SolidType = state.PreviousSolidType;
                    Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(pawn, "CCollisionProperty", "m_nSolidType");
                }
            }
            SetPlayerVisibility(player, 255);
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _chickens.Keys.ToArray())
            {
                Remove(player);
            }
            _chickens.Clear();
            _chickenIndexToPlayer.Clear();
            base.Reset();
        }

        public void OnTick()
        {
            if (_chickens.Count == 0)
            {
                return;
            }

            int now = (int)Server.CurrentTime;
            foreach ((CCSPlayerController player, ChickenState state) in _chickens)
            {
                if (state.NextSound > now || !IsAlivePlayer(player) || !state.Chicken.IsValid)
                {
                    continue;
                }
                _ = player.EmitSound(SoundNames[_random.Next(SoundNames.Length)]);
                state.NextSound = now + NextWait();
            }
        }

        public void CheckTransmit(CCheckTransmitInfoList infoList)
        {
            if (_chickens.Count == 0)
            {
                return;
            }

            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                if (player is not { IsValid: true, IsBot: false }
                    || !_chickens.TryGetValue(player, out ChickenState? state)
                    || !state.Chicken.IsValid)
                {
                    continue;
                }
                info.TransmitEntities.Remove(state.Chicken);
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (!_chickens.TryGetValue(player, out ChickenState? state)
                || !state.Chicken.IsValid
                || player.PlayerPawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }

            if (pressed != PlayerButtons.Duck && released != PlayerButtons.Duck)
            {
                return;
            }

            float z = pawn.AbsOrigin!.Z - (pressed == PlayerButtons.Duck ? 18f : 0f);
            state.Chicken.Teleport(new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, z));
        }

        public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity is not { IsValid: true }
                || !_chickenIndexToPlayer.TryGetValue(entity.Index, out CCSPlayerController? owner)
                || !IsAlivePlayer(owner))
            {
                return HookResult.Continue;
            }

            if (info.NumObjectsPenetrated == RedirectMarker)
            {
                info.NumObjectsPenetrated = 0;
                return HookResult.Continue;
            }

            float damage = info.Damage;
            int damageCustom = info.DamageCustom;
            float originalDamage = info.OriginalDamage;
            float totalledDamage = info.TotalledDamage;
            float friendlyFire = info.FriendlyFireDamageReductionRatio;
            Vector damageForce = info.DamageForce;
            Vector damagePosition = info.DamagePosition;
            uint attackerRaw = info.Attacker.Raw;
            uint inflictorRaw = info.Inflictor.Raw;
            HitGroup_t hitGroup = info.HitGroupId;
            DamageTypes_t bits = info.BitsDamageType;

            Server.NextFrame(() =>
            {
                if (!owner.IsValid || owner.Pawn?.Value is not { IsValid: true } livePawn)
                {
                    return;
                }

                int diSize = Schema.GetClassSize("CTakeDamageInfo");
                int drSize = Schema.GetClassSize("CTakeDamageResult");
                nint diPtr = Marshal.AllocHGlobal(diSize);
                nint drPtr = Marshal.AllocHGlobal(drSize);
                try
                {
                    unsafe
                    {
                        NativeMemory.Clear((void*)diPtr, (nuint)diSize);
                        NativeMemory.Clear((void*)drPtr, (nuint)drSize);
                    }

                    CTakeDamageInfo di = new(diPtr);
                    CTakeDamageResult dr = new(drPtr);
                    Schema.SetSchemaValue(di.Handle, "CTakeDamageInfo", "m_hInflictor", inflictorRaw);
                    Schema.SetSchemaValue(di.Handle, "CTakeDamageInfo", "m_hAttacker", attackerRaw);
                    di.Damage = damage;
                    di.DamageCustom = damageCustom;
                    di.OriginalDamage = originalDamage;
                    di.TotalledDamage = totalledDamage;
                    di.FriendlyFireDamageReductionRatio = friendlyFire;
                    di.DamageForce.X = damageForce.X;
                    di.DamageForce.Y = damageForce.Y;
                    di.DamageForce.Z = damageForce.Z;
                    di.DamagePosition.X = damagePosition.X;
                    di.DamagePosition.Y = damagePosition.Y;
                    di.DamagePosition.Z = damagePosition.Z;
                    di.HitGroupId = hitGroup;
                    di.BitsDamageType = bits;
                    di.NumObjectsPenetrated = RedirectMarker;

                    Schema.SetSchemaValue(dr.Handle, "CTakeDamageResult", "m_pOriginatingInfo", di.Handle);
                    int rd = (int)Math.Round(di.Damage);
                    int rtd = (int)Math.Round(di.TotalledDamage);
                    dr.HealthLost = rd;
                    dr.DamageDealt = rd;
                    dr.PreModifiedDamage = di.Damage;
                    dr.TotalledHealthLost = rtd;
                    dr.TotalledDamageDealt = rtd;
                    dr.WasDamageSuppressed = false;

                    VirtualFunctions.CBaseEntity_TakeDamageOld.Invoke(livePawn, di, dr);
                }
                finally
                {
                    Marshal.FreeHGlobal(diPtr);
                    Marshal.FreeHGlobal(drPtr);
                }
            });

            return HookResult.Stop;
        }

        public HookResult HookUserMessage208(UserMessage um)
        {
            uint soundevent = um.ReadUInt("soundevent_hash");
            if (!SoundHashes.Contains(soundevent))
            {
                return HookResult.Continue;
            }

            new SoundParams
            {
                Guid = um.ReadUInt("soundevent_guid"),
                Recipients = um.Recipients
            }
            .Volume(_config.Dices.PlayAsChicken.SoundVolume)
            .Send();
            return HookResult.Continue;
        }

        private int NextWait()
        {
            int min = Math.Max(1, _config.Dices.PlayAsChicken.MinSoundWaitTime);
            int max = Math.Max(min + 1, _config.Dices.PlayAsChicken.MaxSoundWaitTime + 1);
            return _random.Next(min, max);
        }

        private static void SetPlayerVisibility(CCSPlayerController? player, int alpha)
        {
            if (player?.Pawn?.Value is not { IsValid: true } pawn)
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
    public class PlayAsChickenConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("sound_volume")] public float SoundVolume { get; set; } = 3f;
        [JsonPropertyName("min_sound_wait_time")] public int MinSoundWaitTime { get; set; } = 3;
        [JsonPropertyName("max_sound_wait_time")] public int MaxSoundWaitTime { get; set; } = 7;
        [JsonPropertyName("chicken_size")] public float ChickenSize { get; set; } = 5f;
    }
}
