using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class LordToil_AttendClass : LordToil
    {
        private readonly StudyGroup studyGroup;
        private bool partialAttendanceWarningShown = false;

        public LordToil_AttendClass(StudyGroup studyGroup)
        {
            this.studyGroup = studyGroup;
        }

        public override void UpdateAllDuties()
        {
            EducationLog.Message($"LordToil_AttendClass.UpdateAllDuties called for class '{studyGroup.className}'");
            studyGroup.teacher.mindState.duty = new PawnDuty(DefsOf.PE_TeachDuty, studyGroup.teacher.Position);
            EducationLog.Message($"-> Set teacher {studyGroup.teacher.LabelShort} duty to PE_TeachDuty at position {studyGroup.teacher.Position}");

            foreach (var student in studyGroup.students)
            {
                student.mindState.duty = new PawnDuty(DefsOf.PE_AttendClassDuty, student.Position);
                EducationLog.Message($"-> Set student {student.LabelShort} duty to PE_AttendClassDuty at position {student.Position}");
            }
            EducationLog.Message($"-> Finished setting duties for {studyGroup.students.Count} students");
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if (studyGroup.IsCompleted)
            {
                EducationLog.Message($"Class '{studyGroup.className}' has completed its semester goal. Granting rewards and ending lord.");
                studyGroup.subjectLogic.GrantCompletionRewards();

                string label = "PE_ClassCompleted".Translate(studyGroup.className);
                string text = "PE_ClassCompletedDesc".Translate(studyGroup.className);
                string graduates = "";
                foreach (var student in studyGroup.students)
                {
                    if (!string.IsNullOrEmpty(graduates))
                    {
                        graduates += ", ";
                    }

                    graduates += student.LabelShort;
                }

                text += "\n\n" + "PE_ClassGraduates".Translate(graduates);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent);
                EducationManager.Instance.RemoveStudyGroup(studyGroup);
                lord.ReceiveMemo("ClassCompleted");
            }
            else
            {
                if (studyGroup.ClassIsActive())
                {
                    var lordJob = lord.LordJob as LordJob_AttendClass;
                    lordJob.classStartedSuccessfully = true;
                }

                if (!partialAttendanceWarningShown && studyGroup.subjectLogic is SkillClassLogic)
                {
                    var teacherJobDriver = studyGroup.teacher?.jobs?.curDriver as JobDriver_Teach;
                    if (teacherJobDriver != null && teacherJobDriver.waitingTicks >= StudyGroup.MaxTeacherWaitingTicks && studyGroup.AllStudentsPresent() is false)
                    {
                        Messages.Message("PE_ClassStartedWithoutAllStudents".Translate(studyGroup.className), MessageTypeDefOf.CautionInput);
                        partialAttendanceWarningShown = true;
                    }
                }

                foreach (var student in studyGroup.students)
                {
                    if (student.CurJob is Job job && job.def != DefsOf.PE_AttendClass && student.mindState.IsIdle)
                    {
                        student.jobs.StopAll();
                        EducationLog.Message($"-> Stopped job for student {student.LabelShort} because it was not PE_AttendClass");
                    }
                }
            }
        }
    }
}
