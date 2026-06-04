using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(PawnColumnWorker_CopyPasteTimetable), "CopyFrom")]
public static class PawnColumnWorker_CopyPasteTimetable_CopyFrom_Patch
{
    public static void Postfix()
    {
        var clipboard = PawnColumnWorker_CopyPasteTimetable.clipboard;
        if (clipboard == null) return;

        for (var i = 0; i < clipboard.Count; i++)
        {
            if (clipboard[i].IsStudyGroupAssignment())
            {
                clipboard[i] = i is > 5 and <= 21 ? TimeAssignmentDefOf.Anything : TimeAssignmentDefOf.Sleep;
            }
        }
    }
}