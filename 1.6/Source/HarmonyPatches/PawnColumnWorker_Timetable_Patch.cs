using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProgressionEducation
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnColumnWorker_Timetable), nameof(PawnColumnWorker_Timetable.DoTimeAssignment))]
    public static class PawnColumnWorker_Timetable_Patch
    {
        public static bool Prefix(Rect rect, Pawn p, int hour)
        {
            var currentAssignment = p.timetable.GetAssignment(hour);
            if (currentAssignment.IsStudyGroupAssignment())
            {
                rect = rect.ContractedBy(1f);
                bool mouseButton = Input.GetMouseButton(0);
                GUI.DrawTexture(rect, currentAssignment.ColorTexture);
                if (!mouseButton)
                {
                    MouseoverSounds.DoRegion(rect);
                }
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawBox(rect, 2);
                }
                return mouseButton && TimeAssignmentSelector.selectedAssignment != currentAssignment && false;
            }
            return true;
        }
    }
}
