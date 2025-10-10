using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(ReservationUtility), nameof(ReservationUtility.CanReserveSittableOrSpot), typeof(Pawn), typeof(IntVec3), typeof(Thing), typeof(bool))]
    public static class ReservationUtility_CanReserveSittableOrSpot_Patch
    {
        public static bool Prefix(Pawn pawn, IntVec3 exactSittingPos, Thing ignoreThing, bool ignoreOtherReservations, ref bool __result)
        {
            Building edifice = exactSittingPos.GetEdifice(pawn.Map);

            if (edifice != null)
            {
                if (pawn.CanUseDuringActiveClassTime(edifice) is false)
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
