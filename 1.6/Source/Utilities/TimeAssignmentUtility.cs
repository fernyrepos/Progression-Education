using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public static class TimeAssignmentUtility
    {
        public const string DynamicClassPrefix = "PE_DynamicClass_";

        public static void GenerateTimeAssignmentDef(StudyGroup studyGroup)
        {
            if (DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName, errorOnFail: false) != null)
            {
                return;
            }
            var dynamicDef = new TimeAssignmentDef
            {
                defName = studyGroup.timeAssignmentDefName,
                label = studyGroup.className,
            };
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                dynamicDef.color = studyGroup.classroom.color;
            });
            DefDatabase<TimeAssignmentDef>.Add(dynamicDef);
            EducationLog.Message($"Generating and injecting TimeAssignmentDef: {studyGroup.timeAssignmentDefName}");
        }

        public static void RemoveTimeAssignmentDef(StudyGroup studyGroup)
        {
            var defToRemove = DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName, errorOnFail: false);
            DefDatabase<TimeAssignmentDef>.AllDefsListForReading.Remove(defToRemove);
            EducationLog.Message($"Dynamic Def '{defToRemove.defName}' removed from database.");
        }

        public static void RemoveAllDynamicTimeAssignmentDefs()
        {
            var dynamicDefsToRemove = DefDatabase<TimeAssignmentDef>.AllDefsListForReading
                .Where(def => def.IsStudyGroupAssignment())
                .ToList();
            Log.Message($"Found {dynamicDefsToRemove.ToStringSafeEnumerable()} dynamic defs to remove.");
            Log.Message($"All defs present: {DefDatabase<TimeAssignmentDef>.AllDefsListForReading.ToStringSafeEnumerable()}");
            foreach (var def in dynamicDefsToRemove)
            {
                DefDatabase<TimeAssignmentDef>.AllDefsListForReading.Remove(def);
                EducationLog.Message($"Dynamic Def '{def.defName}' removed from database.");
            }
        }

        public static void ApplyScheduleToPawns(StudyGroup studyGroup, List<Pawn> participants)
        {
            var timeAssignment = DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName);
            SetPawnSchedules(studyGroup, participants, timeAssignment);
        }

        public static void ClearScheduleFromPawns(StudyGroup studyGroup, List<Pawn> participants)
        {
            SetPawnSchedules(studyGroup, participants, TimeAssignmentDefOf.Anything);
        }

        private static void SetPawnSchedules(StudyGroup studyGroup, List<Pawn> participants, TimeAssignmentDef assignment)
        {
            foreach (var participant in participants)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    if (hour >= studyGroup.startHour && hour <= studyGroup.endHour)
                    {
                        participant.timetable.SetAssignment(hour, assignment);
                    }
                }
            }
        }

        public static bool IsPawnScheduledForClass(Pawn pawn, StudyGroup studyGroup)
        {
            var timeAssignment = DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName, errorOnFail: false);
            return timeAssignment != null && pawn.timetable.CurrentAssignment == timeAssignment;
        }

        public static bool IsStudyGroupAssignment(this TimeAssignmentDef def)
        {
            return def != null && def.defName.StartsWith(DynamicClassPrefix);
        }

        public static bool ShouldPreventPriorityForStudyGroup(Pawn pawn, ref float __result)
        {
            if (pawn.timetable == null)
            {
                return false;
            }

            var currentAssignment = pawn.timetable.CurrentAssignment;
            if (currentAssignment != null && currentAssignment.IsStudyGroupAssignment())
            {
                __result = 0f;
                return true;
            }

            return false;
        }
    }
}
