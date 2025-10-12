using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class SkillClassLogic : ClassSubjectLogic
    {
        private SkillDef _skillFocus;
        public SkillDef SkillFocus
        {
            get => _skillFocus;
            set
            {
                if (_skillFocus != value)
                {
                    _skillFocus = value;
                    _validLearningBenches = null;
                }
            }
        }

        public SkillClassLogic() : base() { }
        public SkillClassLogic(StudyGroup studyGroup) : base(studyGroup) { }

        public override string Description => "PE_TrainingSkill".Translate(SkillFocus.label);

        public override string BenchLabel
        {
            get
            {
                var validBenches = GetValidLearningBenches();
                if (validBenches != null && validBenches.Any())
                {
                    return validBenches.Select(b => b.label).ToCommaList();
                }
                return null;
            }
        }

        public override void GrantCompletionRewards()
        {
        }

        public override float CalculateTeacherScore(Pawn teacher)
        {
            if (SkillFocus == null)
            {
                return 0f;
            }
            float teacherSocial = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            float relevantSkill = teacher.skills.GetSkill(SkillFocus).Level;
            float teacherIntelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            var score = (teacherSocial * 0.4f) + (relevantSkill * 0.3f) + (teacherIntelligence * 0.3f);
            return score * 0.05f;
        }

        public override float CalculateProgressPerTick()
        {
            if (SkillFocus == null || studyGroup.teacher == null)
            {
                return 0f;
            }
            var classroom = studyGroup.classroom;
            float classRoomModifier = classroom.CalculateLearningModifier();
            float modMultiplier = EducationSettings.Instance.skillClassesLearningSpeedModifier;
            float progress = CalculateTeacherScore(studyGroup.teacher) * classRoomModifier * modMultiplier;
            return progress;
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            return SkillFocus != null && teacher.skills.GetSkill(SkillFocus).Level < 5
                ? new AcceptanceReport("PE_TeacherNotQualifiedSkill".Translate(teacher.LabelShort, SkillFocus.LabelCap))
                : AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            return SkillFocus != null && student.skills.GetSkill(SkillFocus).TotallyDisabled
                ? new AcceptanceReport("PE_StudentSkillDisabled".Translate(student.LabelShort, SkillFocus.LabelCap))
                : AcceptanceReport.WasAccepted;
        }

        public override bool CanAutoAssign => base.CanAutoAssign && SkillFocus != null;

        public override void ApplyLearningTick(Pawn student)
        {
            base.ApplyLearningTick(student);
            float xpGain = CalculateProgressPerTick();
            student.skills.Learn(SkillFocus, xpGain, false);
        }

        public override void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog)
        {
            Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SkillFocus".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 160f, curY, 200f, 25f), SkillFocus?.LabelCap ?? "PE_Select".Translate()))
            {
                List<FloatMenuOption> options = [];
                foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
                {
                    options.Add(new FloatMenuOption(skillDef.LabelCap, () =>
                    {
                        SkillFocus = skillDef;
                        studyGroup.subjectLogic.AutoAssignStudents(createClassDialog);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 30f;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_SemesterGoal".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 30f;

            var sliderRect = new Rect(rect.x, curY, 360f, 25f);
            int newSemesterGoal = (int)Widgets.HorizontalSlider(sliderRect, studyGroup.semesterGoal, 1000, 100000, false, null, "PE_Xp".Translate(1000.ToString("N0")), "PE_Xp".Translate(100000.ToString("N0")), 100);
            if (newSemesterGoal != studyGroup.semesterGoal)
            {
                studyGroup.semesterGoal = newSemesterGoal;
            }
            curY += 30f;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, curY - 15f, 360f, 25f), "PE_Xp".Translate(studyGroup.semesterGoal.ToString("N0")));
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 20f;

            if (studyGroup.classroom != null)
            {
                float progressPerTick = studyGroup.CalculateProgressPerTick();
                if (progressPerTick > 0)
                {
                    int estimatedTicks = (int)(studyGroup.semesterGoal / progressPerTick);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += 30f;
                }
            }

            DrawBenchRequirementUI(rect, ref curY);
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            var social = pawn.skills.GetSkill(SkillDefOf.Social);
            var intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
            var relevantSkill = SkillFocus != null ? pawn.skills.GetSkill(SkillFocus) : null;
            string text = $"{social.def.LabelCap}: {social.Level}\n{intellectual.def.LabelCap}: {intellectual.Level}";
            if (relevantSkill != null && relevantSkill != social && relevantSkill != intellectual)
            {
                text += $"\n{relevantSkill.def.LabelCap}: {relevantSkill.Level}";
            }
            var map = studyGroup.Map;
            if (studyGroup.classroom != null && map != null)
            {
                float progressPerTick = CalculateProgressPerTick();
                if (progressPerTick > 0f)
                {
                    float num = progressPerTick * 2500f;
                    text += "\n\n" + "PE_EstimatedHourlyProgress".Translate() + ": " + num.ToString("F0") + " " + "PE_XpPerHour".Translate();
                }
            }
            return text;
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            if (SkillFocus != null)
            {
                return $"{SkillFocus.LabelCap}: {pawn.skills.GetSkill(SkillFocus).Level}";
            }
            return null;
        }

        public override JobDef LearningJob
        {
            get
            {
                if (SkillFocus == SkillDefOf.Melee)
                {
                    return DefsOf.PE_AttendMeleeClass;
                }
                if (SkillFocus == SkillDefOf.Shooting)
                {
                    return DefsOf.PE_AttendShootingClass;
                }
                return DefsOf.PE_AttendClass;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref _skillFocus, "skillFocus");
        }

        public override HashSet<ThingDef> GetValidLearningBenches()
        {
            if (_validLearningBenches == null)
            {
                _validLearningBenches = [];
                var requirement = SkillFocus?.GetModExtension<SkillBuildingRequirement>();
                if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
                {
                    _validLearningBenches.UnionWith(requirement.requiredBuildings);
                }
            }
            return _validLearningBenches;
        }
    }
}
