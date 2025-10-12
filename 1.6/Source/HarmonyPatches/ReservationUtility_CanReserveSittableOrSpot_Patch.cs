using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [HotSwappable]
    [HarmonyPatch(typeof(ReservationUtility), nameof(ReservationUtility.CanReserveSittableOrSpot), typeof(Pawn), typeof(IntVec3), typeof(Thing), typeof(bool))]
    public static class ReservationUtility_CanReserveSittableOrSpot_Patch
    {
        public static bool Prefix(Pawn pawn, IntVec3 exactSittingPos, Thing ignoreThing, bool ignoreOtherReservations, ref bool __result)
        {
            Building edifice = exactSittingPos.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                if (pawn.CanUse(edifice) is false)
                {
                    __result = false;
                    return false;
                }
            }
            var buildings = GenRadial.RadialDistinctThingsAround(exactSittingPos, pawn.Map, 1f, true)
                .Where(t => t is Building)
                .Cast<Building>();
            foreach (var building in buildings)
            {
                if (building.InteractionCell == exactSittingPos && pawn.CanUse(building) is false)
                {
                    __result = false;
                    return false;
                }
            }
            __result = true;
            return true;
        }
    }
}
