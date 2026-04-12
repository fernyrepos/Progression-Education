using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ProgressionEducation;

public class LordJob_AttendClass : LordJob
{
    public const string MemoBellRung = "BellRung";
    public const string MemoClassCancelled = "ClassCancelled";

    public const string MemoClassCancelledTeacherIncapacitated =
        "ClassCancelled_TeacherIncapacitated";

    public const string MemoClassCompleted = "ClassCompleted";
    public const string MemoClassTimeFinished = "ClassTimeFinished";

    public bool classStartedSuccessfully = false;
    public StudyGroup studyGroup;

    public LordJob_AttendClass()
    {
    }

    public LordJob_AttendClass(StudyGroup studyGroup)
    {
        this.studyGroup = studyGroup;
        EducationLog.Message($"LordJob_AttendClass created for class '{studyGroup.className}'");
    }

    private void CancelOtherJobs()
    {
        foreach (var member in studyGroup.students
                     .Append(studyGroup.teacher)
                     .Where(m => GatheringsUtility.PawnCanStartOrContinueGathering(m)
                                 && m.CurJob?.def != DefsOf.PE_RingBell
                                 && (studyGroup.classroom.interruptJobs
                                     || CanInterruptJob(m)))
                )
        {
            member.jobs?.StopAll();
            if (member.drafter != null)
            {
                member.drafter.Drafted = true;
                member.drafter.Drafted = false;
            }
        }
    }

    private static bool CanInterruptJob(Pawn member)
    {
        var lordJob = member.GetLord()?.LordJob;
        if (lordJob is LordJob_Joinable_MarriageCeremony or LordJob_Ritual)
        {
            return false;
        }

        if (member.jobs.curDriver?.asleep ?? false)
        {
            return true;
        }

        var curDriver = member.jobs.curDriver;
        if (curDriver is JobDriver_Meditate or JobDriver_Research)
        {
            return true;
        }

        var jobGiver = member.jobs.curDriver?.job?.jobGiver;
        if (jobGiver is JobGiver_GetJoy)
        {
            return true;
        }

        return false;
    }

    private void CheckProficiencyClassStartFailure()
    {
        if (!classStartedSuccessfully
            && studyGroup.subjectLogic is ProficiencyClassLogic)
        {
            Messages.Message(
                "PE_ProficiencyClassNeverStarted".Translate(studyGroup.className),
                MessageTypeDefOf.NegativeEvent);
        }
    }

    private void CheckTeacherBellFailure()
    {
        if (CompBell.AllBells.Count > 0)
        {
            var hasManualBell = CompBell.AllBells.Any(bellComp =>
                bellComp.parent.Map == studyGroup.Map
                && !bellComp.ShouldRingAutomatically);

            if (hasManualBell)
            {
                Messages.Message("PE_TeacherBellFailure".Translate(),
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public override StateGraph CreateGraph()
    {
        var stateGraph = new StateGraph();

        var ringBellToil = new LordToil_RingBell(studyGroup);
        stateGraph.AddToil(ringBellToil);

        var attendClassToil = new LordToil_AttendClass(studyGroup);
        stateGraph.AddToil(attendClassToil);

        var bellRungTransition = new Transition(ringBellToil, attendClassToil);
        bellRungTransition.AddTrigger(new Trigger_Memo(MemoBellRung));
        bellRungTransition.AddPreAction(new TransitionAction_Custom(CancelOtherJobs));
        bellRungTransition.AddPostAction(new TransitionAction_AddStudentsToLord(studyGroup));
        stateGraph.AddTransition(bellRungTransition);

        var endToil = new LordToil_End();
        stateGraph.AddToil(endToil);

        var classTimeFinishedTransition =
            new Transition(ringBellToil, endToil);
        classTimeFinishedTransition.AddTrigger(new Trigger_Memo(MemoClassTimeFinished));
        classTimeFinishedTransition.AddPreAction(
            new TransitionAction_Custom(CheckTeacherBellFailure));
        stateGraph.AddTransition(classTimeFinishedTransition);

        var classCancelledTeacherIncapacitatedTransition =
            new Transition(ringBellToil, endToil);
        classCancelledTeacherIncapacitatedTransition.AddTrigger(
            new Trigger_Memo(MemoClassCancelledTeacherIncapacitated));
        stateGraph.AddTransition(classCancelledTeacherIncapacitatedTransition);

        var classCancelledTransition = new Transition(ringBellToil, endToil);
        classCancelledTransition.AddTrigger(new Trigger_Memo(MemoClassCancelled));
        stateGraph.AddTransition(classCancelledTransition);

        var classCompletedTransition =
            new Transition(attendClassToil, endToil);
        classCompletedTransition.AddTrigger(new Trigger_Memo(MemoClassCompleted));
        stateGraph.AddTransition(classCompletedTransition);

        var timeFinishedEndTransition =
            new Transition(attendClassToil, endToil);
        timeFinishedEndTransition.AddTrigger(new Trigger_Memo(MemoClassTimeFinished));
        timeFinishedEndTransition.AddPreAction(
            new TransitionAction_Custom(CheckProficiencyClassStartFailure));
        stateGraph.AddTransition(timeFinishedEndTransition);

        var cancelledEndTransition = new Transition(attendClassToil, endToil);
        cancelledEndTransition.AddTrigger(new Trigger_Memo(MemoClassCancelled));
        stateGraph.AddTransition(cancelledEndTransition);

        var teacherIncapacitatedTransition =
            new Transition(attendClassToil, endToil);
        teacherIncapacitatedTransition.AddTrigger(
            new Trigger_Memo(MemoClassCancelledTeacherIncapacitated));
        stateGraph.AddTransition(teacherIncapacitatedTransition);

        return stateGraph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref studyGroup, "studyGroup");
    }

    public override void LordJobTick()
    {
        base.LordJobTick();
        if (studyGroup == null)
        {
            EducationLog.Message(
                "LordJob_AttendClass.LordJobTick Study group is null. Canceling class.");
            lord.ReceiveMemo(MemoClassCancelled);

            return;
        }

        if (!GatheringsUtility.PawnCanStartOrContinueGathering(studyGroup.teacher))
        {
            Messages.Message(
                "PE_CannotAttendClass".Translate(studyGroup.className,
                    studyGroup.teacher.LabelShort), MessageTypeDefOf.NegativeEvent);
            EducationLog.Message(
                $"LordJob_AttendClass.LordJobTick Class '{
                    studyGroup.className
                }' is being cancelled: teacher is unavailable.");
            lord.ReceiveMemo(MemoClassCancelledTeacherIncapacitated);
            studyGroup.Notify_TeacherUnavailable();

            return;
        }

        if (studyGroup.teacher.GetLord() != lord)
        {
            EducationLog.Message(
                $"LordJob_AttendClass.LordJobTick Class '{
                    studyGroup.className
                }' is being cancelled: teacher is no longer in the class.");
            studyGroup.Notify_TeacherUnavailable();
            return;
        }

        if (!TimeAssignmentUtility.IsPawnScheduledForClass(studyGroup.teacher,
                studyGroup))
        {
            lord.ReceiveMemo(MemoClassTimeFinished);
            EducationLog.Message(
                $"LordJob_AttendClass.LordJobTick Class '{
                    studyGroup.className
                }' is finished: teacher is no longer scheduled for class.");
            return;
        }

        if (studyGroup.ValidateClassStatus() is { Accepted: false } validationReport)
        {
            EducationLog.Message(
                $"LordJob_AttendClass.LordJobTick Class '{
                    studyGroup.className
                }' is being cancelled: {
                    validationReport.Reason
                }");
        }
    }

    public class TransitionAction_AddStudentsToLord(StudyGroup studyGroup) : TransitionAction
    {
        public override void DoAction(Transition trans)
        {
            if (trans?.target?.lord is not Lord lord)
            {
                return;
            }

            var studentRole = studyGroup.GetStudentRole();
            foreach (var student in studyGroup.students
                         .Where(s => !GatheringsUtility.PawnCanStartOrContinueGathering(s)
                                        && s.GetLord() == null))
            {
                if (!studentRole.CanAcceptPawn(student).Accepted)
                {
                    continue;
                }

                lord.AddPawn(student);
                EducationLog.Message(
                    $"LordJob_AttendClass.TransitionAction_AddStudentsToLord Added student {
                        student.LabelShort
                    } to lord for class '{
                        studyGroup.className
                    }'");
            }
        }
    }
}