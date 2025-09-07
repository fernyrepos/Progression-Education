using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(JobGiver_GetRest), nameof(JobGiver_GetRest.GetPriority))]
    public static class JobGiver_GetRest_GetPriority_Patch
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            return !TimeAssignmentUtility.ShouldPreventPriorityForStudyGroup(pawn, ref __result);
        }
    }
}
