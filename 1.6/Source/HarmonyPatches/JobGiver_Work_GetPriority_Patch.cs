using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.GetPriority))]
    public static class JobGiver_Work_GetPriority_Patch
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            return !TimeAssignmentUtility.ShouldPreventPriorityForStudyGroup(pawn, ref __result);
        }
    }
}
