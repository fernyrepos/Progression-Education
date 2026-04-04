using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class DaycareClassLogic : ClassSubjectLogic
    {
        public DaycareClassLogic() { }
        public DaycareClassLogic(StudyGroup parent) : base(parent) { }

        public DaycareClassLogic(DaycareClassLogic other, StudyGroup parent) : base(other, parent) { }

        public override string Description => "PE_Daycare".Translate();

        public override bool IsInfinite => true;

        public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
        }

        public override float CalculateProgressPerTick(Pawn teacher)
        {
            if (teacher == null || studyGroup.classroom == null)
            {
                return 0f;
            }
            var progress = CalculateTeacherScore(teacher);
            var classroomModifier = studyGroup.classroom.CalculateLearningModifier();
            progress *= classroomModifier;
            progress *= LearningSpeedModifier;
            return progress;
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            if (teacher.DevelopmentalStage < DevelopmentalStage.Adult)
            {
                return new AcceptanceReport("PE_TeacherRoleOnlyForAdults".Translate());
            }
            var skill = teacher.skills.GetSkill(SkillDefOf.Social);
            if (skill.TotallyDisabled)
            {
                return new AcceptanceReport("PE_TeacherSkillDisabled".Translate(teacher.LabelShortCap, skill.def.LabelCap));
            }
            return AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            if (student.DevelopmentalStage != DevelopmentalStage.Child)
            {
                return new AcceptanceReport("PE_MustBeChild".Translate());
            }
            if (student.needs?.learning == null)
            {
                return new AcceptanceReport("PE_NoLearningNeed".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }

        public override float LearningSpeedModifier => EducationSettings.Instance.daycareClassesLearningSpeedModifier;

        public override void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
        {
            base.ApplyTeachingTick(student, jobDriver, delta);
            jobDriver.taughtSkill ??= ChooseSkillDef();
            if (jobDriver.taughtSkill is not SkillDef taughtSkill
                || student?.skills?.GetSkill(taughtSkill) is not SkillRecord)
            {
                return;
            }

            var xpPerTick = CalculateProgressPerTick(studyGroup.teacher);
            student.skills.Learn(taughtSkill, xpPerTick * delta);
        }

        public override float CalculateLearningPerTick(Pawn student)
        {
            return 2f * base.CalculateLearningPerTick(student);
        }

        private IEnumerable<SkillRecord> AvailableStudySkills
        {
            get
            {
                if (studyGroup?.students == null || studyGroup?.teacher == null)
                {
                    yield break;
                }

                var disabledSkillDefs = studyGroup.students
                    .SelectMany(student => student.skills.skills
                        .Where(s => !s.TotallyDisabled)
                        .Select(s => s.def))
                    .Union(studyGroup.teacher.skills.skills
                        .Where(s => !s.TotallyDisabled)
                        .Select(s => s.def)).Distinct();

                foreach (var skillRecord in studyGroup.teacher.skills.skills
                    .Where(s => !disabledSkillDefs
                        .Contains(s.def)))
                {
                    yield return skillRecord;
                }
            }
        }

        private IEnumerable<SkillRecord> BestAvailableStudySkills
        {
            get
            {
                return AvailableStudySkills
                    .OrderByDescending(s => s)
                    .Take(4);
            }
        }

        private SkillDef ChooseSkillDef()
        {
            return BestAvailableStudySkills
                .Select(s => s.def)
                .RandomElementWithFallback(SkillDefOf.Social);
        }

        public override float CalculateTeacherScore(Pawn teacher)
        {
            if (teacher == null || studyGroup.classroom == null)
            {
                return 0f;
            }
            var social = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            var intelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            var socialImpact = teacher.GetStatValue(StatDefOf.SocialImpact);
            var score = ((social * 0.5f) + (intelligence * 0.5f)) * socialImpact;
            return score * 0.02f;
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            if (pawn == null)
            {
                return "";
            }
            var text = new StringBuilder(base.TeacherTooltipFor(pawn));
            text.AppendLineIfNotEmpty();
            if (pawn.skills?.GetSkill(SkillDefOf.Social) is SkillRecord social)
            {
                text.AppendInNewLine(SkillDefOf.Social.LabelCap);
                text.Append(": ");
                text.Append(social.Level);
            }
            if (pawn.skills?.GetSkill(SkillDefOf.Intellectual) is SkillRecord intellectual)
            {
                text.AppendInNewLine(SkillDefOf.Intellectual.LabelCap);
                text.Append(": ");
                text.Append(intellectual.Level);
            }
            if (pawn.GetStatValue(StatDefOf.SocialImpact) is var socialImpact)
            {
                text.AppendInNewLine(StatDefOf.SocialImpact.LabelCap);
                text.Append(": ");
                text.Append(socialImpact.ToStringPercent());
            }
            text.AppendLineIfNotEmpty();
            if (CalculateProgressPerTick(pawn) is var xpPerTick and > 0f)
            {
                var xpPerHour = xpPerTick * GenDate.TicksPerHour;
                text.AppendInNewLine("PE_TeachingHourlyBase".Translate());
                text.Append(": ");
                text.Append(xpPerHour.ToString("F0"));
                text.Append("PE_XpPerHour".Translate());
            }
            return text.ToString();
        }

        public override void HandleStudentLifecycleEvents()
        {
            foreach (var student in studyGroup.students
                         .Where(student => student.DevelopmentalStage != DevelopmentalStage.Child))
            {
                studyGroup.RemoveStudent(student);
            }
        }
    }
}
