using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Hud
{
    public enum ReelSource
    {
        Rtd,
        Auto,
        Admin,
    }

    public enum ReelTone
    {
        Good,
        Bad,
        Neutral,
    }

    /// <summary>
    /// Slot-machine roll animation on the <c>Reel</c> panel of banner.xml. Purely visual: the dice is already
    /// rolled and applied when <see cref="Begin"/> runs; the reel only shows it. The strip animation runs on the
    /// client (keyframes <c>rr-spin</c>); the server writes the winner into the fixed row 20, starts the animation
    /// with one class swap and plays the tick sounds on the same ease-out curve. While a reel spins, the
    /// player's banner, chat line and dice sound are held back and released when it stops.
    /// </summary>
    public static class Reel
    {
        public const string Panel = "Reel";
        public const string TitleVar = "rr_title";
        public const string SourceVar = "rr_source";
        public const string WinVar = "rr_win";
        public const string CreditVar = "rr_credit";
        public const string FillerVarPrefix = "rr_f";
        public const string RowIdPrefix = "ReelRow";
        public const int RowCount = 22;
        public const int WinRow = 20;

        private const string CreditText = "made by: kandru.net";
        private const float OpenDelaySeconds = 0.2f;
        private const float StaggerSeconds = 0.05f;
        private const float RowHeight = 44f;
        private const float OvershootPx = 850f;
        private const float OvershootAt = 0.86f;
        private const float TickMinGap = 0.04f;
        private const int MaxSlots = 65;
        private const int AuthoredSpin = 2;               // spin-20
        private const int AuthoredWin = (int)ReelTone.Neutral;
        private static readonly TimeSpan FadeGrace = TimeSpan.FromSeconds(0.25);

        private static readonly string[] Tones = ["tone-good", "tone-bad", "tone-neutral"];
        private static readonly string[] WinTones = ["win-good", "win-bad", "win-neutral"];
        private static readonly string[] Replays = ["replay-0", "replay-1"];
        private static readonly string[] SpinClasses = BuildSteps("spin-", 10, 50);
        private static readonly string[] HoldClasses = BuildSteps("hold-", 5, 50);
        private static readonly string[] RowIds = BuildRows(RowIdPrefix);
        private static readonly string[] FillerVars = BuildRows(FillerVarPrefix);

        /// <summary>Default tone per dice class; config <c>reel.tones</c> overrides single entries.</summary>
        public static readonly IReadOnlyDictionary<string, ReelTone> DefaultTones = new Dictionary<string, ReelTone>(StringComparer.Ordinal)
        {
            ["Bankrupt"] = ReelTone.Bad,
            ["BigTaserBattery"] = ReelTone.Good,
            ["ChangeName"] = ReelTone.Neutral,
            ["ChangePlayerModel"] = ReelTone.Good,
            ["ChangePlayerSize"] = ReelTone.Neutral,
            ["ChickenLeader"] = ReelTone.Neutral,
            ["Cloak"] = ReelTone.Good,
            ["Cutter"] = ReelTone.Good,
            ["DamageMultiplier"] = ReelTone.Neutral,
            ["Deaf"] = ReelTone.Bad,
            ["DecreaseHealth"] = ReelTone.Bad,
            ["DecreaseMoney"] = ReelTone.Bad,
            ["DisguiseAsProp"] = ReelTone.Good,
            ["FastMapAction"] = ReelTone.Good,
            ["FogOfWar"] = ReelTone.Bad,
            ["GiveHealthShot"] = ReelTone.Good,
            ["Glow"] = ReelTone.Bad,
            ["HeadshotOnly"] = ReelTone.Bad,
            ["HighGravity"] = ReelTone.Bad,
            ["IncreaseHealth"] = ReelTone.Good,
            ["IncreaseMoney"] = ReelTone.Good,
            ["IncreaseSpeed"] = ReelTone.Good,
            ["InfiniteAmmo"] = ReelTone.Good,
            ["Invisible"] = ReelTone.Good,
            ["Jammer"] = ReelTone.Bad,
            ["LongerFlashes"] = ReelTone.Good,
            ["LoudSteps"] = ReelTone.Bad,
            ["LowGravity"] = ReelTone.Good,
            ["MakeFakeGunSounds"] = ReelTone.Neutral,
            ["MakeHostageSounds"] = ReelTone.Bad,
            ["Millionaire"] = ReelTone.Good,
            ["NoExplosives"] = ReelTone.Bad,
            ["NoHeadshot"] = ReelTone.Bad,
            ["NoRecoil"] = ReelTone.Good,
            ["OneHP"] = ReelTone.Bad,
            ["PlayAsChicken"] = ReelTone.Neutral,
            ["PlayerHealthBar"] = ReelTone.Good,
            ["RandomWeapon"] = ReelTone.Neutral,
            ["ResetOnReload"] = ReelTone.Bad,
            ["Respawn"] = ReelTone.Good,
            ["ReturnToSender"] = ReelTone.Good,
            ["StripWeapons"] = ReelTone.Bad,
            ["Suicide"] = ReelTone.Bad,
            ["ThirdPersonView"] = ReelTone.Neutral,
            ["Vampire"] = ReelTone.Good,
            ["WeirdGrenades"] = ReelTone.Neutral,
            ["ZoomIn"] = ReelTone.Neutral,
            ["ZoomOut"] = ReelTone.Neutral,
        };

        private enum Phase : byte
        {
            Idle,
            Waiting,
            Spinning,
            Holding,
        }

        // config
        private static bool _enabled = true;
        private static float _spinSeconds = 2f;
        private static float _holdSeconds = 1.5f;
        private static int _spinClass = AuthoredSpin;
        private static int _holdClass = 2;
        private static string _tickSound = string.Empty;
        private static float[] _ticks = BuildTicks(2f);
        private static Dictionary<string, string> _toneOverrides = [];

        // dice pool of the current map
        private static string[] _poolClass = [];
        private static string[] _poolLabel = [];
        private static int[] _poolTone = [];
        private static string _titleRolling = string.Empty;
        private static string _titleRolled = string.Empty;
        private static string[] _sourceLabels = [string.Empty, string.Empty, string.Empty];

        // per owner (the player who rolled)
        private static readonly Phase[] _phase = new Phase[MaxSlots];
        private static readonly float[] _openAt = new float[MaxSlots];
        private static readonly float[] _stopAt = new float[MaxSlots];
        private static readonly float[] _hideAt = new float[MaxSlots];
        private static readonly int[] _tickIndex = new int[MaxSlots];
        private static readonly int[] _dice = new int[MaxSlots];
        private static readonly int[] _source = new int[MaxSlots];
        private static readonly List<int>?[] _viewers = new List<int>?[MaxSlots];
        private static readonly string?[] _pendingBanner = new string?[MaxSlots];
        private static readonly List<string>?[] _pendingChat = new List<string>?[MaxSlots];
        private static readonly Action?[] _pendingSound = new Action?[MaxSlots];
        private static int _active;

        // per viewer (owner or spectator): last written class of each group, -1 = none
        private static readonly int[] _viewerOwner = Filled(-1);
        private static readonly bool[] _touched = new bool[MaxSlots];
        private static readonly int[] _spinIdx = Filled(AuthoredSpin);
        private static readonly int[] _holdIdx = Filled(-1);
        private static readonly int[] _winIdx = Filled(AuthoredWin);
        private static readonly int[] _replayIdx = Filled(-1);

        // global filler strip (same for every viewer, rewritten once per round)
        private static readonly int[] _rowTone = FilledRows((int)ReelTone.Neutral);
        private static bool _fillerWritten;
        private static readonly Random _random = new();
        private static int _staggerTick = -1;
        private static int _staggerCount;

        public static void BindConfig(PluginConfig config)
        {
            ReelConfig reel = config.Reel ?? new ReelConfig();
            _enabled = reel.Enabled && config.NotifyPlayerViaPanorama;
            _spinClass = StepIndex(reel.SpinSeconds, 1.0f, SpinClasses.Length);
            _holdClass = StepIndex(reel.HoldSeconds, 0.5f, HoldClasses.Length);
            _spinSeconds = 1.0f + (_spinClass * 0.5f);
            _holdSeconds = 0.5f + (_holdClass * 0.5f);
            _ticks = BuildTicks(_spinSeconds);
            _tickSound = reel.TickSound ?? string.Empty;
            _toneOverrides = reel.Tones ?? [];
        }

        /// <summary>Enabled dice of the current map (filler names for the strip) and the localized labels.</summary>
        public static void SetPool(IReadOnlyList<string> classNames, IStringLocalizer localizer)
        {
            _poolClass = [.. classNames];
            _poolLabel = new string[_poolClass.Length];
            _poolTone = new int[_poolClass.Length];
            for (int i = 0; i < _poolClass.Length; i++)
            {
                string name = _poolClass[i];
                LocalizedString label = localizer[$"dice_{name}_reel"];
                _poolLabel[i] = label.ResourceNotFound ? name : label.Value;
                _poolTone[i] = (int)ResolveTone(name);
            }

            _titleRolling = localizer["reel.title.rolling"].Value;
            _titleRolled = localizer["reel.title.rolled"].Value;
            _sourceLabels =
            [
                localizer["reel.source.rtd"].Value,
                localizer["reel.source.auto"].Value,
                localizer["reel.source.admin"].Value,
            ];
        }

        /// <summary>
        /// Starts the reel for a dice that was just rolled. Call before the dice is applied so its notifications
        /// are held back. Returns false (and changes nothing) when no reel is shown, e.g. for bots.
        /// </summary>
        public static bool Begin(CCSPlayerController player, string className, ReelSource source)
        {
            if (!_enabled
                || _poolClass.Length == 0
                || !CustomHud.IsHumanViewer(player)
                || !player.PawnIsAlive
                || player.Slot is < 0 or >= MaxSlots
                || !CustomHud.EnsureSpawned(player))
            {
                return false;
            }

            WriteFiller();
            int slot = player.Slot;
            if (_phase[slot] == Phase.Idle)
            {
                _active++;
            }
            else
            {
                HideViewers(slot);
            }

            ClearPending(slot);
            _dice[slot] = PoolIndex(className);
            _source[slot] = (int)source;
            _phase[slot] = Phase.Waiting;
            _openAt[slot] = Server.CurrentTime + OpenDelaySeconds + NextStagger();
            return true;
        }

        public static bool IsPending(CCSPlayerController? player) =>
            player is { IsValid: true }
            && player.Slot is >= 0 and < MaxSlots
            && _phase[player.Slot] is Phase.Waiting or Phase.Spinning;

        /// <summary>Holds the banner text back while the reel spins (no center message - the reel replaces it).</summary>
        public static bool TryDeferBanner(CCSPlayerController player, string text)
        {
            if (!IsPending(player))
            {
                return false;
            }
            _pendingBanner[player.Slot] = text;
            return true;
        }

        public static bool TryDeferChat(CCSPlayerController player, string message)
        {
            if (!IsPending(player))
            {
                return false;
            }
            (_pendingChat[player.Slot] ??= []).Add(message);
            return true;
        }

        public static bool TryDeferSound(CCSPlayerController player, Action play)
        {
            if (!IsPending(player))
            {
                return false;
            }
            _pendingSound[player.Slot] = play;
            return true;
        }

        public static void OnTick()
        {
            if (_active == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                switch (_phase[slot])
                {
                    case Phase.Waiting:
                        if (now >= _openAt[slot])
                        {
                            Open(slot, now);
                        }
                        break;
                    case Phase.Spinning:
                        PlayDueTicks(slot, now);
                        if (now >= _stopAt[slot])
                        {
                            Stop(slot, now);
                        }
                        break;
                    case Phase.Holding:
                        if (now >= _hideAt[slot])
                        {
                            Finish(slot);
                        }
                        break;
                    case Phase.Idle:
                    default:
                        break;
                }
            }
        }

        /// <summary>Owner died or lost the dice: hide the reel now and drop what was held back.</summary>
        public static void Cancel(CCSPlayerController? player)
        {
            if (player is not { IsValid: true } || player.Slot is < 0 or >= MaxSlots)
            {
                return;
            }
            Cancel(player.Slot, hide: true);
        }

        public static void Forget(CCSPlayerController? player)
        {
            if (player is not { IsValid: true } || player.Slot is < 0 or >= MaxSlots)
            {
                return;
            }

            int slot = player.Slot;
            Cancel(slot, hide: false);
            int owner = _viewerOwner[slot];
            if (owner >= 0)
            {
                _ = _viewers[owner]?.Remove(slot);
            }
            ResetViewer(slot);
        }

        /// <summary>Called on round start/end before the defaults are written back.</summary>
        public static void CancelAll()
        {
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                Cancel(slot, hide: false);
            }
        }

        public static void Shutdown()
        {
            CancelAll();
            OnLayoutReset();
        }

        /// <summary>A new layout entity starts at the XML defaults: forget everything written so far.</summary>
        public static void OnLayoutReset()
        {
            for (int v = 0; v < MaxSlots; v++)
            {
                ResetViewer(v);
            }
            Array.Fill(_rowTone, (int)ReelTone.Neutral);
            _fillerWritten = false;
        }

        /// <summary>Round restart rebuilds the client layout from XML: write the XML defaults back (touched viewers only).</summary>
        public static void WriteDefaults(CCSPlayerController player)
        {
            int v = player.Slot;
            if (v is < 0 or >= MaxSlots || !_touched[v])
            {
                return;
            }

            CustomHud.SetText(player, Panel, TitleVar, string.Empty);
            CustomHud.SetText(player, Panel, SourceVar, string.Empty);
            CustomHud.SetText(player, Panel, WinVar, string.Empty);
            CustomHud.SetHasClass(player, Panel, "ph-fading", false);
            CustomHud.SetHasClass(player, Panel, "ph-off", true);
            CustomHud.SetHasClass(player, Panel, "is-stopped", false);
            CustomHud.SetHasClass(player, Panel, "is-running", false);
            Swap(player, SpinClasses, ref _spinIdx[v], AuthoredSpin);
            Swap(player, HoldClasses, ref _holdIdx[v], -1);
            Swap(player, WinTones, ref _winIdx[v], AuthoredWin);
            Swap(player, Replays, ref _replayIdx[v], -1);
            HudTheme.WriteDefault(player, Panel);
            _touched[v] = false;
        }

        /// <summary>Global part of the defaults: empty filler strip, tone-neutral rows. Rewritten on the next roll.</summary>
        public static void WriteGlobalDefaults()
        {
            if (!_fillerWritten || !CustomHud.HasLayout())
            {
                return;
            }

            for (int i = 0; i < RowCount; i++)
            {
                if (i == WinRow)
                {
                    continue;
                }
                CustomHud.SetGlobalText(Panel, FillerVars[i], string.Empty);
                SwapGlobal(i, (int)ReelTone.Neutral);
            }
            CustomHud.SetGlobalText(Panel, CreditVar, string.Empty);
            _fillerWritten = false;
        }

        private static void Open(int slot, float now)
        {
            if (Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } owner || !owner.PawnIsAlive)
            {
                Cancel(slot, hide: true);
                return;
            }

            List<int> viewers = _viewers[slot] ??= [];
            viewers.Clear();
            foreach (CCSPlayerController viewer in Utilities.GetPlayers())
            {
                if (!CustomHud.IsHumanViewer(viewer)
                    || (viewer.Slot != slot && Banner.GetSpectatedOwner(viewer)?.Slot != slot))
                {
                    continue;
                }

                if (Present(viewer, slot))
                {
                    viewers.Add(viewer.Slot);
                }
            }

            _phase[slot] = Phase.Spinning;
            _tickIndex[slot] = 0;
            _openAt[slot] = now;
            _stopAt[slot] = now + _spinSeconds;
        }

        private static bool Present(CCSPlayerController viewer, int owner)
        {
            int v = viewer.Slot;
            if (v is < 0 or >= MaxSlots || !CustomHud.EnsureSpawned(viewer))
            {
                return false;
            }

            int previous = _viewerOwner[v];
            if (previous >= 0 && previous != owner)
            {
                _ = _viewers[previous]?.Remove(v);
            }
            _viewerOwner[v] = owner;
            _touched[v] = true;

            int dice = _dice[owner];
            HudTheme.ApplyTo(viewer, Panel);
            CustomHud.SetText(viewer, Panel, TitleVar, _titleRolling);
            CustomHud.SetText(viewer, Panel, SourceVar, _sourceLabels[_source[owner]]);
            CustomHud.SetText(viewer, Panel, WinVar, _poolLabel[dice]);
            CustomHud.SetHasClass(viewer, Panel, "is-stopped", false);
            CustomHud.SetHasClass(viewer, Panel, "is-running", false);
            Swap(viewer, WinTones, ref _winIdx[v], _poolTone[dice]);
            Swap(viewer, HoldClasses, ref _holdIdx[v], _holdClass);
            Swap(viewer, SpinClasses, ref _spinIdx[v], _spinClass);
            // alternating replay-0/1 restarts the keyframes on every roll
            Swap(viewer, Replays, ref _replayIdx[v], _replayIdx[v] == 0 ? 1 : 0);
            return CustomHud.ShowPanel(viewer, Panel);
        }

        private static void PlayDueTicks(int slot, float now)
        {
            float[] ticks = _ticks;
            int i = _tickIndex[slot];
            float elapsed = now - _openAt[slot];
            if (i >= ticks.Length || elapsed < ticks[i])
            {
                return;
            }

            while (i < ticks.Length && elapsed >= ticks[i])
            {
                i++;
            }
            _tickIndex[slot] = i;

            if (_tickSound.Length > 0 && Utilities.GetPlayerFromSlot(slot) is { IsValid: true } owner)
            {
                PlaySound(owner, _tickSound);
            }
        }

        private static void Stop(int slot, float now)
        {
            _phase[slot] = Phase.Holding;
            _hideAt[slot] = now + _holdSeconds;

            List<int>? viewers = _viewers[slot];
            if (viewers != null)
            {
                foreach (int v in viewers)
                {
                    if (_viewerOwner[v] != slot || Utilities.GetPlayerFromSlot(v) is not { IsValid: true } viewer)
                    {
                        continue;
                    }
                    CustomHud.SetHasClass(viewer, Panel, "is-stopped", true);
                    CustomHud.SetText(viewer, Panel, TitleVar, _titleRolled);
                    // hold-N was set on open, so this starts the drain transition
                    CustomHud.SetHasClass(viewer, Panel, "is-running", true);
                }
            }

            ReleasePending(slot);
        }

        private static void Finish(int slot)
        {
            HideViewers(slot);
            _phase[slot] = Phase.Idle;
            _active--;
        }

        private static void Cancel(int slot, bool hide)
        {
            if (_phase[slot] == Phase.Idle)
            {
                return;
            }

            if (hide)
            {
                HideViewers(slot);
            }
            else
            {
                DetachViewers(slot);
            }
            ClearPending(slot);
            _phase[slot] = Phase.Idle;
            _active--;
        }

        private static void HideViewers(int slot)
        {
            List<int>? viewers = _viewers[slot];
            if (viewers == null)
            {
                return;
            }

            foreach (int v in viewers)
            {
                if (_viewerOwner[v] != slot)
                {
                    continue;
                }
                _viewerOwner[v] = -1;
                if (Utilities.GetPlayerFromSlot(v) is { IsValid: true } viewer)
                {
                    CustomHud.HidePanel(viewer, Panel, FadeGrace);
                }
            }
            viewers.Clear();
        }

        private static void DetachViewers(int slot)
        {
            List<int>? viewers = _viewers[slot];
            if (viewers == null)
            {
                return;
            }

            foreach (int v in viewers)
            {
                if (_viewerOwner[v] == slot)
                {
                    _viewerOwner[v] = -1;
                }
            }
            viewers.Clear();
        }

        private static void ReleasePending(int slot)
        {
            string? banner = _pendingBanner[slot];
            List<string>? chat = _pendingChat[slot];
            Action? sound = _pendingSound[slot];
            _pendingBanner[slot] = null;
            _pendingSound[slot] = null;

            if (Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } owner)
            {
                chat?.Clear();
                return;
            }

            if (banner != null)
            {
                Banner.Show(owner, banner);
            }

            if (chat != null)
            {
                foreach (string message in chat)
                {
                    owner.PrintToChat(message);
                }
                chat.Clear();
            }

            try
            {
                sound?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice][Reel] Sound failed: {ex.Message}");
            }
        }

        private static void ClearPending(int slot)
        {
            _pendingBanner[slot] = null;
            _pendingChat[slot]?.Clear();
            _pendingSound[slot] = null;
        }

        /// <summary>Filler names and tones for every row but the winner row, written once per round for all viewers.</summary>
        private static void WriteFiller()
        {
            if (_fillerWritten || _poolClass.Length == 0 || !CustomHud.HasLayout())
            {
                return;
            }

            int[] order = new int[_poolClass.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }
            _random.Shuffle(order);

            int next = 0;
            for (int row = 0; row < RowCount; row++)
            {
                if (row == WinRow)
                {
                    continue;
                }
                int dice = order[next++ % order.Length];
                CustomHud.SetGlobalText(Panel, FillerVars[row], _poolLabel[dice]);
                SwapGlobal(row, _poolTone[dice]);
            }
            CustomHud.SetGlobalText(Panel, CreditVar, CreditText);
            _fillerWritten = true;
        }

        private static void Swap(CCSPlayerController viewer, string[] classes, ref int current, int next)
        {
            if (current == next)
            {
                return;
            }
            if (current >= 0)
            {
                CustomHud.SetHasClass(viewer, Panel, classes[current], false);
            }
            if (next >= 0)
            {
                CustomHud.SetHasClass(viewer, Panel, classes[next], true);
            }
            current = next;
        }

        private static void SwapGlobal(int row, int tone)
        {
            int current = _rowTone[row];
            if (current == tone)
            {
                return;
            }
            CustomHud.SetGlobalClass(RowIds[row], Tones[current], false);
            CustomHud.SetGlobalClass(RowIds[row], Tones[tone], true);
            _rowTone[row] = tone;
        }

        private static void ResetViewer(int v)
        {
            _viewerOwner[v] = -1;
            _touched[v] = false;
            _spinIdx[v] = AuthoredSpin;
            _holdIdx[v] = -1;
            _winIdx[v] = AuthoredWin;
            _replayIdx[v] = -1;
        }

        internal static void PlaySound(CCSPlayerController player, string sound)
        {
            if (sound.StartsWith("sounds/", StringComparison.Ordinal))
            {
                player.ExecuteClientCommand($"play {sound}");
                return;
            }

            RecipientFilter filter = [player];
            _ = player.EmitSound(sound, filter);
        }

        private static float NextStagger()
        {
            int tick = Server.TickCount;
            if (tick != _staggerTick)
            {
                _staggerTick = tick;
                _staggerCount = 0;
                return 0f;
            }
            return ++_staggerCount * StaggerSeconds;
        }

        private static int PoolIndex(string className)
        {
            for (int i = 0; i < _poolClass.Length; i++)
            {
                if (string.Equals(_poolClass[i], className, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return 0;
        }

        private static ReelTone ResolveTone(string className)
        {
            if (_toneOverrides.TryGetValue(className, out string? value)
                && Enum.TryParse(value, ignoreCase: true, out ReelTone tone)
                && Enum.IsDefined(tone))
            {
                return tone;
            }
            return DefaultTones.TryGetValue(className, out ReelTone fallback) ? fallback : ReelTone.Neutral;
        }

        /// <summary>Times (s after open) when a row boundary passes the win line on the ease-out curve of rr-spin.</summary>
        private static float[] BuildTicks(float spinSeconds)
        {
            List<float> ticks = [];
            float last = -1f;
            float tickInterval = 1f / 64f;
            for (int k = 1; k < WinRow; k++)
            {
                float distance = (k * RowHeight) - (RowHeight / 2f);
                float t = OvershootAt * spinSeconds * EaseOutTime(distance / OvershootPx);
                t = MathF.Round(t / tickInterval) * tickInterval;
                if (t - last < TickMinGap)
                {
                    continue;
                }
                ticks.Add(t);
                last = t;
            }
            return [.. ticks];
        }

        /// <summary>Inverse of cubic-bezier(0, 0, 0.58, 1): time fraction for a progress fraction.</summary>
        private static float EaseOutTime(float progress)
        {
            static float Bezier(float s, float p1, float p2) =>
                (3f * (1f - s) * (1f - s) * s * p1) + (3f * (1f - s) * s * s * p2) + (s * s * s);

            float lo = 0f;
            float hi = 1f;
            for (int i = 0; i < 32; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Bezier(mid, 0f, 1f) < progress)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }
            return Bezier((lo + hi) / 2f, 0f, 0.58f);
        }

        private static int StepIndex(float seconds, float min, int count)
        {
            int index = (int)MathF.Round((seconds - min) / 0.5f);
            return Math.Clamp(index, 0, count - 1);
        }

        private static string[] BuildSteps(string prefix, int fromTenths, int toTenths)
        {
            List<string> classes = [];
            for (int value = fromTenths; value <= toTenths; value += 5)
            {
                classes.Add($"{prefix}{value:00}");
            }
            return [.. classes];
        }

        private static string[] BuildRows(string prefix)
        {
            string[] names = new string[RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                names[i] = prefix + i;
            }
            return names;
        }

        private static int[] Filled(int value)
        {
            int[] values = new int[MaxSlots];
            Array.Fill(values, value);
            return values;
        }

        private static int[] FilledRows(int value)
        {
            int[] values = new int[RowCount];
            Array.Fill(values, value);
            return values;
        }
    }
}
