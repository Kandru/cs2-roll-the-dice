using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    internal static class WorldTextManager
    {
        // Constants for magic numbers and strings
        private const float DefaultWorldUnitsPerPxFactor = 0.01f;
        private const float PlayerWorldUnitsPerPxBaseFactor = 0.25f;
        private const float PlayerWorldUnitsPerPxDivisor = 1050f;
        private const string SchemaViewModelServices = "CCSPlayer_ViewModelServices";
        private const string SchemaMemberViewModel = "m_hViewModel";
        private const string PredictedViewModelName = "predicted_viewmodel";
        private const string SchemaPawnViewModelServicesClass = "CCSPlayerPawnBase";
        private const string SchemaMemberPawnViewModelServices = "m_pViewModelServices";
        private const float PlayerForwardOffsetMultiplier = 7f;
        private const float PlayerRotationOffsetY = 270f;
        private const float PlayerRotationOffsetZBase = 90f;
        private const string WorldTextEntityName = "point_worldtext";
        private const string SetParentInputName = "SetParent";
        private const string ActivatorKeyword = "!activator";

        private const float DefaultBackgroundHeightFactorPerLine = 0.01f;
        private const float DefaultBackgroundCharWidthFactor = 0.004f;
        private const float DefaultBackgroundMinWidthFactor = 0.05f;

        internal static CPointWorldText? Create(
            CCSPlayerController? player,
            string message,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f,
            float shiftY = 0f,
            PointWorldTextJustifyHorizontal_t horizontalAlignement = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT,
            PointWorldTextJustifyVertical_t verticalAlignement = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER,
            bool drawBackground = true,
            float backgroundFactor = 1f
        )
        {
            return Create(player, null, null, null, message, size, color, font, shiftX, shiftY, horizontalAlignement, verticalAlignement, drawBackground, backgroundFactor);
        }

        internal static CPointWorldText? Create(
            CBaseModelEntity? entity,
            string message,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f,
            float shiftY = 0f,
            PointWorldTextJustifyHorizontal_t horizontalAlignement = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT,
            PointWorldTextJustifyVertical_t verticalAlignement = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER,
            bool drawBackground = true,
            float backgroundFactor = 1f
        )
        {
            return Create(null, entity, null, null, message, size, color, font, shiftX, shiftY, horizontalAlignement, verticalAlignement, drawBackground, backgroundFactor);
        }

        internal static CPointWorldText? Create(
            Vector? absPosition,
            QAngle? absRotation,
            string message,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f, // shiftX/Y are only used if player is not null
            float shiftY = 0f, // shiftX/Y are only used if player is not null
            PointWorldTextJustifyHorizontal_t horizontalAlignement = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT,
            PointWorldTextJustifyVertical_t verticalAlignement = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER,
            bool drawBackground = true,
            float backgroundFactor = 1f
        )
        {
            return Create(null, null, absPosition, absRotation, message, size, color, font, shiftX, shiftY, horizontalAlignement, verticalAlignement, drawBackground, backgroundFactor);
        }

        private static CHandle<CBaseEntity>? GetOrInitializeViewModelHandle(CCSPlayerPawn playerPawn)
        {
            CPlayer_ViewModelServices? viewModelServices = playerPawn.ViewModelServices;
            if (viewModelServices == null)
            {
                return null;
            }

            CHandle<CBaseEntity>? handle = new(viewModelServices.Handle + Schema.GetSchemaOffset(SchemaViewModelServices, SchemaMemberViewModel) + 4);
            if (!handle.IsValid
                || handle.Value == null
                || !handle.Value.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>(PredictedViewModelName)!;
                if (viewmodel == null)
                {
                    return null;
                }

                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                // Ensure the viewmodel is parented to the player's viewmodel services to avoid crashes on map change or disconnect
                // This might require setting the owner or viewmodel specific properties if just SetParent is not enough.
                // For predicted_viewmodel, it's often managed by the game after being assigned.
                Utilities.SetStateChanged(playerPawn, SchemaPawnViewModelServicesClass, SchemaMemberPawnViewModelServices);
            }
            return handle;
        }

        private static (Vector FinalAbsPosition, QAngle FinalAbsRotation) CalculateTransformForAttached(
            Vector sourceOrigin,
            QAngle sourceViewAngles,
            float sourceViewOffsetZValue,
            float shiftX,
            float shiftY)
        {
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(sourceViewAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector calculatedOffset = forward * PlayerForwardOffsetMultiplier;
            calculatedOffset += right * shiftX;
            calculatedOffset += up * shiftY;

            QAngle finalAbsRotation = new()
            {
                Y = sourceViewAngles.Y + PlayerRotationOffsetY,
                Z = PlayerRotationOffsetZBase - sourceViewAngles.X,
                X = 0
            };

            Vector finalAbsPosition = sourceOrigin + calculatedOffset + new Vector(0, 0, sourceViewOffsetZValue);

            return (finalAbsPosition, finalAbsRotation);
        }

        internal static CPointWorldText? Create(
            CCSPlayerController? player,
            CBaseModelEntity? entity,
            Vector? absPositionParam,
            QAngle? absRotationParam,
            string message,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f,
            float shiftY = 0f,
            PointWorldTextJustifyHorizontal_t horizontalAlignement = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT,
            PointWorldTextJustifyVertical_t verticalAlignement = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER,
            bool drawBackground = true,
            float backgroundFactor = 1f
        )
        {
            Vector? finalAbsPosition = null;
            QAngle? finalAbsRotation = null;
            CHandle<CBaseEntity>? viewModelHandle = null;
            float currentWorldUnitsPerPx;

            if (player != null && player.IsValid && player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
            {
                CCSPlayerPawn playerPawn = player.PlayerPawn.Value;
                if (playerPawn.AbsOrigin == null)
                {
                    return null; // Safety check
                }

                currentWorldUnitsPerPx = PlayerWorldUnitsPerPxBaseFactor / PlayerWorldUnitsPerPxDivisor * size;
                viewModelHandle = GetOrInitializeViewModelHandle(playerPawn);

                (Vector FinalAbsPosition, QAngle FinalAbsRotation) = CalculateTransformForAttached(
                    playerPawn.AbsOrigin,
                    playerPawn.EyeAngles,
                    playerPawn.ViewOffset.Z,
                    shiftX,
                    shiftY);
                finalAbsPosition = FinalAbsPosition;
                finalAbsRotation = FinalAbsRotation;
            }
            else if (entity != null && entity.IsValid)
            {
                if (entity.AbsOrigin == null
                    || entity.AbsRotation == null)
                {
                    return null; // Safety check
                }

                currentWorldUnitsPerPx = PlayerWorldUnitsPerPxBaseFactor / PlayerWorldUnitsPerPxDivisor * size;
                viewModelHandle = new CHandle<CBaseEntity>(entity.Handle + Schema.GetSchemaOffset(SchemaViewModelServices, SchemaMemberViewModel) + 4)
                {
                    Raw = entity.EntityHandle.Raw
                };

                (Vector FinalAbsPosition, QAngle FinalAbsRotation) = CalculateTransformForAttached(
                    entity.AbsOrigin,
                    entity.AbsRotation,
                    entity.ViewOffset.Z,
                    shiftX,
                    shiftY);
                finalAbsPosition = FinalAbsPosition;
                finalAbsRotation = FinalAbsRotation;
            }
            else if (absPositionParam != null)
            {
                currentWorldUnitsPerPx = DefaultWorldUnitsPerPxFactor * size;
                finalAbsPosition = absPositionParam;
                finalAbsRotation = absRotationParam;
            }
            else
            {
                return null; // No valid source for position/rotation
            }

            string[] lines = message.Split('\n');
            // The original background height calculation's conditional logic had parts that were unlikely to trigger
            // or were unreachable. The effective calculation was based on lines.Length.
            float backgroundHeight = DefaultBackgroundHeightFactorPerLine * backgroundFactor * lines.Length;
            // If message is an empty string, lines.Length will be 1, providing some default height.
            // If specific behavior for truly empty messages (e.g., zero height) is needed, this may need adjustment.

            float longestLineCharCount = lines.Length > 0 ? lines.Max(static l => l.Length) : 0;
            float backgroundWidth;
            if (longestLineCharCount > 0)
            {
                backgroundWidth = DefaultBackgroundCharWidthFactor * backgroundFactor * longestLineCharCount;
            }
            else // No text or all lines empty
            {
                // This handles cases like message = "" or message = "\n"
                backgroundWidth = (lines.Length > 0 && lines.All(string.IsNullOrEmpty))
                                  ? DefaultBackgroundMinWidthFactor * backgroundFactor / 2f
                                  : 0;
            }

            CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>(WorldTextEntityName)!;
            if (worldText == null)
            {
                return null;
            }

            worldText.MessageText = message;
            worldText.Enabled = true;
            worldText.FontSize = size;
            worldText.Fullbright = true;
            worldText.Color = color ?? Color.White;
            worldText.WorldUnitsPerPx = currentWorldUnitsPerPx;
            worldText.FontName = font;
            worldText.JustifyHorizontal = horizontalAlignement;
            worldText.JustifyVertical = verticalAlignement;
            worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

            if (drawBackground)
            {
                worldText.DrawBackground = true;
                worldText.BackgroundBorderHeight = backgroundHeight;
                worldText.BackgroundBorderWidth = backgroundWidth;
                // Potentially set worldText.BackgroundColor here if needed
            }

            worldText.DispatchSpawn();
            // finalAbsPosition is guaranteed non-null here by the logic above.
            worldText.Teleport(finalAbsPosition!, finalAbsRotation, null);

            if (viewModelHandle != null && viewModelHandle.IsValid && viewModelHandle.Value != null && viewModelHandle.Value.IsValid)
            {
                worldText.AcceptInput(SetParentInputName, viewModelHandle.Value, worldText, ActivatorKeyword);
            }

            return worldText;
        }
    }
}