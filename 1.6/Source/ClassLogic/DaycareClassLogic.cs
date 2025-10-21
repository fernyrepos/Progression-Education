using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class DaycareClassLogic : ClassSubjectLogic
    {
        public DaycareClassLogic() { }
        public DaycareClassLogic(StudyGroup parent) : base(parent) { }

        public override string Description => "PE_Daycare".Translate();

        public override bool IsInfinite => true;

        public override void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog)
        {
        }

        public override float CalculateProgressPerTick()
        {
            return 0f;
        }

        public override void GrantCompletionRewards()
        {
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            return teacher.DevelopmentalStage >= DevelopmentalStage.Adult;
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
            return true;
        }

        public override float LearningSpeedModifier => EducationSettings.Instance.daycareClassesLearningSpeedModifier;

        public override void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver)
        {
            var pawn = jobDriver.pawn;
            var taughtSkill = jobDriver.taughtSkill;
            if (taughtSkill is null)
            {
                taughtSkill = jobDriver.taughtSkill = ChooseSkill(studyGroup) ?? SkillDefOf.Social;
            }
            if (pawn.IsHashIntervalTick(900))
            {
                pawn.interactions.TryInteractWith(student, taughtSkill.lessonInteraction);
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.GaveLesson, student);
                student.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.WasTaught, pawn);
            }
            if (student.skills.GetSkill(taughtSkill).TotallyDisabled)
            {
                return;
            }
            float num = LearningDesireDefOf.Lessontaking.xpPerTick * LearningUtility.LearningRateFactor(student) * this.studyGroup.classroom.CalculateLearningModifier() * LearningSpeedModifier;
            student.skills.Learn(taughtSkill, num * 3f);
        }

        private SkillDef ChooseSkill(StudyGroup studyGroup)
        {
            var availableSkills = studyGroup.teacher.skills.skills
                .Where(s => !s.TotallyDisabled && s.def.lessonInteraction != null &&
                           studyGroup.students.Any(st => !st.skills.GetSkill(s.def).TotallyDisabled))
                .OrderByDescending(s => s.Level)
                .Take(4)
                .ToList();

            if (availableSkills.Any())
            {
                return availableSkills.RandomElement().def;
            }
            return null;
        }

        public override float CalculateTeacherScore(Pawn p)
        {
            return p.GetStatValue(StatDefOf.SocialImpact, true);
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            return "";
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            return "";
        }
        
        public override void HandleStudentLifecycleEvents()
        {
            List<Pawn> studentsToRemove = [];
            foreach (var student in studyGroup.students)
            {
                if (student.DevelopmentalStage != DevelopmentalStage.Child)
                {
                    studentsToRemove.Add(student);
                }
            }
            foreach (var student in studentsToRemove)
            {
                studyGroup.RemoveStudent(student);
            }
        }
    }
}
