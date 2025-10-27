using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class LordJob_AttendClass : LordJob
    {
        public StudyGroup studyGroup;
        public bool classStartedSuccessfully = false;
        public const string MemoBellRung = "BellRung";
        public const string MemoClassCompleted = "ClassCompleted";
        public const string MemoClassTimeFinished = "ClassTimeFinished";
        public const string MemoClassCancelled = "ClassCancelled";
        public const string MemoClassCancelledTeacherIncapacitated = "ClassCancelled_TeacherIncapacitated";

        public LordJob_AttendClass()
        {
        }

        public LordJob_AttendClass(StudyGroup studyGroup)
        {
            this.studyGroup = studyGroup;
            EducationLog.Message($"LordJob_AttendClass created for class '{studyGroup.className}'");
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (studyGroup.teacher.Dead || studyGroup.teacher.Downed || studyGroup.teacher.InMentalState)
            {
                EducationLog.Message($"Class '{studyGroup.className}' is being cancelled: teacher is dead, downed, or in mental state.");
                lord.ReceiveMemo(MemoClassCancelledTeacherIncapacitated);
            }
            else if (studyGroup.teacher.GetLord() != lord)
            {
                EducationLog.Message($"Class '{studyGroup.className}' is being cancelled: teacher is no longer in the class.");
                lord.ReceiveMemo(MemoClassCancelled);
            }
            else if (TimeAssignmentUtility.IsPawnScheduledForClass(studyGroup.teacher, studyGroup) is false)
            {
                lord.ReceiveMemo(MemoClassTimeFinished);
                EducationLog.Message($"Class '{studyGroup.className}' is finished: teacher is no longer scheduled for class.");
            }
            else
            {
                var validationReport = studyGroup.ValidateClassStatus();
                if (!validationReport.Accepted)
                {
                    EducationLog.Message($"Class '{studyGroup.className}' is being cancelled: {validationReport.Reason}");
                    lord.ReceiveMemo(MemoClassCancelled);
                }
            }
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new();
            LordToil_RingBell ringBellToil = new(studyGroup);
            stateGraph.AddToil(ringBellToil);
            LordToil_AttendClass attendClassToil = new(studyGroup);
            stateGraph.AddToil(attendClassToil);
            Transition ringToAttendTransition = new(ringBellToil, attendClassToil);
            ringToAttendTransition.AddTrigger(new Trigger_Memo(MemoBellRung));
            ringToAttendTransition.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                var members = studyGroup.students.Concat(studyGroup.teacher).ToList();
                foreach (var member in members)
                {
                    Job curJob = member.CurJob;
                    if (studyGroup.classroom?.interruptJobs != true)
                    {
                        member.jobs.StopAll();
                    }
                    else
                    {
                        if (CanInterruptJob(member))
                        {
                            member.jobs.StopAll();
                        }
                    }
                }
            }));
            ringToAttendTransition.AddPostAction(new TransitionAction_AddStudentsToLord(studyGroup));
            stateGraph.AddTransition(ringToAttendTransition);
            LordToil_End endToil = new();
            stateGraph.AddToil(endToil);

            Transition timeFinishedEndTransition_Ring = new(ringBellToil, endToil);
            timeFinishedEndTransition_Ring.AddTrigger(new Trigger_Memo(MemoClassTimeFinished));
            timeFinishedEndTransition_Ring.AddPreAction(new TransitionAction_Custom(CheckTeacherBellFailure));
            stateGraph.AddTransition(timeFinishedEndTransition_Ring);

            Transition teacherIncapacitatedTransition_Ring = new(ringBellToil, endToil);
            teacherIncapacitatedTransition_Ring.AddTrigger(new Trigger_Memo(MemoClassCancelledTeacherIncapacitated));
            stateGraph.AddTransition(teacherIncapacitatedTransition_Ring);

            Transition cancelledEndTransition_Ring = new(ringBellToil, endToil);
            cancelledEndTransition_Ring.AddTrigger(new Trigger_Memo(MemoClassCancelled));
            stateGraph.AddTransition(cancelledEndTransition_Ring);

            Transition attendToClassEndTransition = new(attendClassToil, endToil);
            attendToClassEndTransition.AddTrigger(new Trigger_Memo(MemoClassCompleted));
            stateGraph.AddTransition(attendToClassEndTransition);

            Transition timeFinishedEndTransition = new(attendClassToil, endToil);
            timeFinishedEndTransition.AddTrigger(new Trigger_Memo(MemoClassTimeFinished));
            timeFinishedEndTransition.AddPreAction(new TransitionAction_Custom(CheckProficiencyClassStartFailure));
            stateGraph.AddTransition(timeFinishedEndTransition);

            Transition cancelledEndTransition = new(attendClassToil, endToil);
            cancelledEndTransition.AddTrigger(new Trigger_Memo(MemoClassCancelled));
            stateGraph.AddTransition(cancelledEndTransition);

            Transition teacherIncapacitatedTransition = new(attendClassToil, endToil);
            teacherIncapacitatedTransition.AddTrigger(new Trigger_Memo(MemoClassCancelledTeacherIncapacitated));
            stateGraph.AddTransition(teacherIncapacitatedTransition);

            return stateGraph;
        }

        private static bool CanInterruptJob(Pawn member)
        {
            if (member.Deathresting)
            {
                return false;
            }
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref studyGroup, "studyGroup");
        }

        private void CheckProficiencyClassStartFailure()
        {
            if (!classStartedSuccessfully && studyGroup.subjectLogic is ProficiencyClassLogic)
            {
                Messages.Message("PE_ProficiencyClassNeverStarted".Translate(studyGroup.className), MessageTypeDefOf.NegativeEvent);
            }
        }

        private void CheckTeacherBellFailure()
        {
            if (CompBell.AllBells.Count > 0)
            {
                bool hasManualBell = false;
                foreach (var bellComp in CompBell.AllBells)
                {
                    if (bellComp.parent.Map == studyGroup.Map && !bellComp.ShouldRingAutomatically)
                    {
                        hasManualBell = true;
                        break;
                    }
                }

                if (hasManualBell)
                {
                    Messages.Message("PE_TeacherBellFailure".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        public class TransitionAction_AddStudentsToLord : TransitionAction
        {
            private readonly StudyGroup studyGroup;

            public TransitionAction_AddStudentsToLord(StudyGroup studyGroup)
            {
                this.studyGroup = studyGroup;
            }

            public override void DoAction(Transition trans)
            {
                var lord = trans.target.lord;
                if (lord != null)
                {
                    var studentRole = studyGroup.GetStudentRole();

                    foreach (var student in studyGroup.students)
                    {
                        var studentQualification = studentRole.CanAcceptPawn(student);
                        if (studentQualification.Accepted)
                        {
                            if (student.GetLord() == null)
                            {
                                lord.AddPawn(student);
                                EducationLog.Message($"Added student {student.LabelShort} to lord for class '{studyGroup.className}'");
                            }
                        }
                    }
                }
            }
        }
    }
}
