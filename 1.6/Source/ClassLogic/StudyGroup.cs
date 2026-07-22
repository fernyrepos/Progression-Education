using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

[HotSwappable]
public class StudyGroup : IExposable, ILoadReferenceable, IRenameable
{
    public const int MaxTeacherWaitingTicks = GenDate.TicksPerHour * 2;

    private static readonly Type t_MapParent_Vehicle =
        GenTypes.GetTypeInAnyAssembly("VehicleMapFramework.MapParent_Vehicle",
            "VehicleMapFramework");

    public int cancelledUntilTick = -1;
    public string className;
    public Classroom classroom;
    public float currentProgress;
    public int endHour;
    public int id = -1;
    private List<Pawn_TimetableTracker_Fixed> priorTime = [];
    public int semesterGoal;
    public int startHour;
    public List<Pawn> students = [];
    public ClassSubjectLogic subjectLogic;
    public bool suspended;
    public Pawn teacher;
    public string timeAssignmentDefName;

    public StudyGroup(StudyGroup other)
        : this()
    {
        id = other.id;
        teacher = other.teacher;
        students = new List<Pawn>(other.students);
        className = other.className;
        semesterGoal = other.semesterGoal;
        currentProgress = other.currentProgress;
        classroom = other.classroom;
        startHour = other.startHour;
        endHour = other.endHour;
        timeAssignmentDefName = other.timeAssignmentDefName;
        suspended = other.suspended;
        subjectLogic = other.subjectLogic.DeepClone(this);
        priorTime = [];
        foreach (var otherTimeAssignment in other.priorTime)
        {
            var timeAssignment = new Pawn_TimetableTracker_Fixed(otherTimeAssignment.pawn);
            priorTime.Add(timeAssignment);
            for (var i = 0; i < 24; ++i)
            {
                timeAssignment.SetAssignment(i,
                    otherTimeAssignment.GetAssignment(i));
            }
        }
    }

    public StudyGroup(
        Pawn teacher,
        List<Pawn> students,
        string className,
        int semesterGoal,
        int startHour,
        int endHour)
        : this()
    {
        id = EducationManager.Instance.GetNextStudyGroupId();
        this.teacher = teacher;
        this.students = students;
        this.className = className;
        this.semesterGoal = semesterGoal;
        currentProgress = 0;
        classroom = null;
        this.startHour = startHour;
        this.endHour = endHour;
        timeAssignmentDefName = TimeAssignmentUtility.DynamicClassPrefix + GetUniqueLoadID();
    }

    public StudyGroup()
    {
    }

    public List<Pawn> AllParticipants => students.Append(teacher).Where(p => p != null).ToList();

    public int Duration => 1
                           + (
                               startHour > endHour
                                   ? 24 - startHour + endHour
                                   : endHour - startHour
                           );

    public bool IsCompleted => !subjectLogic.IsInfinite && currentProgress >= semesterGoal;

    public Map Map => classroom?.LearningBoard?.parent?.Map;

    public List<Pawn_TimetableTracker_Fixed> PriorTime => priorTime;

    public float ProgressPercentage => semesterGoal <= 0
        ? 1f
        : Mathf.Clamp01(currentProgress / semesterGoal);

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, nameof(id), -1);
        Scribe_References.Look(ref teacher, nameof(teacher));
        Scribe_Collections.Look(ref students, nameof(students),
            LookMode.Reference);
        Scribe_Values.Look(ref className, nameof(className));
        Scribe_Deep.Look(ref subjectLogic, nameof(subjectLogic), this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit
            && subjectLogic == null)
        {
            subjectLogic = new SkillClassLogic(this);
        }

        Scribe_Values.Look(ref semesterGoal, nameof(semesterGoal));
        Scribe_Values.Look(ref currentProgress, nameof(currentProgress));
        Scribe_References.Look(ref classroom, nameof(classroom));
        Scribe_Values.Look(ref startHour, nameof(startHour));
        Scribe_Values.Look(ref endHour, nameof(endHour));
        Scribe_Values.Look(ref timeAssignmentDefName, nameof(timeAssignmentDefName));
        Scribe_Values.Look(ref suspended, nameof(suspended));
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (var pawn in AllParticipants)
            {
                TimeAssignmentUtility.TryRepairTimetable(pawn);
            }
        }

        Scribe_Collections.Look(ref priorTime, nameof(priorTime),
            LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit
            && priorTime == null)
        {
            priorTime = [];
        }
        Scribe_Values.Look(ref cancelledUntilTick, "cancelledUntilTick", -1);
    }

    public string GetUniqueLoadID()
    {
        return "StudyGroup_" + id;
    }

    public string RenamableLabel
    {
        get => className;
        set => className = value;
    }

    public string InspectLabel => className;

    public string BaseLabel => className;

    public void AddProgress(float amount)
    {
        currentProgress += amount;
    }

    public void AddStudent(Pawn student)
    {
        if (students.Contains(student))
        {
            return;
        }

        students.Add(student);
        TimeAssignmentUtility.ApplyScheduleToPawn(this, student);
    }

    public bool AllStudentsPresentAndAttending()
    {
        return students.All(IsStudentPresentAndAttending);
    }

    public AcceptanceReport ArePrerequisitesMet()
    {
        var subjectPrerequisites = subjectLogic.ArePrerequisitesMet();
        if (!subjectPrerequisites.Accepted)
        {
            return subjectPrerequisites;
        }

        if (classroom?.LearningBoard?.parent == null)
        {
            return new AcceptanceReport("PE_NoLearningBoard".Translate());
        }

        if (!EducationUtility.HasBellOnMap(classroom.LearningBoard.parent.Map,
                false))
        {
            return new AcceptanceReport("PE_NoBell".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public AcceptanceReport AreWorkspacesAvailable()
    {
        var benchLabel = subjectLogic.BenchLabel;
        if (!string.IsNullOrEmpty(benchLabel)
            && subjectLogic.BenchCount < students.Count)
        {
            return new AcceptanceReport("PE_NotEnoughBenches".Translate(benchLabel,
                students.Count,
                subjectLogic.BenchCount));
        }

        if (!EducationUtility.HasBellOnMap(Map, false))
        {
            return new AcceptanceReport("PE_NoBell".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public AcceptanceReport CanAcceptMoreStudents()
    {
        var studentRole = GetStudentRole();
        if (students.Count + 1 > studentRole.MaxCount)
        {
            return new AcceptanceReport("PE_StudyGroupFull".Translate(studentRole.MaxCount));
        }

        if (students.Count + 1 > subjectLogic.BenchCount)
        {
            return new AcceptanceReport("PE_NotEnoughBenches".Translate(
                subjectLogic.BenchLabel,
                students.Count + 1,
                subjectLogic.BenchCount));
        }

        return AcceptanceReport.WasAccepted;
    }

    public void CancelClass()
    {
        if (Map?.lordManager?.lords?
                .FirstOrDefault(l =>
                    l.LordJob is LordJob_AttendClass lordJob
                    && lordJob.studyGroup == this)
            is { } lordToCancel)
        {
            lordToCancel.ReceiveMemo(LordJob_AttendClass.MemoClassCancelled);
        }
    }

    public bool ClassIsActive()
    {
        if (students.NullOrEmpty())
        {
            return false;
        }

        if (subjectLogic is DaycareClassLogic)
        {
            return true;
        }

        if (subjectLogic is SkillClassLogic
            && teacher?.jobs?.curDriver is JobDriver_Teach
            {
                waitingTicks: >= MaxTeacherWaitingTicks,
            })
        {
            return true;
        }

        return AllStudentsPresentAndAttending();
    }

    public StudentRole GetStudentRole()
    {
        return new StudentRole(this);
    }

    public TeacherRole GetTeacherRole()
    {
        return new TeacherRole(this);
    }

    public bool IsStudentPresentAndAttending(Pawn student)
    {
        if (!GatheringsUtility.PawnCanStartOrContinueGathering(student))
        {
            return false;
        }

        if (student.Map != Map)
        {
            return false;
        }

        if (student.jobs?.curDriver is not JobDriver_AttendClass attendClassDriver)
        {
            return false;
        }

        // The assigned desk/bench already belongs to this classroom. Room
        // identities can change mid-job and must not deadlock the whole class.
        if (attendClassDriver is JobDriver_AttendMeleeClass)
        {
            if (!GenAdj.CellsAdjacent8Way(attendClassDriver.TargetA.Thing)
                    .Contains(student.Position))
            {
                return false;
            }
        }
        else if (student.Position
                 != JobDriver_AttendClass.DeskSpotForStudent(attendClassDriver.job
                     .GetTarget(TargetIndex.A)
                     .Thing))
        {
            return false;
        }

        return true;
    }

    public AcceptanceReport IsValid()
    {
        if (teacher == null)
        {
            return new AcceptanceReport("PE_SelectTeacher".Translate());
        }

        if (students.NullOrEmpty()
            && !subjectLogic.IsInfinite)
        {
            return new AcceptanceReport("PE_SelectAtLeastOneStudent".Translate());
        }

        var prerequisitesMet = subjectLogic.ArePrerequisitesMet();
        return !prerequisitesMet.Accepted ? prerequisitesMet : AcceptanceReport.WasAccepted;
    }

    public void Notify_TeacherUnavailable()
    {
        Messages.Message(
            "PE_CannotAttendClass".Translate(className, teacher.LabelShort),
            MessageTypeDefOf.CautionInput);
        Suspend(true);
    }

    public void RemoveStudent(Pawn student)
    {
        if (!students.Contains(student))
        {
            return;
        }

        students.Remove(student);
        TimeAssignmentUtility.ClearScheduleFromPawn(this, student);
        if (student.GetLord() is not Lord lord
            || lord.LordJob is not LordJob_AttendClass)
        {
            return;
        }

        lord.RemovePawn(student);
        student.jobs?.StopAll();
    }

    public void Suspend(bool suspend)
    {
        if (suspended == suspend)
        {
            return;
        }

        if (!suspend
            && suspended
           )
        {
            if (students.NullOrEmpty())
            {
                Messages.Message("PE_CannotUnsuspendNoStudents".Translate(),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            if (!teacher.CanAttendClass())
            {
                Messages.Message("PE_CannotUnsuspendNoTeacher".Translate(),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            if (subjectLogic.ArePrerequisitesMet() is var report
                && !report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput);
                return;
            }

            if (EducationManager.Instance.StudyGroups
                    .Except(this)
                    .FirstOrDefault(sg => !sg.suspended
                                          && this.HasConflict(sg)
                                          && (students.Intersect(sg.students).Any()
                                              || teacher == sg.teacher))
                is StudyGroup otherGroup)
            {
                Messages.Message("PE_CannotParticipateScheduled".Translate(
                        otherGroup.startHour,
                        otherGroup.endHour,
                        otherGroup.className),
                    MessageTypeDefOf.RejectInput);
                return;
            }
        }

        suspended = suspend;
        if (suspend)
        {
            TimeAssignmentUtility.ClearScheduleFromPawns(this,
                AllParticipants);
        }
        else
        {
            TimeAssignmentUtility.ApplyScheduleToPawns(this,
                AllParticipants);
        }

        if (suspended)
        {
            CancelClass();
        }

        EducationManager.Instance.Notify_ClassInvalidated(this);
    }

    public AcceptanceReport ValidateClassStatus()
    {
        if (suspended)
        {
            return AcceptanceReport.WasRejected;
        }

        var learningBoard = classroom?.LearningBoard?.parent;
        if (classroom == null
            || learningBoard?.Map == null)
        {
            return new AcceptanceReport("PE_NoLearningBoard".Translate());
        }

        var facingCell = learningBoard.Position + learningBoard.Rotation.FacingCell;
        var blockingBuilding = facingCell.GetFirstBuilding(classroom.LearningBoard.parent.Map);
        if (blockingBuilding != null
            && blockingBuilding.def.passability != Traversability.Standable
            && blockingBuilding.def != ThingDefOf.HiddenConduit)
        {
            return new AcceptanceReport(
                "PE_LearningBoardIsBlocked".Translate(classroom.LearningBoard.parent.def.label));
        }

        var workspaceReport = AreWorkspacesAvailable();
        if (!workspaceReport.Accepted)
        {
            return workspaceReport;
        }

        var learningBoardSourceMap = MapOrSourceMap(classroom.LearningBoard.parent);
        if (!teacher.Spawned
            || MapOrSourceMap(teacher) != learningBoardSourceMap)
        {
            return new AcceptanceReport("PE_TeacherOffMap".Translate(className));
        }

        var teacherRole = GetTeacherRole();
        var teacherQualification = teacherRole.CanAcceptPawn(teacher);
        if (!teacherQualification.Accepted)
        {
            return teacherQualification;
        }

        var studentRole = GetStudentRole();
        List<Pawn> studentsOffMap = [];
        List<Pawn> unqualifiedStudents = [];

        foreach (var student in students)
        {
            if (!student.Spawned
                || MapOrSourceMap(student) != learningBoardSourceMap)
            {
                if (student.Map?.Parent is PocketMapParent mapParent
                    && mapParent.sourceMap == classroom.LearningBoard.parent.Map)
                {
                    continue;
                }

                studentsOffMap.Add(student);
                continue;
            }

            var studentQualification = studentRole.CanAcceptPawn(student);
            if (!studentQualification.Accepted)
            {
                unqualifiedStudents.Add(student);
            }
        }

        if (studentsOffMap.Count > 0)
        {
            return new AcceptanceReport("PE_StudentsOffMap".Translate());
        }

        if (unqualifiedStudents.Count > 0)
        {
            return new AcceptanceReport("PE_StudentsUnqualified".Translate(unqualifiedStudents.Select(x => x.Label).ToStringSafeEnumerable()));
        }

        if (students.Count == 0)
        {
            return new AcceptanceReport("PE_NoStudents".Translate());
        }

        return AcceptanceReport.WasAccepted;

        Map MapOrSourceMap(Thing thing)
        {
            var map = thing.Map;
            var sourceMap = map.PocketMapParent?.sourceMap;
            if (sourceMap != null
                && map.Parent.GetType().SameOrSubclassOf(t_MapParent_Vehicle))
            {
                return sourceMap;
            }

            return map;
        }
    }

}
