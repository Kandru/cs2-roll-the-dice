using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private void DebugPrint(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
            }
        }

        private int GetRandomDice()
        {
            // Filter enabled dices based on map-specific and global configuration
            var enabledDiceIndices = _dices
                .Select((dice, index) => new { dice, index })
                .Where(diceInfo =>
                {
                    var diceName = diceInfo.dice.Method.Name;
                    // Check map configuration for dices
                    if (Config.MapConfigs.TryGetValue(_currentMap, out MapConfig? mapConfig) && mapConfig.Dices.TryGetValue(diceName, out var diceConfig) && diceConfig is Dictionary<string, object> diceDict)
                    {
                        if (diceDict.TryGetValue("enabled", out var enabledValue))
                        {
                            if (bool.TryParse(enabledValue.ToString(), out var isEnabled))
                            {
                                return isEnabled;
                            }
                        }
                        return false;
                    }
                    // Check global configuration if no map-specific configuration is available
                    if (Config.Dices.TryGetValue(diceName, out diceConfig) && diceConfig is Dictionary<string, object> diceDict2)
                    {
                        if (diceDict2.TryGetValue("enabled", out var enabledValue))
                        {
                            if (bool.TryParse(enabledValue.ToString(), out var isEnabled))
                            {
                                return isEnabled;
                            }
                        }
                        return false;
                    }
                    // Default to enabled if not found in either configuration
                    return true;
                })
                .Select(diceInfo => diceInfo.index)
                .ToList();
            if (enabledDiceIndices.Count == 0) return -1;
            // subset of enabled dices from dice counter
            var countRolledDicesEnabled = _countRolledDices
                .Where(diceCounter => enabledDiceIndices.Contains(_dices.FindIndex(dice => dice.Method.Name == diceCounter.Key)))
                .ToDictionary(diceCounter => diceCounter.Key, diceCounter => diceCounter.Value);
            // get the lowest count from enabled dices
            var countRolledDicesLowestCount = countRolledDicesEnabled.Values.Min();
            var countRolledDicesLowest = countRolledDicesEnabled
                .Where(diceCounter => diceCounter.Value == countRolledDicesLowestCount)
                .Select(diceCounter => _dices.FindIndex(dice => dice.Method.Name == diceCounter.Key))
                .ToList();
            // Get random dice from lowest used enabled dices
            return countRolledDicesLowest[_random.Next(countRolledDicesLowest.Count)];
        }

        private void SendGlobalChatMessage(string message, float delay = 0, CCSPlayerController? player = null)
        {
            DebugPrint(message);
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                if (entry == null || !entry.IsValid || entry.IsBot || entry == player) continue;
                if (delay > 0)
                    AddTimer(delay, () =>
                    {
                        if (entry == null || !entry.IsValid) return;
                        entry.PrintToChat(message);
                    });
                else
                    entry.PrintToChat(message);
            }
        }

        private void ChangeGameRule(string rule, object value)
        {
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                var gameRules = ent.GameRules;
                if (gameRules == null) continue;

                var property = gameRules.GetType().GetProperty(rule);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(gameRules, Convert.ChangeType(value, property.PropertyType));
                }
            }
        }

        private object? GetGameRule(string rule)
        {
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                var gameRules = ent.GameRules;
                if (gameRules == null) continue;

                var property = gameRules.GetType().GetProperty(rule);
                if (property != null && property.CanRead)
                {
                    return property.GetValue(gameRules);
                }
            }
            return null;
        }

        private int SpawnProp(CCSPlayerController player, string model, float scale = 1.0f, bool setParent = false, bool animated = false)
        {
            // sanity checks
            if (player == null
            || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
            || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return -1;
            // create dynamic prop
            CDynamicProp prop;
            prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override")!;
            // set attributes
            prop.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            prop.Collision.SolidType = SolidType_t.SOLID_NONE;
            prop.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
            prop.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
            // spawn it
            prop.DispatchSpawn();
            prop.SetModel(model);
            if (setParent)
                // follow player
                prop.AcceptInput("SetParent", player.PlayerPawn.Value, player.PlayerPawn.Value, "!activator");
            prop.Teleport(player.PlayerPawn.Value.AbsOrigin, player.PlayerPawn.Value.AbsRotation);
            prop.AnimGraphUpdateEnabled = animated;
            prop.CBodyComponent!.SceneNode!.Scale = scale;
            return (int)prop.Index;
        }

        private void UpdateProp(CCSPlayerController player, int index, int offset_z = 0, int offset_angle = 0)
        {
            var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)index);
            // sanity checks
            if (prop == null
            || player == null
            || player.Pawn == null
            || player.Pawn.Value == null
            || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
            || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            // get player pawn
            var playerPawn = player!.PlayerPawn!.Value;
            // teleport it to player
            Vector playerOrigin = new Vector(
                (float)Math.Round(playerPawn.AbsOrigin!.X, 5),
                (float)Math.Round(playerPawn.AbsOrigin!.Y, 5),
                (float)Math.Round(playerPawn.AbsOrigin!.Z, 5) + offset_z
            );
            Vector propOrigin = new Vector(
                (float)Math.Round(prop.AbsOrigin!.X, 5),
                (float)Math.Round(prop.AbsOrigin!.Y, 5),
                (float)Math.Round(prop.AbsOrigin!.Z, 5)
            );
            QAngle playerRotation = new QAngle(
                0,
                (float)Math.Round(playerPawn.AbsRotation!.Y, 5) + offset_angle,
                0
            );
            QAngle propRotation = new QAngle(
                0,
                (float)Math.Round(prop.AbsRotation!.Y, 5),
                0
            );
            if (playerOrigin.X == propOrigin.X
                && playerOrigin.Y == propOrigin.Y
                && playerOrigin.Z == propOrigin.Z
                && playerRotation.Y == propRotation.Y) return;
            prop.Teleport(playerOrigin, playerRotation);
        }

        private void RemoveProp(int index, bool softRemove = false)
        {
            var prop = Utilities.GetEntityFromIndex<CDynamicProp>((int)index);
            // remove plant entity
            if (prop == null || !prop.IsValid)
                return;
            if (softRemove)
                prop.Teleport(new Vector(-999, -999, -999));
            else
                prop.Remove();
        }

        private void MakePlayerInvisible(CCSPlayerController player)
        {
            // sanity checks
            if (player == null
            || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
            || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            // get player pawn
            var playerPawn = player!.PlayerPawn!.Value;
            playerPawn.Render = Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            if (playerPawn.WeaponServices?.MyWeapons != null)
            {
                foreach (var gun in playerPawn.WeaponServices!.MyWeapons)
                {
                    var weapon = gun.Value;
                    if (weapon != null)
                    {
                        weapon.Render = Color.FromArgb(0, 255, 255, 255);
                        weapon.ShadowStrength = 0.0f;
                        Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                    }
                }
            }
        }

        private void MakePlayerVisible(CCSPlayerController player)
        {
            // sanity checks
            if (player == null
            || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null) return;
            // get player pawn
            var playerPawn = player!.PlayerPawn!.Value;
            playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
            if (playerPawn.WeaponServices!.MyWeapons != null)
            {
                foreach (var gun in playerPawn.WeaponServices!.MyWeapons)
                {
                    var weapon = gun.Value;
                    if (weapon != null)
                    {
                        weapon.Render = Color.FromArgb(255, 255, 255, 255);
                        weapon.ShadowStrength = 0.0f;
                        Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                    }
                }
            }
        }

        private static string GetPlayerModel(CCSPlayerPawn playerPawn)
        {
            return playerPawn.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName ?? string.Empty;
        }

        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        public static float CalculateDistance(Vector point1, Vector point2)
        {
            float dx = point2.X - point1.X;
            float dy = point2.Y - point1.Y;
            float dz = point2.Z - point1.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        public static Vector CalculatePositionInFront(CCSPlayerController player, float offSetXY, float offSetZ = 0)
        {
            var pawn = player.PlayerPawn.Value;
            // Extract yaw angle from player's rotation QAngle
            float yawAngle = pawn!.EyeAngles!.Y;

            // Convert yaw angle from degrees to radians
            float yawAngleRadians = (float)(yawAngle * Math.PI / 180.0);

            // Calculate offsets in x and y directions
            float offsetX = offSetXY * (float)Math.Cos(yawAngleRadians);
            float offsetY = offSetXY * (float)Math.Sin(yawAngleRadians);

            // Calculate position in front of the player
            var positionInFront = new Vector
            {
                X = pawn!.AbsOrigin!.X + offsetX,
                Y = pawn!.AbsOrigin!.Y + offsetY,
                Z = pawn!.AbsOrigin!.Z + offSetZ
            };

            return positionInFront;
        }


        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        public static Vector CalculateVelocity(Vector positionA, Vector positionB, float timeDuration)
        {
            // Step 1: Determine direction from A to B
            Vector directionVector = positionB - positionA;
            // Step 2: Calculate distance between A and B
            float distance = directionVector.Length();
            // Step 3: Choose a desired time duration for the movement
            // Ensure that timeDuration is not zero to avoid division by zero
            if (timeDuration == 0)
            {
                timeDuration = 1;
            }
            // Step 4: Calculate velocity magnitude based on distance and time
            float velocityMagnitude = distance / timeDuration;
            // Step 5: Normalize direction vector
            if (distance != 0)
            {
                directionVector /= distance;
            }
            // Step 6: Scale direction vector by velocity magnitude to get velocity vector
            Vector velocityVector = directionVector * velocityMagnitude;
            return velocityVector;
        }


        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        public static QAngle MoveTowardsAngle(QAngle angle, QAngle targetAngle, float baseStepSize)
        {
            return new QAngle(
                MoveTowards(angle.X, targetAngle.X, baseStepSize),
                MoveTowards(angle.Y, targetAngle.Y, baseStepSize),
                0
            );
        }

        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        private static float MoveTowards(float current, float target, float baseStepSize)
        {
            // Normalize angles to the range [-180, 180]
            current = NormalizeAngle(current);
            target = NormalizeAngle(target);

            // Calculate the shortest direction to rotate
            float delta = target - current;

            // Ensure the shortest path is taken by adjusting delta
            if (delta > 180)
                delta -= 360;
            else if (delta < -180)
                delta += 360;

            // Dynamically adjust the step size based on the magnitude of the delta
            float dynamicStepSize = Math.Min(baseStepSize * Math.Abs(delta) / 180f, Math.Abs(delta));

            // Clamp the delta to the dynamicStepSize
            if (Math.Abs(delta) <= dynamicStepSize)
            {
                return target; // We have reached the target
            }

            // Move towards the target
            return NormalizeAngle(current + Math.Sign(delta) * dynamicStepSize);
        }

        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        private static float NormalizeAngle(float angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }

        // thx to https://github.com/grrhn/ThirdPerson-WIP/ (license GPLv3)
        public static void UpdateCamera(CDynamicProp _cameraProp, CCSPlayerController target)
        {
            _cameraProp.Teleport(
                CalculatePositionInFront(target, -110, 90),
                MoveTowardsAngle(_cameraProp.AbsRotation!, target.PlayerPawn.Value!.V_angle, 64),
                CalculateVelocity(_cameraProp.AbsOrigin!, CalculatePositionInFront(target, -110, 90), 0.1f)
            );
        }

        private void RollTheDiceForPlayer(CCSPlayerController player)
        {
            if (player.IsBot
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                || _playersThatRolledTheDice.ContainsKey(player)) return;
            var diceIndex = GetRandomDice();
            if (diceIndex == -1)
            {
                player.PrintToChat(Localizer["command.givedice.nodicefound"]);
                return;
            }
            // add player to list
            _playersThatRolledTheDice.Add(player, new Dictionary<string, object> { { "dice", _dices[diceIndex].Method.Name } });
            // count dice roll
            _countRolledDices[_dices[diceIndex].Method.Name]++;
            // execute
            ExecuteDice(player, diceIndex);
        }

        private void RollTheDiceEveryXSeconds(int seconds)
        {
            AddTimer(seconds, () =>
            {
                if (!_isDuringRound) return;
                foreach (var player in Utilities.GetPlayers())
                {
                    ResetDiceForPlayer(player);
                    RollTheDiceForPlayer(player);
                }
                RollTheDiceEveryXSeconds(seconds);
            });
        }
    }
}