using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ThirdPersonView : DiceBlueprint
    {
        public override string ClassName => "ThirdPersonView";
        public override List<string> Listeners { get; } = ["OnTick"];

        private sealed class CameraState
        {
            public required CPointCamera Camera { get; init; }
            public int TraceTick;
            public float BoomDistance = MaxDistance;
            public float Scale = 1f;
        }

        private readonly Dictionary<CCSPlayerController, CameraState> _cameras = [];

        private const float MaxDistance = 110f;
        private const float HeightScale = 95f;
        private const float MaxHeight = 100f;
        private const float BackwardOffset = 15f;
        private const float SmoothFactor = 0.3f;
        private const int TraceEveryNTicks = 3;
        private static readonly Vector ZeroVelocity = new();

        public ThirdPersonView(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player)
                || player.PlayerPawn?.Value is not { IsValid: true } pawn
                || _cameras.ContainsKey(player))
            {
                return;
            }

            CPointCamera? camera = Utilities.CreateEntityByName<CPointCamera>("point_camera");
            if (camera == null)
            {
                return;
            }

            CameraState state = new() { Camera = camera };
            camera.DispatchSpawn();
            if (!TryGetCameraTarget(pawn, state, out Vector position))
            {
                camera.Remove();
                return;
            }

            camera.Teleport(position, pawn.V_angle, ZeroVelocity);

            Server.NextFrame(() =>
            {
                if (!camera.IsValid
                    || !player.IsValid
                    || player.PlayerPawn?.Value is not { IsValid: true, CameraServices: not null } nextPawn)
                {
                    return;
                }

                nextPawn.CameraServices.ViewEntity.Raw = camera.EntityHandle.Raw;
                Utilities.SetStateChanged(nextPawn, "CBasePlayerPawn", "m_pCameraServices");
            });

            _players.Add(player);
            _cameras[player] = state;
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_cameras.Remove(player, out CameraState? state))
            {
                if (player.PlayerPawn?.Value is { IsValid: true, CameraServices: not null } pawn)
                {
                    pawn.CameraServices.ViewEntity.Raw = uint.MaxValue;
                    Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");
                }

                if (state.Camera.IsValid)
                {
                    state.Camera.Remove();
                }
            }

            base.Remove(player, reason);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _cameras.Keys.ToList())
            {
                Remove(player);
            }

            base.Reset();
        }

        public void OnTick()
        {
            if (_cameras.Count == 0)
            {
                return;
            }

            foreach ((CCSPlayerController player, CameraState state) in _cameras)
            {
                if (!IsAlivePlayer(player)
                    || !state.Camera.IsValid
                    || player.PlayerPawn?.Value is not { } pawn
                    || !TryGetCameraTarget(pawn, state, out Vector target))
                {
                    continue;
                }

                Vector current = state.Camera.AbsOrigin ?? target;
                state.Camera.Teleport(Lerp(current, target, SmoothFactor), pawn.V_angle, ZeroVelocity);
            }
        }

        private static bool TryGetCameraTarget(CCSPlayerPawn pawn, CameraState state, out Vector target)
        {
            target = null!;

            if (pawn.AbsOrigin is not { } origin
                || pawn.CBodyComponent?.SceneNode is not { } sceneNode
                || sceneNode.Handle == IntPtr.Zero)
            {
                return false;
            }

            // TraceTick 0 → always collide on first frame; then every N ticks.
            bool shouldTrace = state.TraceTick % TraceEveryNTicks == 0;
            state.TraceTick++;

            if (shouldTrace)
            {
                state.Scale = sceneNode.GetSkeletonInstance().Scale;
            }

            float yaw = pawn.V_angle.Y * MathF.PI / 180f;
            float elevation = Math.Clamp(pawn.V_angle.X, 0f, 90f) * MathF.PI / 180f;
            float h = MathF.Cos(elevation);
            float v = MathF.Sin(elevation);
            float backX = -MathF.Cos(yaw) * h;
            float backY = -MathF.Sin(yaw) * h;

            float startX = origin.X + (backX * BackwardOffset);
            float startY = origin.Y + (backY * BackwardOffset);
            float startZ = origin.Z + MathF.Min(HeightScale * state.Scale, MaxHeight) + (v * BackwardOffset);

            if (shouldTrace)
            {
                TraceResult trace = RayTrace.TraceLine(
                    new Vector(startX, startY, startZ),
                    new Vector(
                        startX + (backX * MaxDistance),
                        startY + (backY * MaxDistance),
                        startZ + (v * MaxDistance)),
                    pawn);

                state.BoomDistance = trace.DidHit()
                    ? Math.Clamp((trace.Fraction * MaxDistance) - 10f, 10f, MaxDistance)
                    : MaxDistance;
            }

            float distance = state.BoomDistance;
            target = new Vector(
                startX + (backX * distance),
                startY + (backY * distance),
                startZ + (v * distance));
            return true;
        }

        private static Vector Lerp(Vector from, Vector to, float t) => new(
            from.X + ((to.X - from.X) * t),
            from.Y + ((to.Y - from.Y) * t),
            from.Z + ((to.Z - from.Z) * t));
    }
}

namespace RollTheDice.Configs
{
    public class ThirdPersonViewConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
