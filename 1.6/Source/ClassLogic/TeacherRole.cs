using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class TeacherRole : ClassRole
    {
        public TeacherRole(StudyGroup studyGroup) : base("teacher", 1, 1, "PE_TeacherRole".Translate(), "PE_TeacherRole".Translate(), studyGroup)
        {
        }

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            var baseReport = base.CanAcceptPawn(pawn);
            if (!baseReport.Accepted)
            {
                return baseReport;
            }
            if (pawn.DevelopmentalStage != DevelopmentalStage.Adult)
            {
                return new AcceptanceReport("PE_TeacherRoleOnlyForAdults".Translate());
            }

            if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            {
                return new AcceptanceReport("PE_TeacherRoleRequiresSocialSkill".Translate());
            }
            return studyGroup.subjectLogic != null ? studyGroup.subjectLogic.IsTeacherQualified(pawn) : (AcceptanceReport)false;
        }
    }
}