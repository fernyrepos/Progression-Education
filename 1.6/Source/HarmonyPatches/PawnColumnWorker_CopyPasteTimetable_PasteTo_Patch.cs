using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(PawnColumnWorker_CopyPasteTimetable), "PasteTo")]
public static class PawnColumnWorker_CopyPasteTimetable_PasteTo_Patch
{
    public static void Prefix(Pawn p, out List<TimeAssignmentDef> __state)
    {
        __state = p?.timetable?.times?.ToList();
    }

    public static void Postfix(Pawn p, List<TimeAssignmentDef> __state)
    {
        if (__state == null || p?.timetable?.times == null) return;

        for (var i = 0; i < 24; i++)
        {
            var originalAssignment = __state[i];
            if (originalAssignment != null && originalAssignment.IsStudyGroupAssignment())
            {
                p.timetable.times[i] = originalAssignment;
            }
        }
    }
}