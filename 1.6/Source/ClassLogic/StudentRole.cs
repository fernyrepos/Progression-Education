using Verse;

namespace ProgressionEducation;

public class StudentRole(StudyGroup studyGroup)
    : ClassRole(
        "Student",
        99,
        1,
        "PE_StudentRole".Translate(),
        "PE_StudentRole".Translate(),
        studyGroup
    )
{
    public override AcceptanceReport CanAcceptPawn(Pawn pawn)
    {
        var baseReport = base.CanAcceptPawn(pawn);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        return studyGroup?.subjectLogic?.IsStudentQualified(pawn) ?? AcceptanceReport.WasRejected;
    }

    public override float ScoreFor(Pawn pawn)
    {
        return studyGroup?.subjectLogic?.CalculateStudentScore(pawn)
               ?? base.ScoreFor(pawn);
    }
}