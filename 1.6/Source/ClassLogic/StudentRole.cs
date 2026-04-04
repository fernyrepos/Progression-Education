using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class StudentRole(StudyGroup studyGroup)
        : ClassRole(roleId: "Student", 
            maxCount:99, 
            minCount: 1,
            label: "PE_StudentRole".Translate(),
            categoryLabel: "PE_StudentRole".Translate(),
            studyGroup: studyGroup)
    {
        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            var baseReport = base.CanAcceptPawn(pawn);
            if (!baseReport.Accepted)
            {
                return baseReport;
            }
            return studyGroup.subjectLogic?.IsStudentQualified(pawn) 
                   ?? AcceptanceReport.WasRejected;
        }
    }
}