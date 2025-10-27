using HarmonyLib;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(Pawn), "ExposeData")]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ProficiencyUtility.ApplyProficiencyTraitToPawn(__instance);
                TimeAssignmentUtility.TryRepairTimetable(__instance);
            }
        }
    }
}
