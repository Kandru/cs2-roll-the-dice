using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;

namespace RollTheDice.Utils
{
    public static class Entities
    {
        public static void SetSchemaValue<T>(CBaseEntity entity, string className, string propertyName, T value)
        {
            Schema.SetSchemaValue(entity.Handle, className, propertyName, value);
            if (Schema.IsSchemaFieldNetworked(className, propertyName))
            {
                Utilities.SetStateChanged(entity, className, propertyName);
            }
        }

        public static CDynamicProp? CreatePlayerEntity(Vector position, QAngle rotation, string model, string animation, bool loop = false)
        {
            CDynamicProp? prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (prop == null)
            {
                return null;
            }
            prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            // do not use default animation stuff
            prop.UseAnimGraph = false;
            // set idle animation
            prop.IdleAnim = animation;
            prop.IdleAnimLoopMode = loop ? AnimLoopMode_t.ANIM_LOOP_MODE_LOOPING : AnimLoopMode_t.ANIM_LOOP_MODE_NOT_LOOPING;
            prop.DispatchSpawn();
            // set player model
            prop.SetModel(model);
            // start animation
            prop.AcceptInput("Enable");
            prop.Teleport(position, rotation);
            return prop;
        }

        public static CDynamicProp? CreatePropEntity(Vector position, QAngle rotation, string model, float scale = 1f, CEntityInstance? parent = null)
        {
            CDynamicProp? prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (prop == null)
            {
                return null;
            }
            prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(prop.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            // do not use default animation stuff
            prop.UseAnimGraph = true;
            // scale
            CEntityKeyValues kv = new();
            kv.SetFloat("modelscale", scale);
            // spawn it
            prop.DispatchSpawn(kv);
            // set player model
            prop.SetModel(model);
            // start animation
            prop.AcceptInput("Enable");
            prop.Teleport(position, rotation);
            // parent if enabled
            if (parent != null)
            {
                prop.AcceptInput("SetParent", parent, parent, "!activator");
            }
            return prop;
        }

        public static string GetModel(CBaseEntity entity)
        {
            return entity?.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? string.Empty;
        }

        public static void RemoveEntity(CBaseEntity? entity)
        {
            if (entity != null
                && entity.IsValid)
            {
                entity.AcceptInput("Kill");
                entity.Remove();
            }
        }

        public static string? PlayerWeaponName(CBasePlayerWeapon weapon)
        {
            if (!weapon.IsValid)
            {
                return null;
            }
            try
            {
                CCSWeaponBaseVData? vdata = weapon.GetVData<CCSWeaponBaseVData>();
                return vdata?.Name ?? null;
            }
            catch
            {
                return null;
            }
        }

        public static Vector GetForwardVector(QAngle angles)
        {
            // Converts QAngle (pitch, yaw, roll) to a forward direction vector
            float pitch = angles.X * (float)Math.PI / 180f;
            float yaw = angles.Y * (float)Math.PI / 180f;
            float x = (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = (float)(Math.Cos(pitch) * Math.Sin(yaw));
            float z = (float)-Math.Sin(pitch);
            return new Vector(x, y, z);
        }

        public static Vector GetRightVector(QAngle angles)
        {
            float pitch = angles.X * (float)Math.PI / 180f;
            float yaw = angles.Y * (float)Math.PI / 180f;
            float roll = angles.Z * (float)Math.PI / 180f;

            float sp = (float)Math.Sin(pitch);
            float cp = (float)Math.Cos(pitch);
            float sy = (float)Math.Sin(yaw);
            float cy = (float)Math.Cos(yaw);
            float sr = (float)Math.Sin(roll);
            float cr = (float)Math.Cos(roll);

            float x = (-1 * sr * sp * cy) + (-1 * cr * -sy);
            float y = (-1 * sr * sp * sy) + (-1 * cr * cy);
            float z = -1 * sr * cp;
            return new Vector(x, y, z);
        }

        public static Vector GetUpVector(QAngle angles)
        {
            float pitch = angles.X * (float)Math.PI / 180f;
            float yaw = angles.Y * (float)Math.PI / 180f;
            float roll = angles.Z * (float)Math.PI / 180f;

            float sp = (float)Math.Sin(pitch);
            float cp = (float)Math.Cos(pitch);
            float sy = (float)Math.Sin(yaw);
            float cy = (float)Math.Cos(yaw);
            float sr = (float)Math.Sin(roll);
            float cr = (float)Math.Cos(roll);

            float x = (cr * sp * cy) + (-sr * -sy);
            float y = (cr * sp * sy) + (-sr * cy);
            float z = cr * cp;
            return new Vector(x, y, z);
        }
    }
}