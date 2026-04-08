using Verse;

namespace ProgressionEducation;

public class TeacherRole(StudyGroup studyGroup)
    : ClassRole(
        "teacher",
        1,
        1,
        "PE_TeacherRole".Translate(),
        "PE_TeacherRole".Translate(),
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

        return studyGroup?.subjectLogic?.IsTeacherQualified(pawn)
               ?? AcceptanceReport.WasRejected;
    }

    public override float ScoreFor(Pawn pawn)
    {
        return studyGroup?.subjectLogic?.CalculateTeacherScore(pawn)
               ?? base.ScoreFor(pawn);
    }
}