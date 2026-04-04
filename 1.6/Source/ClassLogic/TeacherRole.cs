using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class TeacherRole(StudyGroup studyGroup) : 
        ClassRole(roleId: "teacher",
           maxCount: 1,
           minCount: 1,
           label: "PE_TeacherRole".Translate(),
           categoryLabel: "PE_TeacherRole".Translate(),
           studyGroup: studyGroup)
    {
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
            return studyGroup?.subjectLogic?.IsTeacherQualified(pawn) 
                   ?? AcceptanceReport.WasAccepted;
        }
    }
}