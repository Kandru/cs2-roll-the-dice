using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Damage dealt by a dice with a real attacker (kill feed, score, money), through the engine's TakeDamage like
    /// PlayAsChicken's redirect. Damage dealt here carries <see cref="Marker"/>: damage hooks of the dices skip it, so
    /// damage that causes damage (Voodoo, toxic smoke) never feeds itself.
    /// </summary>
    public static class Damage
    {
        /// <summary>Stored in m_nNumObjectsPenetrated (bullets never penetrate this many objects).</summary>
        public const int Marker = 4242;

        public static bool IsFromDice(CTakeDamageInfo info) => info.NumObjectsPenetrated == Marker;

        public static void Deal(CCSPlayerPawn victim, CCSPlayerPawn? attacker, float amount, DamageTypes_t type = DamageTypes_t.DMG_GENERIC)
        {
            if (!victim.IsValid || victim.LifeState != (byte)LifeState_t.LIFE_ALIVE || amount <= 0f)
            {
                return;
            }

            // no attacker (a fall): the world deals it, like the engine's own fall damage; an empty handle is dropped
            CBaseEntity? source = attacker is { IsValid: true } ? attacker : Utilities.GetEntityFromIndex<CBaseEntity>(0);
            uint attackerRaw = source is { IsValid: true } ? source.EntityHandle.Raw : victim.EntityHandle.Raw;
            int infoSize = Schema.GetClassSize("CTakeDamageInfo");
            int resultSize = Schema.GetClassSize("CTakeDamageResult");
            nint infoPtr = Marshal.AllocHGlobal(infoSize);
            nint resultPtr = Marshal.AllocHGlobal(resultSize);
            try
            {
                unsafe
                {
                    NativeMemory.Clear((void*)infoPtr, (nuint)infoSize);
                    NativeMemory.Clear((void*)resultPtr, (nuint)resultSize);
                }

                CTakeDamageInfo info = new(infoPtr);
                CTakeDamageResult result = new(resultPtr);
                Schema.SetSchemaValue(info.Handle, "CTakeDamageInfo", "m_hInflictor", attackerRaw);
                Schema.SetSchemaValue(info.Handle, "CTakeDamageInfo", "m_hAttacker", attackerRaw);
                info.Damage = amount;
                info.OriginalDamage = amount;
                info.TotalledDamage = amount;
                info.FriendlyFireDamageReductionRatio = 1f;
                if (victim.AbsOrigin is { } origin)
                {
                    info.DamagePosition.X = origin.X;
                    info.DamagePosition.Y = origin.Y;
                    info.DamagePosition.Z = origin.Z;
                }
                info.DamageFlags = TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;
                info.BitsDamageType = type;
                info.NumObjectsPenetrated = Marker;

                Schema.SetSchemaValue(result.Handle, "CTakeDamageResult", "m_pOriginatingInfo", info.Handle);
                int lost = Math.Max(1, (int)Math.Round(amount));
                result.HealthLost = lost;
                result.DamageDealt = lost;
                result.PreModifiedDamage = amount;
                result.TotalledHealthLost = lost;
                result.TotalledDamageDealt = lost;
                result.WasDamageSuppressed = false;

                VirtualFunctions.CBaseEntity_TakeDamageOld.Invoke(victim, info, result);
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
                Marshal.FreeHGlobal(resultPtr);
            }
        }

        /// <summary>Heals up to the pawn's max health (never above it).</summary>
        public static void Heal(CCSPlayerPawn pawn, int amount)
        {
            if (!pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || amount <= 0 || pawn.Health >= pawn.MaxHealth)
            {
                return;
            }
            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + amount);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }
}
