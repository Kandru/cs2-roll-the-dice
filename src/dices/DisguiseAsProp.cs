using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class DisguiseAsProp : DiceBlueprint
    {
        public override string ClassName => "DisguiseAsProp";
        public override List<string> Listeners { get; } = ["OnTick"];
        private readonly Random _random = new();
        private readonly Dictionary<CCSPlayerController, PropState> _states = [];

        private static readonly Dictionary<string, (string Model, int OffsetZ, int OffsetAngle)> Models = new()
        {
            { "OfficePlant", ("models/props/cs_office/plant01.vmdl", 0, 0) },
            { "Barstool", ("models/generic/barstool_01/barstool_01.vmdl", 0, 0) },
            { "Table", ("models/props_c17/furnituretable001a_static.vmdl", 15, 0) },
            { "Hostage", ("models/hostage/hostage.vmdl", 0, 0) },
            { "Chicken", ("models/chicken/chicken.vmdl", 0, 0) },
            { "BigFlag", ("models/props_fairgrounds/fairgrounds_flagpole01.vmdl", 0, 180) },
            { "AnubisInfoPanel", ("models/anubis/signs/anubis_info_panel_01.vmdl", 0, 0) },
            { "CopyMachine", ("models/props_interiors/copymachine01.vmdl", 0, 270) },
            { "FileCabinet", ("models/props_office/file_cabinet_03.vmdl", 0, 0) },
            { "MailDropbox", ("models/props_street/mail_dropbox.vmdl", 0, 0) },
            { "Pottery", ("models/ar_shoots/shoots_pottery_02.vmdl", 0, 0) },
            { "TrashCan", ("models/props_interiors/trashcan01.vmdl", 0, 0) },
            { "Trafficcone", ("models/props/de_vertigo/trafficcone_clean.vmdl", 15, 0) },
            { "Fireextinguisher", ("models/generic/fire_extinguisher_01/fire_extinguisher_01.vmdl", 0, 0) },
        };
        private static readonly string[] ModelKeys = [.. Models.Keys];

        private sealed class PropState
        {
            public required string PropName { get; set; }
            public required CDynamicProp Prop { get; set; }
            public string Status { get; set; } = "player";
            public bool UseDown { get; set; }
            public int LastModelChange { get; set; }
            public int OffsetZ { get; set; }
            public int OffsetAngle { get; set; }
        }

        public DisguiseAsProp(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player) || player.Pawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }

            string key = ModelKeys[_random.Next(ModelKeys.Length)];
            (string model, int offsetZ, int offsetAngle) = Models[key];
            CDynamicProp? prop = Entities.CreatePropEntity(pawn.AbsOrigin!, pawn.AbsRotation!, model, 1f);
            if (prop == null)
            {
                return;
            }

            HideProp(prop);
            _players.Add(player);
            _states[player] = new PropState
            {
                PropName = key,
                Prop = prop,
                OffsetZ = offsetZ,
                OffsetAngle = offsetAngle,
                LastModelChange = (int)Server.CurrentTime,
            };
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "model", ModelLabel(key) }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_states.TryGetValue(player, out PropState? state))
            {
                Entities.RemoveEntity(state.Prop);
                SetPlayerVisible(player, true);
            }
            _ = _states.Remove(player);
            base.Remove(player, reason);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _states.Keys.ToArray())
            {
                Remove(player);
            }
            _states.Clear();
            base.Reset();
        }

        public void OnTick()
        {
            if (_states.Count == 0)
            {
                return;
            }

            bool allowChange = _config.Dices.DisguiseAsProp.AllowModelChange;
            int now = (int)Server.CurrentTime;
            foreach ((CCSPlayerController player, PropState state) in _states)
            {
                if (!IsAlivePlayer(player) || !state.Prop.IsValid)
                {
                    continue;
                }

                PlayerButtons buttons = player.Buttons;
                if (buttons == 0 && state.Status == "player")
                {
                    state.Status = "plant";
                    SetPlayerVisible(player, false);
                    ShowProp(player, state);
                    Banner.Show(player, StripChatColors(ModelLabel(state.PropName)));
                    continue;
                }

                if (allowChange
                    && buttons == PlayerButtons.Use
                    && state.Status == "plant"
                    && !state.UseDown
                    && state.LastModelChange <= now)
                {
                    state.UseDown = true;
                    CycleModel(player, state);
                    continue;
                }

                if (allowChange && buttons != PlayerButtons.Use && state.UseDown)
                {
                    state.UseDown = false;
                    state.LastModelChange = now + 1;
                    ShowProp(player, state);
                    continue;
                }

                if (buttons != 0 && !state.UseDown && state.Status == "plant")
                {
                    state.Status = "player";
                    SetPlayerVisible(player, true);
                    HideProp(state.Prop);
                    Banner.Show(player, StripChatColors(_localizer["dice_DisguiseAsProp_disabled"].Value));
                }
            }
        }

        private void CycleModel(CCSPlayerController player, PropState state)
        {
            int index = Array.IndexOf(ModelKeys, state.PropName);
            string next = ModelKeys[(index + 1) % ModelKeys.Length];
            (string model, int offsetZ, int offsetAngle) = Models[next];
            state.Prop.SetModel(model);
            state.PropName = next;
            state.OffsetZ = offsetZ;
            state.OffsetAngle = offsetAngle;
            string label = ModelLabel(next);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "model", label }
            });
            Banner.Show(player, StripChatColors(
                _localizer["dice_DisguiseAsProp_status"].Value.Replace("{model}", label)));
        }

        private void ShowProp(CCSPlayerController player, PropState state)
        {
            if (player.Pawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }
            state.Prop.Teleport(
                new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + state.OffsetZ),
                new QAngle(pawn.AbsRotation!.X, pawn.AbsRotation.Y + state.OffsetAngle, pawn.AbsRotation.Z)
            );
            state.Prop.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(state.Prop, "CBaseModelEntity", "m_clrRender");
        }

        private static void HideProp(CDynamicProp prop)
        {
            prop.Teleport(new Vector(-999, -999, -999));
            prop.Render = Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(prop, "CBaseModelEntity", "m_clrRender");
        }

        private string ModelLabel(string key)
        {
            string localeKey = $"model_{key.ToLowerInvariant()}";
            return _localizer[localeKey].ResourceNotFound ? key : _localizer[localeKey].Value;
        }

        private static void SetPlayerVisible(CCSPlayerController player, bool visible)
        {
            if (player.PlayerPawn?.Value is not { IsValid: true } pawn)
            {
                return;
            }
            int alpha = visible ? 255 : 0;
            pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            if (pawn.WeaponServices?.MyWeapons == null)
            {
                return;
            }
            foreach (CHandle<CBasePlayerWeapon> gun in pawn.WeaponServices.MyWeapons)
            {
                if (gun.Value is not { IsValid: true } weapon)
                {
                    continue;
                }
                weapon.Render = Color.FromArgb(alpha, 255, 255, 255);
                weapon.ShadowStrength = 0.0f;
                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class DisguiseAsPropConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("allow_model_change")] public bool AllowModelChange { get; set; } = true;
    }
}
