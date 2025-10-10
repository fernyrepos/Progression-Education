using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class Dialog_RescheduleClass : Window
    {
        private readonly StudyGroup studyGroup;
        private int selectedStartHour;
        private int selectedEndHour;

        public Dialog_RescheduleClass(StudyGroup studyGroup)
        {
            this.studyGroup = studyGroup;
            selectedStartHour = studyGroup.startHour;
            selectedEndHour = studyGroup.endHour;

            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override Vector2 InitialSize => new(300f, 200f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "PE_RescheduleClass".Translate());
            Text.Font = GameFont.Small;

            float curY = inRect.y + 35f;
            Widgets.Label(new Rect(inRect.x, curY, 100f, 25f), "PE_StartHour".Translate());
            if (Widgets.ButtonText(new Rect(inRect.x + 110f, curY, 150f, 25f), selectedStartHour.ToString()))
            {
                var options = Dialog_CreateClass.GenerateHourSelectionOptions(hour => selectedStartHour = hour);
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 30f;
            Widgets.Label(new Rect(inRect.x, curY, 100f, 25f), "PE_EndHour".Translate());
            if (Widgets.ButtonText(new Rect(inRect.x + 110f, curY, 150f, 25f), selectedEndHour.ToString()))
            {
                var options = Dialog_CreateClass.GenerateHourSelectionOptions(hour => selectedEndHour = hour);
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 40f;
            if (Widgets.ButtonText(new Rect(inRect.x, curY, 100f, 35f), "OK".Translate()))
            {
                string conflict = CheckForConflicts(studyGroup, selectedStartHour, selectedEndHour);
                if (conflict != null)
                {
                    Messages.Message(conflict, MessageTypeDefOf.RejectInput);
                }
                else
                {
                    Reschedule(studyGroup, selectedStartHour, selectedEndHour);
                    Close();
                }
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 110f, curY, 100f, 35f), "Cancel".Translate()))
            {
                Close();
            }
        }

        public static void Reschedule(StudyGroup studyGroup, int newStart, int newEnd)
        {
            List<Pawn> allParticipants = [studyGroup.teacher, .. studyGroup.students];
            TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup, allParticipants);
            studyGroup.startHour = newStart;
            studyGroup.endHour = newEnd;
            var educationManager = EducationManager.Instance;
            educationManager.ApplyScheduleToPawns(studyGroup);
        }

        private string CheckForConflicts(StudyGroup currentGroup, int startHour, int endHour)
        {
            List<Pawn> allParticipants = [currentGroup.teacher, .. currentGroup.students];
            foreach (var pawn in allParticipants)
            {
                foreach (var otherGroup in EducationManager.Instance.StudyGroups)
                {
                    if (otherGroup != currentGroup && (otherGroup.students.Contains(pawn) || otherGroup.teacher == pawn))
                    {
                        if (TimeAssignmentUtility.HasConflict(startHour, endHour, otherGroup.startHour, otherGroup.endHour))
                        {
                            return "PE_CannotRescheduleScheduled".Translate(pawn.LabelShort, otherGroup.startHour, otherGroup.endHour, otherGroup.className);
                        }
                    }
                }
            }
            return null;
        }
    }
}