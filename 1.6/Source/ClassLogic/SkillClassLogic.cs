using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class SkillClassLogic : ClassSubjectLogic
    {
        public override float LearningSpeedModifier => EducationSettings.Instance.skillClassesLearningSpeedModifier;
        private SkillDef skillFocus;
        public SkillDef SkillFocus
        {
            get => skillFocus;
            set
            {
                if (skillFocus != value)
                {
                    skillFocus = value;
                    validLearningBenches = null;
                }
            }
        }

        public SkillClassLogic() : base() { }
        public SkillClassLogic(StudyGroup studyGroup) : base(studyGroup) { }

        public SkillClassLogic(SkillClassLogic other, StudyGroup studyGroup) : base(other, studyGroup)
        {
            skillFocus = other.skillFocus;
        }

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

        public override float CalculateTeacherScore(Pawn teacher)
        {
            if (teacher == null
                || skillFocus == null
                || !studyGroup.GetTeacherRole().CanAcceptPawn(teacher))
            {
                return 0f;
            }
            var social = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            var intelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            var socialImpact = teacher.GetStatValue(StatDefOf.SocialImpact);
            var relevantSkill = teacher.skills.GetSkill(skillFocus).Level;
            var passionBonus = CalculatePassionBonus(teacher);
            var score = ((social * 0.25f) + (intelligence * 0.25f) + (relevantSkill * 0.5f)) * socialImpact;
            return score * 0.02f + passionBonus * 0.03f;
        }

        private float CalculatePassionBonus(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }
            var learnRateFactor = pawn.skills.GetSkill(skillFocus).LearnRateFactor(direct: true);
            return Mathf.Min(2f, learnRateFactor);
        }

        public override float CalculateProgressPerTick(Pawn teacher)
        {
            if (SkillFocus == null || teacher == null || studyGroup.classroom == null)
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
            return SkillFocus != null && teacher.skills.GetSkill(SkillFocus).Level < 5
                ? new AcceptanceReport("PE_TeacherNotQualifiedSkill".Translate(teacher.LabelShort, SkillFocus.LabelCap))
                : AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            if (student.DevelopmentalStage < DevelopmentalStage.Child)
            {
                return new AcceptanceReport("PE_TooYoung".Translate(student.LabelShortCap));
            }
            if (SkillFocus != null && student.skills.GetSkill(SkillFocus).TotallyDisabled)
            {
                return new AcceptanceReport("PE_StudentSkillDisabled".Translate(student.LabelShort, SkillFocus.LabelCap));
            }
            if (studyGroup.currentProgress > 0f && !studyGroup.students.NotNullAndContains(student))
            {
                return new AcceptanceReport("PE_CannotAddOngoing".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }

        public override bool CanAutoAssign => base.CanAutoAssign && SkillFocus != null;

        public override void ApplyLearningTick(Pawn student, int delta)
        {
            base.ApplyLearningTick(student, delta);
            var xpGain = CalculateProgressPerTick(studyGroup.teacher);
            student.skills.Learn(SkillFocus, xpGain * delta);
        }

        public override void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
        {
            jobDriver.taughtSkill ??= SkillFocus;
            base.ApplyTeachingTick(student, jobDriver, delta);
        }


        public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
            DrawSkillUI(rect, ref curY, classDialog);
            DrawSemesterGoalUI(rect, ref curY, classDialog);
            var progressPerTick = CalculateProgressPerTick(studyGroup.teacher);
            if (progressPerTick > 0)
            {
                var progress = studyGroup.semesterGoal - studyGroup.currentProgress;
                var estimatedTicks = (int)(progress / progressPerTick);
                Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
                curY += 30f;
                var sessionNeeded = Mathf.CeilToInt((float)estimatedTicks / (GenDate.TicksPerHour * studyGroup.Duration));
                Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudySessionsNeeded".Translate(sessionNeeded));
                curY += 30f;
            }
        }

        private void DrawSkillUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
            switch (classDialog)
            {
                case Dialog_CreateClass:
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SkillFocus".Translate());
                    if (Widgets.ButtonText(new Rect(rect.x + 160f, curY, 200f, 25f), SkillFocus?.LabelCap ?? "PE_Select".Translate()))
                    {
                        List<FloatMenuOption> options = [];
                        foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
                        {
                            options.Add(new FloatMenuOption(skillDef.LabelCap, () =>
                            {
                                SkillFocus = skillDef;
                                studyGroup.subjectLogic.AutoAssignStudents(classDialog);
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    curY += 30f;
                    break;
                case Dialog_EditClass:
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SkillFocus".Translate());
                    Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f), SkillFocus?.LabelCap ?? "NoneBrackets".Translate());
                    curY += 30f;
                    break;
            }
        }

        private void DrawSemesterGoalUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
            switch (classDialog)
            {
                case Dialog_CreateClass:
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(rect.x, curY, 360f, 25f), 
                        "PE_SemesterGoal".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += 30f;
                    var sliderRectCreate = new Rect(rect.x, curY, 360f, 25f);
                    studyGroup.semesterGoal = (int)Widgets.HorizontalSlider(sliderRectCreate, 
                        studyGroup.semesterGoal, 
                        min: 1000, 
                        max: 100000,
                        leftAlignedLabel: "PE_Xp".Translate(1000.ToString("N0")),
                        rightAlignedLabel: "PE_Xp".Translate(100000.ToString("N0")), 
                        roundTo: 1000);
                    curY += 30f;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(rect.x, curY - 15f, 360f, 25f), 
                        "PE_Xp".Translate(studyGroup.semesterGoal.ToString("N0")));
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += 20f;
                    break;
                case Dialog_EditClass:
                    if (studyGroup.currentProgress > 0)
                    {
                        Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SemesterProgress".Translate());
                        Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f), 
                            "PE_Xp".Translate(studyGroup.currentProgress.ToString("N0")));
                        curY += 30f;
                        Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SemesterGoal".Translate());
                    }
                    else
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(new Rect(rect.x, curY, 360f, 25f), 
                            "PE_SemesterGoal".Translate());
                        Text.Anchor = TextAnchor.UpperLeft;
                        curY += 30f;
                        var sliderRectEdit = new Rect(rect.x, curY, 360f, 25f);
                        studyGroup.semesterGoal = (int)Widgets.HorizontalSlider(sliderRectEdit, 
                            studyGroup.semesterGoal, 
                            min: 1000, 
                            max: 100000,
                            leftAlignedLabel: "PE_Xp".Translate(1000.ToString("N0")),
                            rightAlignedLabel: "PE_Xp".Translate(100000.ToString("N0")), 
                            roundTo: 1000);
                    }
                    curY += 30f;
                    Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f), "PE_Xp".Translate(studyGroup.semesterGoal.ToString("N0")));
                    curY += 30f;
                    break;
            }
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            if (pawn == null)
            {
                return "";
            }
            var text = new StringBuilder(base.TeacherTooltipFor(pawn));
            text.AppendLineIfNotEmpty();
            if (studyGroup != null && skillFocus != null)
            {
                if (pawn.skills.GetSkill(skillFocus) is SkillRecord relevantSkill)
                {
                    text.AppendInNewLine(relevantSkill.def.LabelCap);
                    text.Append(": ");
                    text.Append(relevantSkill.Level);
                }
                if (skillFocus != SkillDefOf.Social 
                    && pawn.skills.GetSkill(SkillDefOf.Social) is SkillRecord social)
                {
                    text.AppendInNewLine(SkillDefOf.Social.LabelCap);
                    text.Append(": ");
                    text.Append(social.Level);
                }
                if (skillFocus != SkillDefOf.Intellectual 
                    && pawn.skills.GetSkill(SkillDefOf.Intellectual) is SkillRecord intellectual)
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
                if (CalculatePassionBonus(pawn) is var passionBonus and > 0)
                {
                    text.AppendInNewLine("PE_SkillPassionBonus".Translate());
                    text.Append(": ");
                    text.Append(passionBonus.ToString("F1"));
                }
                text.AppendLineIfNotEmpty();
                var baseXpPerTick = CalculateTeacherScore(pawn);
                if (baseXpPerTick > 0f)
                {
                    var xpPerHour = baseXpPerTick * GenDate.TicksPerHour;
                    text.AppendInNewLine("PE_TeachingHourlyBase".Translate());
                    text.Append(": ");
                    text.Append(xpPerHour.ToString("F0"));
                    text.Append("PE_XpPerHour".Translate());
                }
            }
            return text.ToString();
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            var text = new StringBuilder();
            if (studyGroup is { classroom: not null, Map: not null }
                && SkillFocus != null
                && pawn?.skills?.GetSkill(SkillFocus) is SkillRecord relevantSkill)
            {
                text.AppendInNewLine($"{relevantSkill.def.LabelCap}: {relevantSkill.Level} ({relevantSkill.passion.GetLabel()})");
                text.AppendLineIfNotEmpty();
                var baseXpPerTick = CalculateProgressPerTick(studyGroup.teacher);
                var learningFactor = pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
                var skillLearnFactor = relevantSkill.LearnRateFactor(direct: true);
                var learningXpPerTick = baseXpPerTick * learningFactor * skillLearnFactor;
                if (learningXpPerTick > 0f)
                {
                    var learningXpPerHour = learningXpPerTick * GenDate.TicksPerHour;
                    text.AppendInNewLine($"{"PE_EstimatedHourlyProgress".Translate()}: {learningXpPerHour:F0} {"PE_XpPerHour".Translate()}");
                }
            }
            text.AppendInNewLine(base.StudentTooltipFor(pawn));
            return text.ToString();
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
            Scribe_Defs.Look(ref skillFocus, "skillFocus");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (skillFocus is null)
                {
                    EducationManager.Instance.RemoveStudyGroup(studyGroup);
                    Log.Error(studyGroup?.className + " had no skill focus, removing it...");
                }
            }
        }

        public override HashSet<ThingDef> GetValidLearningBenches()
        {
            if (validLearningBenches == null)
            {
                validLearningBenches = [];
                var requirement = SkillFocus?.GetModExtension<SkillBuildingRequirement>();
                if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
                {
                    validLearningBenches.UnionWith(requirement.requiredBuildings);
                }
            }
            return validLearningBenches;
        }

    }
}
