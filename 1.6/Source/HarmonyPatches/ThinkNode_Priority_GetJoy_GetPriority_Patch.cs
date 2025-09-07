using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(ThinkNode_Priority_GetJoy), nameof(ThinkNode_Priority_GetJoy.GetPriority))]
    public static class ThinkNode_Priority_GetJoy_GetPriority_Patch
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            return !TimeAssignmentUtility.ShouldPreventPriorityForStudyGroup(pawn, ref __result);
        }
    }
}
