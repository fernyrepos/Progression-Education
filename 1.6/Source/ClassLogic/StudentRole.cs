using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class StudentRole : ClassRole
    {
        public StudentRole(StudyGroup studyGroup) : base("Student", 99, 1, "PE_StudentRole".Translate(), "PE_StudentRole".Translate(), studyGroup)
        {
        }

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            var baseReport = base.CanAcceptPawn(pawn);
            if (!baseReport.Accepted)
            {
                return baseReport;
            }
            return studyGroup.subjectLogic != null ? studyGroup.subjectLogic.IsStudentQualified(pawn) : (AcceptanceReport)false;
        }
    }
}