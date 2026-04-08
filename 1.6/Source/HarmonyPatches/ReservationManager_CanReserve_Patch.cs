using HarmonyLib;
using Verse;
using Verse.AI;

namespace ProgressionEducation;

[HotSwappable]
[HarmonyPatch(typeof(ReservationManager),
    nameof(ReservationManager.CanReserve))]
public static class ReservationManager_CanReserve_Patch
{
    public static bool Prefix(Pawn claimant, LocalTargetInfo target, ref bool __result)
    {
        if (target.Thing is not Building building)
        {
            return true;
        }

        if (claimant.CanUse(building))
        {
            return true;
        }

        __result = false;
        return false;
    }
}