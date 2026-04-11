using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ProgressionEducation;

[HotSwappable]
[HarmonyPatch(typeof(ReservationUtility),
    nameof(ReservationUtility.CanReserveSittableOrSpot),
    typeof(Pawn),
    typeof(IntVec3), typeof(Thing), typeof(bool))]
[HarmonyAfter("OELS.VehicleMapFramework")]
public static class ReservationUtility_CanReserveSittableOrSpot_Patch
{
    public static bool Prefix(Pawn pawn, IntVec3 exactSittingPos, Thing ignoreThing,
        bool ignoreOtherReservations,
        ref bool __result)
    {
        var edifice = exactSittingPos.GetEdifice(pawn.Map);
        if (edifice != null
            && !pawn.CanUse(edifice))
        {
            __result = false;
            return false;
        }

        var buildings = GenRadial
            .RadialDistinctThingsAround(exactSittingPos, pawn.Map, 1f,
                true)
            .OfType<Building>();
        if (buildings.Any(building => building.InteractionCell == exactSittingPos
                                      && !pawn.CanUse(building)))
        {
            __result = false;
            return false;
        }

        __result = true;
        return true;
    }
}