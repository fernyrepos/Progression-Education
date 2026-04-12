using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class LordToil_AttendClass(StudyGroup studyGroup) : LordToil
{
    private bool partialAttendanceWarningShown;

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (studyGroup.IsCompleted)
        {
            EducationLog.Message(
                $"Class '{
                    studyGroup.className
                }' has completed its semester goal. Granting rewards and ending lord.");
            studyGroup.subjectLogic.GrantCompletionRewards();

            var label = "PE_ClassCompleted".Translate(studyGroup.className);
            var text = new StringBuilder("PE_ClassCompletedDesc".Translate(studyGroup.className));
            var graduates = new StringBuilder();
            foreach (var student in studyGroup.students)
            {
                graduates.AppendWithComma(student.LabelShort);
            }

            text.AppendLineIfNotEmpty();
            text.AppendInNewLine("PE_ClassGraduates".Translate(graduates.ToString()));
            Find.LetterStack.ReceiveLetter(label, text.ToString(),
                LetterDefOf.PositiveEvent);
            EducationManager.Instance.RemoveStudyGroup(studyGroup);
            lord.ReceiveMemo("ClassCompleted");
        }
        else
        {
            if (studyGroup.ClassIsActive())
            {
                var lordJob = lord.LordJob as LordJob_AttendClass;
                lordJob?.classStartedSuccessfully = true;
            }

            if (!partialAttendanceWarningShown
                && studyGroup.subjectLogic is SkillClassLogic)
            {
                if (studyGroup.teacher?.jobs?.curDriver
                        is JobDriver_Teach { waitingTicks: >= StudyGroup.MaxTeacherWaitingTicks }
                    && !studyGroup.AllStudentsPresentAndAttending())
                {
                    Messages.Message(
                        "PE_ClassPartiallyFunctioningWarning".Translate(studyGroup.className),
                        MessageTypeDefOf.CautionInput);
                    partialAttendanceWarningShown = true;
                }
            }

            foreach (var student in studyGroup.students)
            {
                if (!GatheringsUtility.PawnCanStartOrContinueGathering(student)
                    || student.CurJob is not Job job
                    || job.def == DefsOf.PE_AttendClass
                    || !student.mindState.IsIdle)
                {
                    continue;
                }

                student.jobs.StopAll();
                EducationLog.Message(
                    $"-> Stopped job for student {
                        student.LabelShort
                    } because it was not PE_AttendClass");
                if (lord.ownedPawns.Contains(student)
                    || !CanAddPawn(student))
                {
                    continue;
                }

                EducationLog.Message(
                    $"-> Adding student {
                        student.LabelShort
                    } to Lord PE_AttendClass because it was orphaned");
                lord.AddPawn(student);
            }
        }
    }

    public override void UpdateAllDuties()
    {
        EducationLog.Message(
            $"LordToil_AttendClass.UpdateAllDuties called for class '{studyGroup.className}'");
        if (!studyGroup.teacher.CanAttendClass())
        {
            lord.ReceiveMemo(LordJob_AttendClass.MemoClassCancelledTeacherIncapacitated);
            return;
        }

        studyGroup.teacher.mindState.duty =
            new PawnDuty(DefsOf.PE_TeachDuty, studyGroup.teacher.Position);
        EducationLog.Message(
            $"-> Set teacher {
                studyGroup.teacher.LabelShort
            } duty to PE_TeachDuty at position {
                studyGroup.teacher.Position
            }");

        foreach (var student in studyGroup.students)
        {
            if (!GatheringsUtility.PawnCanStartOrContinueGathering(student))
            {
                lord.RemovePawn(student);
            }
            else
            {
                student.mindState.duty = new PawnDuty(DefsOf.PE_AttendClassDuty,
                    student.Position);
                EducationLog.Message(
                    $"-> Set student {
                        student.LabelShort
                    } duty to PE_AttendClassDuty at position {
                        student.Position
                    }");
            }
        }

        EducationLog.Message(
            $"-> Finished setting duties for {studyGroup.students.Count} students");
    }
}