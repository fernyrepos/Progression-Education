using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class SkillClassLogic : ClassSubjectLogic
{
    private SkillDef skillFocus;

    public SkillClassLogic()
    {
    }

    public SkillClassLogic(StudyGroup parent)
        : base(parent)
    {
    }

    public override string BenchLabel =>
        GetValidLearningBenches()?.Select(b => b.label)?.ToCommaList() ?? "";

    public override string Description => "PE_TrainingSkill".Translate(SkillFocus.LabelCap);

    public override string Label => "PE_SubjectSkill".Translate();

    public override string LabelFocus =>
        SkillFocus?.LabelCap ?? "None".Translate().CapitalizeFirst();

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

    public override float LearningSpeedModifier =>
        EducationSettings.Instance.skillClassesLearningSpeedModifier;

    public override float ProgressPerTick
    {
        get
        {
            if (SkillFocus == null
                || studyGroup.teacher == null
                || studyGroup.classroom == null)
            {
                return 0f;
            }

            return Mathf.Max(0,
                CalculateTeacherScore(studyGroup.teacher)
                * studyGroup.classroom.ClassSpeed
                * LearningSpeedModifier);
        }
    }

    public SkillDef SkillFocus
    {
        get => skillFocus;
        set
        {
            if (skillFocus != value)
            {
                skillFocus = value;
                cachedValidLearningBenches = null;
            }
        }
    }

    public override void ApplyLearningTick(Pawn student, int delta)
    {
        base.ApplyLearningTick(student, delta);
        student.skills.Learn(SkillFocus, ProgressPerTick * delta);
    }

    public override void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
    {
        jobDriver.taughtSkill ??= SkillFocus;
        base.ApplyTeachingTick(student, jobDriver, delta);
    }

    private float CalculatePassionBonus(Pawn pawn)
    {
        if (pawn == null)
        {
            return 0f;
        }

        var learnRateFactor = pawn.skills.GetSkill(skillFocus).LearnRateFactor(true);
        return Mathf.Min(2f, learnRateFactor);
    }

    public override float CalculateStudentScore(Pawn student)
    {
        if (student == null
            || skillFocus == null
            || !IsStudentQualified(student))
        {
            return 0f;
        }

        var relevantSkill = student.skills?.GetSkill(SkillFocus);
        var learningFactor = student.GetStatValue(StatDefOf.GlobalLearningFactor);
        var skillLearnFactor = relevantSkill?.LearnRateFactor(true) ?? 0f;
        return Mathf.Max(0, learningFactor * skillLearnFactor);
    }

    public override float CalculateTeacherScore(Pawn teacher)
    {
        if (teacher == null
            || skillFocus == null
            || !IsTeacherQualified(teacher))
        {
            return 0f;
        }

        var social = teacher.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
        var intelligence = teacher.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        var relevantSkill = teacher.skills?.GetSkill(SkillFocus)?.Level ?? 0;
        var socialImpact = CalculateSocialImpactFactor(teacher);
        var passionBonus = CalculatePassionBonus(teacher);
        var score = (social * 0.25f + intelligence * 0.25f + relevantSkill * 0.5f) * socialImpact;
        return Mathf.Max(0,
            score * 0.02f + passionBonus * 0.03f);
    }

    public override ClassSubjectLogic DeepClone(StudyGroup parent)
    {
        return new SkillClassLogic(parent)
        {
            skillFocus = skillFocus,
        };
    }


    public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
    {
        DrawSkillUI(rect, ref curY, classDialog);
        DrawSemesterGoalUI(rect, ref curY, classDialog);
        var progressPerTick = ProgressPerTick;
        if (progressPerTick <= 0)
        {
            return;
        }

        var progress = studyGroup.semesterGoal - studyGroup.currentProgress;
        var estimatedTicks = (int)(progress / progressPerTick);
        Widgets.Label(new Rect(rect.x, curY, 360f, 25f),
            "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
        curY += 30f;
        var sessionsNeeded = Mathf.Ceil(
            (float)estimatedTicks / (GenDate.TicksPerHour * studyGroup.Duration));
        Widgets.Label(new Rect(rect.x, curY, 360f, 25f),
            "PE_StudySessionsNeeded".Translate(sessionsNeeded.ToString("F0")
                .Colorize(ColoredText.DateTimeColor)));

        curY += 30f;
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
                studyGroup.semesterGoal = (int)Widgets.HorizontalSlider(
                    sliderRectCreate,
                    studyGroup.semesterGoal,
                    1000,
                    100000,
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
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                        "PE_SemesterProgress".Translate());
                    Widgets.Label(
                        new Rect(rect.x + 160f, curY, 200f, 25f),
                        "PE_Xp".Translate(studyGroup.currentProgress.ToString("N0")));
                    curY += 30f;
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                        "PE_SemesterGoal".Translate());
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
                        1000,
                        100000,
                        leftAlignedLabel: "PE_Xp".Translate(1000.ToString("N0")),
                        rightAlignedLabel: "PE_Xp".Translate(100000.ToString("N0")),
                        roundTo: 1000);
                    curY += 30f;
                }

                Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f),
                    "PE_Xp".Translate(studyGroup.semesterGoal.ToString("N0")));
                curY += 30f;
                break;
        }
    }

    private void DrawSkillUI(Rect rect, ref float curY, IClassDialog classDialog)
    {
        switch (classDialog)
        {
            case Dialog_CreateClass:
                Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                    "PE_SkillFocus".Translate());
                if (Widgets.ButtonText(
                        new Rect(rect.x + 160f, curY, 200f, 25f),
                        SkillFocus?.LabelCap ?? "PE_Select".Translate()))
                {
                    var options = DefDatabase<SkillDef>.AllDefs
                        .Select(skillDef =>
                            new FloatMenuOption(skillDef.LabelCap, () =>
                            {
                                SkillFocus = skillDef;
                                studyGroup.subjectLogic.UnassignParticipants(classDialog);
                            }))
                        .ToList();

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                curY += 30f;
                break;
            case Dialog_EditClass:
                Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                    "PE_SkillFocus".Translate());
                Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f),
                    SkillFocus?.LabelCap ?? "NoneBrackets".Translate());
                curY += 30f;
                break;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref skillFocus, "skillFocus");
        if (Scribe.mode == LoadSaveMode.PostLoadInit
            && skillFocus == null)
        {
            EducationManager.Instance.RemoveStudyGroup(studyGroup);
            EducationLog.Error(studyGroup?.className + " had no skill focus, removing it...");
        }
    }

    public override HashSet<ThingDef> GetValidLearningBenches()
    {
        if (cachedValidLearningBenches != null)
        {
            return cachedValidLearningBenches;
        }

        cachedValidLearningBenches = [];
        var requirement = SkillFocus?.GetModExtension<SkillBuildingRequirement>();
        if (!requirement?.requiredBuildings.NullOrEmpty() ?? false)
        {
            cachedValidLearningBenches.UnionWith(requirement.requiredBuildings);
        }

        return cachedValidLearningBenches;
    }

    public override AcceptanceReport IsStudentQualified(Pawn student)
    {
        var baseReport = base.IsStudentQualified(student);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        if (SkillFocus == null)
        {
            return new AcceptanceReport("PE_SkillFocusMissing".Translate());
        }

        if (student.DevelopmentalStage < DevelopmentalStage.Child)
        {
            return new AcceptanceReport("PE_TooYoung".Translate(student.LabelShortCap));
        }

        var skill = SkillFocus != null ? student.skills?.GetSkill(SkillFocus) : null;
        if (SkillFocus != null
            && skill != null
            && skill.TotallyDisabled)
        {
            return new AcceptanceReport(
                "PE_StudentSkillDisabled".Translate(student.LabelShort,
                    SkillFocus.LabelCap));
        }

        if (studyGroup.currentProgress > 0f
            && !studyGroup.students.Contains(student))
        {
            return new AcceptanceReport("PE_CannotAddOngoing".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public override AcceptanceReport IsTeacherQualified(Pawn teacher)
    {
        var baseReport = base.IsTeacherQualified(teacher);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        if (SkillFocus == null)
        {
            return new AcceptanceReport("PE_SkillFocusMissing".Translate());
        }

        var skill = teacher.skills?.GetSkill(SkillFocus);
        if (skill == null
            || skill.TotallyDisabled)
        {
            return new AcceptanceReport(
                "PE_TeacherSkillDisabled".Translate(teacher.LabelShort,
                    SkillFocus?.LabelCap ?? ""));
        }

        if (skill.Level < 5)
        {
            return new AcceptanceReport(
                "PE_TeacherNotQualifiedSkill".Translate(teacher.LabelShort,
                    SkillFocus?.LabelCap ?? ""));
        }

        if (studyGroup.currentProgress > 0f
            && teacher != studyGroup.teacher)
        {
            return new AcceptanceReport("PE_CantChangeTeacher".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public override string StudentTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !IsStudentQualified(pawn)
            || SkillFocus == null
            || studyGroup is not { classroom: not null })
        {
            return "";
        }

        var text = new StringBuilder();
        if (pawn.skills?.GetSkill(SkillFocus) is SkillRecord relevantSkill)
        {
            text.AppendLineTagged(relevantSkill.def.LabelCap.AsTipTitle());
            text.AppendLineTagged(
                relevantSkill.def.description.Colorize(ColoredText.SubtleGrayColor));
            text.AppendLine();
            text.AppendLineTagged($"{
                "SkillLevel".Translate().CapitalizeFirst().AsTipTitle()
            }: {
                relevantSkill.GetLevelForUI()
            }");
            text.AppendLine();
            text.AppendLineTagged(
                $"{"Experience".Translate().CapitalizeFirst()}:".AsTipTitle()
                + $" {relevantSkill.xpSinceLastLevel:F0} "
                + $"/ {relevantSkill.XpRequiredForLevelUp:F0}"
            );
            text.AppendLine();
            var xpPerHour = ProgressPerTick * CalculateStudentScore(pawn) * GenDate.TicksPerHour;
            if (xpPerHour > 0)
            {
                text.AppendLineTagged(
                    $"{"PE_HourlyProgress".Translate()}:".AsTipTitle()
                    + $" {
                        xpPerHour
                        * LearningSpeedModifier
                        * studyGroup.classroom.ClassSpeed
                        :F0}"
                );
                text.AppendLine();
            }

            var globalLearningFactor = pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            var learnRateFactor = relevantSkill.LearnRateFactor(true);
            text.AppendLineTagged(
                $"{"PE_LearningFactor".Translate()}:".AsTipTitle()
                + $" {
                    (LearningSpeedModifier
                     * studyGroup.classroom.ClassSpeed
                     * globalLearningFactor
                     * learnRateFactor
                    ).ToStringPercent()
                }"
            );
            text.AppendLine($" - {
                "StatsReport_BaseValue".Translate()
            }: x{
                LearningSpeedModifier.ToStringPercent()
            }");
            text.AppendLine($" - {
                "PE_ClassSpeed".Translate()
            }: x{
                studyGroup.classroom.ClassSpeed.ToStringPercent()
            }");
            text.AppendLine($" - {
                relevantSkill.passion.GetLabel()
            }: x{
                learnRateFactor.ToStringPercent()
            }");
            text.AppendLine($" - {
                StatDefOf.GlobalLearningFactor.LabelCap
            }: x{
                globalLearningFactor.ToStringPercent()
            }");

            if (pawn.needs?.learning != null)
            {
                text.AppendLine();
                var learningPerHour = CalculateLearningPerTick(pawn) * GenDate.TicksPerHour;
                var learningPerSession = learningPerHour * studyGroup.Duration;
                text.AppendLineTagged(
                    $"{NeedDefOf.Learning.LabelCap} {"PE_Session".Translate()}:"
                        .AsTipTitle()
                    + $" {learningPerSession.ToStringPercent()}"
                );
            }
        }

        return text.ToString().TrimEndNewlines();
    }

    public override string TeacherTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !IsTeacherQualified(pawn).Accepted
            || studyGroup is not { classroom: not null }
            || skillFocus == null)
        {
            return "";
        }

        var text = new StringBuilder(base.TeacherTooltipFor(pawn));
        text.AppendLineIfNotEmpty();
        AppendSkillLevel(skillFocus, pawn, text);
        if (skillFocus != SkillDefOf.Social)
        {
            AppendSkillLevel(SkillDefOf.Social, pawn, text);
        }

        if (skillFocus != SkillDefOf.Intellectual)
        {
            AppendSkillLevel(SkillDefOf.Intellectual, pawn, text);
        }

        text.AppendLine();
        var xpPerHour = CalculateTeacherScore(pawn) * GenDate.TicksPerHour;
        if (xpPerHour > 0)
        {
            var socialImpactFactor = CalculateSocialImpactFactor(pawn);
            var passionBonus = CalculatePassionBonus(pawn);
            text.AppendLineTagged(
                $"{"PE_HourlyTeaching".Translate()}:".AsTipTitle()
                + $" {
                    xpPerHour
                    * LearningSpeedModifier
                    * studyGroup.classroom.ClassSpeed
                    :F0}"
            );
            text.AppendLine();
            text.AppendLineTagged(
                $"{"PE_TeachingFactor".Translate()}:".AsTipTitle()
                + $" {
                    (LearningSpeedModifier
                     * studyGroup.classroom.ClassSpeed
                     * socialImpactFactor
                     * passionBonus).ToStringPercent()
                }"
            );
            text.AppendLine($" - {
                "StatsReport_BaseValue".Translate()
            }: x{
                LearningSpeedModifier.ToStringPercent()
            }");
            text.AppendLine($" - {
                "PE_ClassSpeed".Translate()
            }: x{
                studyGroup.classroom.ClassSpeed.ToStringPercent()
            }");
            text.AppendLine(
                $" - {StatDefOf.SocialImpact.LabelCap}:"
                + $" x{socialImpactFactor.ToStringPercent()}"
            );
            text.AppendLine($" - {
                "PE_SkillPassionBonus".Translate()
            }: x{
                passionBonus.ToStringPercent()
            }");
        }

        return text.ToString().TrimEndNewlines();
    }
}