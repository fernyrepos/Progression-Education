using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(Pawn_TimetableTracker), nameof(Pawn_TimetableTracker.SetAssignment))]
public static class Pawn_TimetableTracker_SetAssignment_Patch
{
    public static bool Prefix(Pawn_TimetableTracker __instance, TimeAssignmentDef ta)
    {
        if (ta.IsStudyGroupAssignment())
        {
            var pawn = __instance.pawn;
            var assignmentDefName = ta.defName;
            var studyGroup = EducationManager.Instance.StudyGroups.FirstOrDefault(sg => sg.timeAssignmentDefName == assignmentDefName);
            if (studyGroup == null || !studyGroup.AllParticipants.Contains(pawn))
            {
                return false;
            }
        }
        return true;
    }
}
