using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [HotSwappable]
    [HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.ExtraRequirements))]
    public static class WorkGiver_GrowerSow_ExtraRequirements_Patch
    {
        public static void Postfix(ref bool __result, IPlantToGrowSettable settable, Pawn pawn)
        {
            if (!__result)
            {
                return;
            }
    
            if (settable is Building plantGrower)
            {
                __result = pawn.CanUse(plantGrower);
            }
        }
    }
}
