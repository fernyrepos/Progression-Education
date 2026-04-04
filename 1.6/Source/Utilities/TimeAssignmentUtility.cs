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
            DefDatabase<TimeAssignmentDef>.Remove(defToRemove);
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
                DefDatabase<TimeAssignmentDef>.Remove(def);
                EducationLog.Message($"Dynamic Def '{def.defName}' removed from database.");
            }
        }

        public static void ApplyScheduleToPawn(StudyGroup studyGroup, Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            var timeAssignment = DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName);
            SetPawnSchedule(studyGroup, pawn, timeAssignment);
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

        private static void SetPawnSchedule(StudyGroup studyGroup, Pawn pawn, TimeAssignmentDef timeAssignment = null)
        {
            TryRepairTimetable(pawn);
            for (var hour = studyGroup.startHour; hour != studyGroup.endHour + 1; hour = ++hour % 24)
            {
                var assignmentToSet = timeAssignment ?? (hour is > 5 and <= 21 
                    ? TimeAssignmentDefOf.Anything 
                    : TimeAssignmentDefOf.Sleep);
                pawn.timetable.SetAssignment(hour, assignmentToSet);
                EducationLog.Message($"Set timetable for pawn {pawn.LabelShort} at hour {hour} to {assignmentToSet.defName}");
            }
        }

        private static void SetPawnSchedules(StudyGroup studyGroup, List<Pawn> participants, TimeAssignmentDef assignment = null)
        {
            foreach (var participant in participants)
            {
                SetPawnSchedule(studyGroup, participant, assignment);
            }
        }

        public static void TryRepairTimetable(Pawn pawn)
        {
            if (pawn.timetable?.times is null) return;
            for (var i = 0; i < 24; i++)
            {
                try
                {
                    _ = pawn.timetable.GetAssignment(i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    while (pawn.timetable.times.Count < 24)
                    {
                        EducationLog.Warning($"Timetable for {pawn.LabelShort} is incomplete. Appending hour {pawn.timetable.times.Count}.");
                        var hour = pawn.timetable.times.Count;
                        var defaultAssignment = hour is > 5 and <= 21 
                            ? TimeAssignmentDefOf.Anything 
                            : TimeAssignmentDefOf.Sleep;
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

        public static bool HasConflict(this StudyGroup lhs, StudyGroup rhs)
        {
            return HasConflict(lhs.startHour, lhs.endHour, rhs.startHour, rhs.endHour);
        }

        public static bool HasConflict(int startHour1, int endHour1, int startHour2, int endHour2)
        {
            if (startHour1 <= endHour1)
            {
                if (startHour2 <= endHour2)
                {
                    return (startHour1 <= endHour2)
                        && (startHour2 <= endHour1);
                }
                else
                {
                    return (startHour1 < 24 && startHour2 <= endHour1)
                        || (startHour1 <= endHour2 && startHour2 < 24);
                }
            }
            else
            {
                if (startHour2 <= endHour2)
                {
                    return (startHour2 < 24 && startHour1 <= endHour2)
                        || (startHour2 <= endHour1 && startHour1 < 24);
                }
                else
                {
                    return true;
                }
            }
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
