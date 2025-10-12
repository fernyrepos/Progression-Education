using RimWorld;
using System;
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
            EducationLog.Message($"Found {dynamicDefsToRemove.ToStringSafeEnumerable()} dynamic defs to remove.");
            EducationLog.Message($"All defs present: {DefDatabase<TimeAssignmentDef>.AllDefsListForReading.ToStringSafeEnumerable()}");
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
            SetPawnSchedules(studyGroup, participants);
        }

        private static void SetPawnSchedules(StudyGroup studyGroup, List<Pawn> participants, TimeAssignmentDef assignment = null)
        {
            foreach (var participant in participants)
            {
                TryRepairTimetable(participant);
                for (int hour = 0; hour < 24; hour++)
                {
                    bool isScheduled;
                    if (studyGroup.startHour <= studyGroup.endHour)
                    {
                        isScheduled = hour >= studyGroup.startHour && hour <= studyGroup.endHour;
                    }
                    else
                    {
                        isScheduled = hour >= studyGroup.startHour || hour <= studyGroup.endHour;
                    }

                    if (isScheduled)
                    {
                        TimeAssignmentDef assignmentToSet = assignment ?? ((hour > 5 && hour <= 21) ? TimeAssignmentDefOf.Anything : TimeAssignmentDefOf.Sleep);
                        participant.timetable.SetAssignment(hour, assignmentToSet);
                        EducationLog.Message($"Set timetable for pawn {participant.LabelShort} at hour {hour} to {assignmentToSet.defName}");
                    }
                }
            }
        }

        public static void TryRepairTimetable(Pawn pawn)
        {
            if (pawn.timetable.times == null)
            {
                pawn.timetable.times = new List<TimeAssignmentDef>();
            }
            for (int i = 0; i < 24; i++)
            {
                try
                {
                    _ = pawn.timetable.GetAssignment(i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    while (pawn.timetable.times.Count < 24)
                    {
                        Log.Warning($"Timetable for {pawn.LabelShort} is incomplete. Appending hour {pawn.timetable.times.Count}.");
                        int hour = pawn.timetable.times.Count;
                        TimeAssignmentDef defaultAssignment = ((hour > 5 && hour <= 21) ? TimeAssignmentDefOf.Anything : TimeAssignmentDefOf.Sleep);
                        pawn.timetable.times.Add(defaultAssignment);
                    }
                    break;
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

        private static bool IsHourInSchedule(int hour, int start, int end)
        {
            if (start <= end)
            {
                return hour >= start && hour <= end;
            }
            else
            {
                return hour >= start || hour <= end;
            }
        }

        public static bool HasConflict(int startHour1, int endHour1, int startHour2, int endHour2)
        {
            for (int hour = 0; hour < 24; hour++)
            {
                bool inSchedule1 = IsHourInSchedule(hour, startHour1, endHour1);
                bool inSchedule2 = IsHourInSchedule(hour, startHour2, endHour2);

                if (inSchedule1 && inSchedule2)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool allowUsing;
        public static bool CanUse(this Pawn pawn, Building building)
        {
            if (allowUsing) return true;
            var room = building.GetRoom();
            if (room == null) return true;
            foreach (var classroom in EducationManager.Instance.Classrooms)
            {
                if (classroom.restrictReservationsDuringClass && classroom.LearningBoard.parent.GetRoom() == room)
                {
                    var studyGroupsInClassroom = EducationManager.Instance.StudyGroups.Where(sg => sg.classroom == classroom);
                    foreach (var studyGroup in studyGroupsInClassroom)
                    {
                        var validBenches = studyGroup.subjectLogic.GetValidLearningBenches();
                        if (validBenches.Contains(building.def))
                        {
                            EducationLog.Message($"Pawn {pawn.LabelShort} cannot use {building.Label} during active class time in classroom {classroom.name}.");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
