using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch]
    public static class OutOfAmmoUtility_IsUsableWeaponForPawn_Patch
    {
        private static Type targetType;

        static bool Prepare()
        {
            targetType = AccessTools.TypeByName("ProgressionAmmunition.OutOfAmmoUtility");
            return targetType != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(targetType, "IsUsableWeaponForPawn");
        }

        private static bool Prefix(Thing thing, Pawn pawn, ref bool __result)
        {
            if (thing == null || pawn == null)
                return true;

            if (!pawn.CanEquipItem(thing))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
