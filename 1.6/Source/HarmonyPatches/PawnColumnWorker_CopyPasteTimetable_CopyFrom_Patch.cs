using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(PawnColumnWorker_CopyPasteTimetable), "CopyFrom")]
public static class PawnColumnWorker_CopyPasteTimetable_CopyFrom_Patch
{
    public static void Postfix(Pawn p)
    {
        var clipboard = PawnColumnWorker_CopyPasteTimetable.clipboard;
        if (clipboard == null) return;

        for (var i = 0; i < clipboard.Count; i++)
        {
            var ta = clipboard[i];
            if (ta != null && ta.IsStudyGroupAssignment())
            {
                var fallback = i is > 5 and <= 21 ? TimeAssignmentDefOf.Anything : TimeAssignmentDefOf.Sleep;
                var studyGroup = EducationManager.Instance.StudyGroups.FirstOrDefault(sg => sg.timeAssignmentDefName == ta.defName);
                if (studyGroup != null && studyGroup.PriorTime.FirstOrDefault(t => t.pawn == p) is { } assignments)
                {
                    fallback = assignments.GetAssignment(i);
                }
                clipboard[i] = fallback;
            }
        }
    }
}