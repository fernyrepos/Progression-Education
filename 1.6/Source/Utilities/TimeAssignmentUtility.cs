using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public static class TimeAssignmentUtility
{
    public const string DynamicClassPrefix = "PE_DynamicClass_";

    public static bool allowUsing;

    private static void AddTimeAssignment(StudyGroup studyGroup, Pawn pawn, int hour,
        TimeAssignmentDef timeAssignment)
    {
        if (studyGroup.PriorTime.FirstOrDefault(t => t.pawn == pawn)
            is not Pawn_TimetableTracker_Fixed assignments)
        {
            assignments = new Pawn_TimetableTracker_Fixed(pawn);
            studyGroup.PriorTime.Add(assignments);
        }

        assignments.SetAssignment(hour, timeAssignment);
    }

    public static void ApplyScheduleToPawn(StudyGroup studyGroup, Pawn pawn)
    {
        if (pawn == null)
        {
            return;
        }

        var timeAssignment =
            DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName);
        SetPawnSchedule(studyGroup, pawn, timeAssignment);
    }

    public static void ApplyScheduleToPawns(StudyGroup studyGroup, List<Pawn> participants)
    {
        var timeAssignment =
            DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName);
        SetPawnSchedules(studyGroup, participants,
            timeAssignment);
    }

    public static bool CanUse(this Pawn pawn, Building building)
    {
        if (allowUsing)
        {
            return true;
        }

        var room = building.GetRoom();
        if (room == null)
        {
            return true;
        }

        foreach (var classroom in EducationManager.Instance.Classrooms
                     .Where(c =>
                         c.restrictReservationsDuringClass
                         && c.LearningBoard.parent.GetRoom() == room)
                     .Where(c => EducationManager.Instance.StudyGroups
                         .Any(sg => sg.classroom == c
                                    && sg.subjectLogic.GetValidLearningBenches()
                                        .Contains(building.def))))
        {
            EducationLog.Message(
                $"Pawn {
                    pawn.LabelShort
                } cannot use {
                    building.Label
                } during active class time in classroom {
                    classroom.name
                }.");
            return false;
        }

        return true;
    }


    public static void ClearScheduleFromPawn(StudyGroup studyGroup, Pawn pawn)
    {
        SetPawnSchedule(studyGroup, pawn);
    }

    public static void ClearScheduleFromPawns(StudyGroup studyGroup, List<Pawn> participants)
    {
        SetPawnSchedules(studyGroup, participants);
    }

    public static void GenerateTimeAssignmentDef(StudyGroup studyGroup)
    {
        if (DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName,
                false)
            != null)
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
        EducationLog.Message(
            $"Generating and injecting TimeAssignmentDef: {studyGroup.timeAssignmentDefName}");
    }

    public static bool HasConflict(this StudyGroup lhs, StudyGroup rhs)
    {
        return HasConflict(lhs.startHour, lhs.endHour,
            rhs.startHour, rhs.endHour);
    }

    public static bool HasConflict(int startHour1, int endHour1, int startHour2, int endHour2)
    {
        if (startHour1 <= endHour1)
        {
            if (startHour2 <= endHour2)
            {
                return startHour1 <= endHour2
                       && startHour2 <= endHour1;
            }

            return (startHour1 < 24 && startHour2 <= endHour1)
                   || (startHour1 <= endHour2 && startHour2 < 24);
        }

        if (startHour2 <= endHour2)
        {
            return (startHour2 < 24 && startHour1 <= endHour2)
                   || (startHour2 <= endHour1 && startHour1 < 24);
        }

        return true;
    }

    public static bool IsPawnScheduledForClass(Pawn pawn, StudyGroup studyGroup)
    {
        var timeAssignment =
            DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName,
                false);
        return timeAssignment != null && pawn.timetable.CurrentAssignment == timeAssignment;
    }

    public static bool IsStudyGroupAssignment(this TimeAssignmentDef def)
    {
        return def != null && def.defName.StartsWith(DynamicClassPrefix);
    }

    public static void RemoveAllDynamicTimeAssignmentDefs()
    {
        var dynamicDefsToRemove = DefDatabase<TimeAssignmentDef>.AllDefsListForReading
            .Where(def => def.IsStudyGroupAssignment())
            .ToList();
        EducationLog.Message(
            $"Found {dynamicDefsToRemove.ToStringSafeEnumerable()} dynamic defs to remove.");
        EducationLog.Message(
            $"All defs present: {
                DefDatabase<TimeAssignmentDef>.AllDefsListForReading.ToStringSafeEnumerable()
            }");
        foreach (var def in dynamicDefsToRemove)
        {
            DefDatabase<TimeAssignmentDef>.Remove(def);
            EducationLog.Message($"Dynamic Def '{def.defName}' removed from database.");
        }
    }

    private static TimeAssignmentDef RemoveTimeAssignment(StudyGroup studyGroup, Pawn pawn,
        int hour)
    {
        if (studyGroup.PriorTime.FirstOrDefault(t => t.pawn == pawn) is { } assignments)
        {
            return assignments.GetAssignment(hour);
        }

        return hour is > 5 and <= 21
            ? TimeAssignmentDefOf.Anything
            : TimeAssignmentDefOf.Sleep;
    }

    public static void RemoveTimeAssignmentDef(StudyGroup studyGroup)
    {
        var defToRemove =
            DefDatabase<TimeAssignmentDef>.GetNamed(studyGroup.timeAssignmentDefName,
                false);
        DefDatabase<TimeAssignmentDef>.Remove(defToRemove);
        EducationLog.Message($"Dynamic Def '{defToRemove.defName}' removed from database.");
    }

    private static void SetPawnSchedule(StudyGroup studyGroup, Pawn pawn,
        TimeAssignmentDef timeAssignment = null)
    {
        TryRepairTimetable(pawn);
        EducationLog.Message(
            $"SetPawnSchedule called for pawn {
                pawn.LabelShort
            } in {
                studyGroup.className
            } to {
                timeAssignment?.defName ?? "null"
            }");
        for (var i = 0; i < studyGroup.Duration; ++i)
        {
            var hour = (studyGroup.startHour + i) % 24;
            if (pawn.timetable.GetAssignment(hour) is not TimeAssignmentDef assignmentRemember
                || assignmentRemember == timeAssignment)
            {
                continue;
            }

            if (timeAssignment == null)
            {
                assignmentRemember =
                    RemoveTimeAssignment(studyGroup, pawn, hour);
                pawn.timetable.SetAssignment(hour, assignmentRemember);
                EducationLog.Message(
                    $"Restored timetable for pawn {
                        pawn.LabelShort
                    } at hour {
                        hour
                    } to {
                        assignmentRemember.defName
                    }");
            }
            else
            {
                AddTimeAssignment(studyGroup, pawn, hour,
                    assignmentRemember);
                pawn.timetable.SetAssignment(hour, timeAssignment);
                EducationLog.Message(
                    $"Set timetable for pawn {
                        pawn.LabelShort
                    } at hour {
                        hour
                    } to {
                        timeAssignment.defName
                    }");
            }
        }
    }

    private static void SetPawnSchedules(StudyGroup studyGroup, List<Pawn> participants,
        TimeAssignmentDef assignment = null)
    {
        foreach (var participant in participants)
        {
            SetPawnSchedule(studyGroup, participant, assignment);
        }
    }

    public static bool ShouldPreventPriorityForStudyGroup(Pawn pawn, ref float result)
    {
        var currentAssignment = pawn.timetable?.CurrentAssignment;
        if (currentAssignment == null
            || !currentAssignment.IsStudyGroupAssignment())
        {
            return false;
        }

        result = 0f;
        return true;
    }

    public static void TryRepairTimetable(Pawn pawn)
    {
        if (pawn.timetable?.times == null)
        {
            return;
        }

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
                    EducationLog.Warning(
                        $"Timetable for {
                            pawn.LabelShort
                        } is incomplete. Appending hour {
                            pawn.timetable.times.Count
                        }.");
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
}