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
        public override List<string> Listeners { get; } = ["OnTick", "CheckTransmit", "OnEntityTakeDamagePre", "OnPlayerTakeDamagePre"];
        public override Dictionary<int, HookMode> UserMessages { get; } = new() { { 208, HookMode.Pre } };

        private const int RedirectMarker = 1337;
        /// <summary>About this many default chickens stack to one player height.</summary>
        private const float ChickensPerPlayerHeight = 5.5f;
        /// <summary>Let the client finish the prop create before tick teleports (matches prophunt).</summary>
        private const float PropTeleportSettleDelay = 0.6f;
        private const string ChickenModel = "models/chicken/chicken.vmdl";
        // 1=start asleep, 8=motion disabled, 512/1024=non-interactive.
        // Do not include debris (4) or "no physics damage" (2) — those block bullet hits.
        private const uint NonInteractivePropSpawnflags = 1u | 8u | 512u | 1024u;

        private static readonly string[] SoundNames = ["Chicken.Idle", "Chicken.Panic"];
        private static readonly HashSet<uint> SoundHashes =
        [
            SoundEventUtils.GenerateSoundHash("Chicken.Idle"),
            SoundEventUtils.GenerateSoundHash("Chicken.Panic"),
        ];

        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, ChickenState> _chickens = [];
        private readonly Dictionary<uint, CCSPlayerController> _chickenIndexToPlayer = [];
        /// <summary>Slots currently receiving redirected chicken damage (bypass OnPlayerTakeDamagePre block).</summary>
        private readonly HashSet<int> _redirectAllowSlots = [];
        private readonly HashSet<int> _chickenSlots = [];

        private sealed class ChickenState
        {
            public required CPhysicsProp Chicken { get; init; }
            public CLogicCollisionPair? CollisionPair { get; init; }
            public string PreviousEntityName { get; init; } = string.Empty;
            public uint PreviousDisabledHitGroups { get; init; }
            public byte PreviousCollisionGroup { get; init; }
            public byte PreviousCollisionAttributeGroup { get; init; }
            public ulong PreviousInteractsAs { get; init; }
            public bool HadCollision { get; init; }
            public float TeleportAfter { get; init; }
            public int NextSound { get; set; }
            public int PreviousMaxHealth { get; init; }
        }

        public PlayAsChicken(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player) || player.PlayerPawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }

            float chickenScale = MathF.Max(0.001f, _config.Dices.PlayAsChicken.ChickenSize);
            float playerScale = MathF.Max(0.001f, chickenScale / ChickensPerPlayerHeight);
            string hiderName = $"rtd_chicken_hider_{player.Index}";
            string propName = $"rtd_chicken_prop_{player.Index}";
            string previousName = pawn.Entity?.Name ?? string.Empty;

            SetPlayerScale(player, playerScale);

            // Physics prop is the hitbox. Do not SetParent — a parented child shares the player trace.
            CPhysicsProp? chicken = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_override");
            if (chicken is not { IsValid: true })
            {
                SetPlayerScale(player, 1f);
                return;
            }

            CEntityIdentity identity = chicken.CBodyComponent!.SceneNode!.Owner!.Entity!;
            chicken.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(identity.Flags & ~(1 << 2));

            chicken.Health = 9999;
            chicken.MaxHealth = 9999;
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(chicken, "CBaseEntity", "m_iMaxHealth");

            chicken.SetModel(ChickenModel);
            if (chicken.Collision != null)
            {
                chicken.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                chicken.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            }

            CEntityKeyValues kv = new();
            kv.SetString("targetname", propName);
            // modelscale bakes Collision.Maxs on first spawn; skeleton scale drives the visible size.
            kv.SetFloat("modelscale", chickenScale);
            kv.SetVector("origin", pawn.AbsOrigin!);
            kv.SetAngle("angles", new QAngle(0, pawn.V_angle.Y, 0));
            chicken.DispatchSpawn(kv);
            chicken.Spawnflags = NonInteractivePropSpawnflags;
            FreezePropMotion(chicken);
            ApplyPropScale(chicken, chickenScale);
            // Re-apply after spawn — DispatchSpawn can reset collision group.
            // Do not overwrite InteractsAs: player-like flags on a physics prop stop bullet damage events.
            if (chicken.Collision != null)
            {
                chicken.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
                chicken.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                chicken.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            }

            // Masks.Shot includes Debris — DEBRIS still stops bullets. PassBullets is in
            // Masks.PlayerSolid (movement) but NOT in Masks.Shot, so rounds pass through
            // the invisible player as if they were not there. Hitgroups disabled as backup.
            uint previousHitGroups = 0;
            Span<uint> hitGroups = pawn.DisabledHitGroups;
            if (hitGroups.Length > 0)
            {
                previousHitGroups = hitGroups[0];
                hitGroups[0] = uint.MaxValue;
            }

            bool hadCollision = pawn.Collision != null;
            byte previousGroup = 0;
            byte previousAttrGroup = 0;
            ulong previousInteractsAs = 0;
            if (hadCollision)
            {
                previousGroup = pawn.Collision!.CollisionGroup;
                previousAttrGroup = pawn.Collision.CollisionAttribute.CollisionGroup;
                previousInteractsAs = pawn.Collision.CollisionAttribute.InteractsAs;
                pawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                pawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT;
                pawn.Collision.CollisionAttribute.InteractsAs = (ulong)Contents.PassBullets;
            }

            if (pawn.Entity != null)
            {
                pawn.Entity.Name = hiderName;
            }
            CLogicCollisionPair? pair = CreateCollisionPair(hiderName, propName);

            SetPlayerVisibility(player, 0);
            _players.Add(player);
            _ = _chickenSlots.Add(player.Slot);
            _chickens[player] = new ChickenState
            {
                Chicken = chicken,
                CollisionPair = pair,
                PreviousEntityName = previousName,
                PreviousDisabledHitGroups = previousHitGroups,
                PreviousCollisionGroup = previousGroup,
                PreviousCollisionAttributeGroup = previousAttrGroup,
                PreviousInteractsAs = previousInteractsAs,
                HadCollision = hadCollision,
                TeleportAfter = Server.CurrentTime + PropTeleportSettleDelay,
                NextSound = (int)Server.CurrentTime + NextWait(),
                PreviousMaxHealth = Math.Max(1, pawn.MaxHealth),
            };
            _chickenIndexToPlayer[chicken.Index] = player;
            SetHealth(pawn, Math.Max(1, _config.Dices.PlayAsChicken.Health));
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_chickens.Remove(player, out ChickenState? state))
            {
                _ = _chickenIndexToPlayer.Remove(state.Chicken.Index);
                _ = _chickenSlots.Remove(player.Slot);
                _ = _redirectAllowSlots.Remove(player.Slot);
                Entities.RemoveEntity(state.CollisionPair);
                Entities.RemoveEntity(state.Chicken);
                if (player.PlayerPawn?.Value is { IsValid: true } pawn)
                {
                    if (pawn.Entity != null)
                    {
                        pawn.Entity.Name = state.PreviousEntityName;
                    }
                    Span<uint> hitGroups = pawn.DisabledHitGroups;
                    if (hitGroups.Length > 0)
                    {
                        hitGroups[0] = state.PreviousDisabledHitGroups;
                    }
                    if (state.HadCollision && pawn.Collision != null)
                    {
                        pawn.Collision.CollisionGroup = state.PreviousCollisionGroup;
                        pawn.Collision.CollisionAttribute.CollisionGroup = state.PreviousCollisionAttributeGroup;
                        pawn.Collision.CollisionAttribute.InteractsAs = state.PreviousInteractsAs;
                    }
                    if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                    {
                        RestoreHealthRatio(pawn, state.PreviousMaxHealth);
                    }
                }
                SetPlayerScale(player, 1f);
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
            _chickenSlots.Clear();
            _redirectAllowSlots.Clear();
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
                if (!IsAlivePlayer(player)
                    || !state.Chicken.IsValid
                    || player.Pawn?.Value is not { IsValid: true } pawn
                    || pawn.AbsOrigin == null)
                {
                    continue;
                }

                if (Server.CurrentTime >= state.TeleportAfter)
                {
                    float duckZ = (player.Buttons & PlayerButtons.Duck) != 0 ? -18f : 0f;
                    state.Chicken.Teleport(
                        new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + duckZ),
                        new QAngle(0, pawn.V_angle.Y, 0),
                        new Vector());
                }

                if (state.NextSound > now)
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

            foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
            {
                if (viewer is not { IsValid: true, IsBot: false })
                {
                    continue;
                }

                CCSPlayerController? spectated = GetInEyeSpectatedPlayer(viewer);
                foreach ((CCSPlayerController owner, ChickenState state) in _chickens)
                {
                    if (!state.Chicken.IsValid)
                    {
                        continue;
                    }

                    // Hide from the chicken player and first-person spectators of that player.
                    if (owner == viewer || spectated == owner)
                    {
                        info.TransmitEntities.Remove(state.Chicken);
                        continue;
                    }

                    // Force-add so clients outside the spawn PVS still get the prop create.
                    info.TransmitEntities.Add(state.Chicken);
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
        {
            int slot = player.OriginalController.Value is { IsValid: true } controller
                ? controller.Slot
                : -1;

            // Allow one-shot redirected damage from the chicken prop.
            if (slot >= 0 && _redirectAllowSlots.Contains(slot))
            {
                return HookResult.Continue;
            }

            if (info.NumObjectsPenetrated == RedirectMarker)
            {
                info.NumObjectsPenetrated = 0;
                return HookResult.Continue;
            }

            // Block direct hits on the invisible chicken player; only redirected prop damage applies.
            return slot >= 0 && _chickenSlots.Contains(slot)
                ? HookResult.Stop
                : HookResult.Continue;
        }

        public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (entity is not { IsValid: true })
            {
                return HookResult.Continue;
            }

            if (info.NumObjectsPenetrated == RedirectMarker)
            {
                info.NumObjectsPenetrated = 0;
                return HookResult.Continue;
            }

            if (!_chickenIndexToPlayer.TryGetValue(entity.Index, out CCSPlayerController? owner)
                || !IsAlivePlayer(owner))
            {
                return HookResult.Continue;
            }

            // Prop bullet damage fields can be split across these; take the strongest signal.
            float damage = MathF.Max(info.Damage, MathF.Max(info.OriginalDamage, info.TotalledDamage));
            int damageCustom = info.DamageCustom;
            float originalDamage = info.OriginalDamage;
            float totalledDamage = info.TotalledDamage;
            float friendlyFire = info.FriendlyFireDamageReductionRatio;
            Vector damageForce = info.DamageForce;
            Vector damagePosition = info.DamagePosition;
            uint attackerRaw = info.Attacker.Raw;
            int ownerSlot = owner.Slot;

            Server.NextFrame(() =>
            {
                if (!owner.IsValid
                    || owner.PlayerPawn?.Value is not { IsValid: true } livePawn
                    || livePawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    return;
                }

                // Prefer engine damage path (kill credit / sounds). Fall back to HP if needed.
                if (damage > 0f)
                {
                    ApplyRedirectedTakeDamage(
                        livePawn,
                        ownerSlot,
                        damage,
                        damageCustom,
                        originalDamage,
                        totalledDamage,
                        friendlyFire,
                        damageForce,
                        damagePosition,
                        attackerRaw);
                    return;
                }

                // Last resort: some prop hits report 0 — still confirm a hit registered.
                ApplyRedirectedHealthDamage(livePawn, 1);
            });

            return HookResult.Stop;
        }

        private void ApplyRedirectedTakeDamage(
            CCSPlayerPawn livePawn,
            int ownerSlot,
            float damage,
            int damageCustom,
            float originalDamage,
            float totalledDamage,
            float friendlyFire,
            Vector damageForce,
            Vector damagePosition,
            uint attackerRaw)
        {
            int diSize = Schema.GetClassSize("CTakeDamageInfo");
            int drSize = Schema.GetClassSize("CTakeDamageResult");
            nint diPtr = Marshal.AllocHGlobal(diSize);
            nint drPtr = Marshal.AllocHGlobal(drSize);
            _ = _redirectAllowSlots.Add(ownerSlot);
            try
            {
                unsafe
                {
                    NativeMemory.Clear((void*)diPtr, (nuint)diSize);
                    NativeMemory.Clear((void*)drPtr, (nuint)drSize);
                }

                CTakeDamageInfo di = new(diPtr);
                CTakeDamageResult dr = new(drPtr);
                Schema.SetSchemaValue(di.Handle, "CTakeDamageInfo", "m_hInflictor", attackerRaw);
                Schema.SetSchemaValue(di.Handle, "CTakeDamageInfo", "m_hAttacker", attackerRaw);
                di.Damage = damage;
                di.DamageCustom = damageCustom;
                di.OriginalDamage = originalDamage > 0f ? originalDamage : damage;
                di.TotalledDamage = totalledDamage > 0f ? totalledDamage : damage;
                di.FriendlyFireDamageReductionRatio = friendlyFire;
                di.DamageForce.X = damageForce.X;
                di.DamageForce.Y = damageForce.Y;
                di.DamageForce.Z = damageForce.Z;
                di.DamagePosition.X = damagePosition.X;
                di.DamagePosition.Y = damagePosition.Y;
                di.DamagePosition.Z = damagePosition.Z;
                di.DamageFlags = TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;
                di.BitsDamageType = DamageTypes_t.DMG_BULLET;
                di.NumObjectsPenetrated = RedirectMarker;

                Schema.SetSchemaValue(dr.Handle, "CTakeDamageResult", "m_pOriginatingInfo", di.Handle);
                int rd = Math.Max(1, (int)Math.Round(di.Damage));
                int rtd = Math.Max(rd, (int)Math.Round(di.TotalledDamage));
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
                _ = _redirectAllowSlots.Remove(ownerSlot);
                Marshal.FreeHGlobal(diPtr);
                Marshal.FreeHGlobal(drPtr);
            }
        }

        private static void ApplyRedirectedHealthDamage(CCSPlayerPawn livePawn, int damage)
        {
            int hp = livePawn.Health - Math.Max(1, damage);
            if (hp <= 0)
            {
                livePawn.CommitSuicide(false, true);
                return;
            }

            livePawn.Health = hp;
            Utilities.SetStateChanged(livePawn, "CBaseEntity", "m_iHealth");
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

        private static CLogicCollisionPair? CreateCollisionPair(string attach1, string attach2)
        {
            CLogicCollisionPair? pair = Utilities.CreateEntityByName<CLogicCollisionPair>("logic_collision_pair");
            if (pair is not { IsValid: true })
            {
                return null;
            }

            CEntityKeyValues kv = new();
            kv.SetBool("support_multiple_entities_with_same_name", true);
            kv.SetBool("include_hierarchy", true);
            kv.SetBool("startdisabled", false);
            kv.SetString("attach1", attach1);
            kv.SetString("attach2", attach2);
            kv.SetString("targetname", $"lcp_{attach1}_{attach2}");
            pair.DispatchSpawn(kv);
            // Enabled pair disables collision between player and chicken so they can overlap.
            pair.AcceptInput("Enable");
            return pair;
        }

        private static void FreezePropMotion(CPhysicsProp prop)
        {
            prop.AcceptInput("DisableMotion");
            prop.MoveType = MoveType_t.MOVETYPE_NONE;
            prop.ActualMoveType = MoveType_t.MOVETYPE_NONE;
            Utilities.SetStateChanged(prop, "CBaseEntity", "m_MoveType");
        }

        private static void ApplyPropScale(CPhysicsProp prop, float scale)
        {
            if (prop.CBodyComponent?.SceneNode is null
                || prop.CBodyComponent.SceneNode.Handle == IntPtr.Zero)
            {
                return;
            }

            prop.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = scale;
            Utilities.SetStateChanged(prop, "CBaseEntity", "m_CBodyComponent");
        }

        /// <summary>Maps remaining chicken HP onto the max health the player had before the dice.</summary>
        private static void RestoreHealthRatio(CCSPlayerPawn pawn, int previousMaxHealth)
        {
            int chickenMax = Math.Max(1, pawn.MaxHealth);
            int restoredMax = Math.Max(1, previousMaxHealth);
            float ratio = Math.Clamp(pawn.Health / (float)chickenMax, 0f, 1f);
            int health = (int)Math.Round(ratio * restoredMax);
            if (health < 1)
            {
                health = 1;
            }
            SetHealth(pawn, health, restoredMax);
        }

        private static void SetHealth(CCSPlayerPawn pawn, int health, int? maxHealth = null)
        {
            pawn.Health = health;
            pawn.MaxHealth = maxHealth ?? health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        }

        private static void SetPlayerScale(CCSPlayerController? player, float scale)
        {
            if (player?.Pawn?.Value?.CBodyComponent?.SceneNode == null
                || !player.Pawn.IsValid)
            {
                return;
            }

            player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = scale;
            player.Pawn.Value.AcceptInput("SetScale", null, null, scale.ToString());
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_CBodyComponent");
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

        private static CCSPlayerController? GetInEyeSpectatedPlayer(CCSPlayerController viewer)
        {
            if (viewer.ObserverPawn?.Value is not { IsValid: true, ObserverServices: not null } obsPawn)
            {
                return null;
            }

            CPlayer_ObserverServices services = obsPawn.ObserverServices;
            if ((ObserverMode_t)services.ObserverMode != ObserverMode_t.OBS_MODE_IN_EYE)
            {
                return null;
            }

            if (services.ObserverTarget.Value is not CCSPlayerPawnBase { IsValid: true } targetPawn)
            {
                return null;
            }

            CCSPlayerController? controller = targetPawn.OriginalController.Value;
            return controller is { IsValid: true } && controller != viewer
                ? controller
                : null;
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
        [JsonPropertyName("health")] public int Health { get; set; } = 150;
    }
}
