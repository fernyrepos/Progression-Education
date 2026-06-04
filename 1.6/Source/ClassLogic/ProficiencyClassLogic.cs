using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class ProficiencyClassLogic : ClassSubjectLogic
{
    public ProficiencyDef proficiencyTrack;
    public ProficiencyTierDef targetTier;

    public ProficiencyClassLogic()
    {
    }

    public ProficiencyClassLogic(StudyGroup parent)
        : base(parent)
    {
        proficiencyTrack = DefsOf.PE_WeaponTrack;
        targetTier = DefsOf.PE_FirearmTier;
    }

    public override string Description =>
        "PE_TrainingProficiency".Translate(GetLabel(targetTier).CapitalizeFirst());

    public override string Label => "PE_SubjectProficiency".Translate();

    public override float LearningSpeedModifier =>
        EducationMod.settings.proficiencyClassesLearningSpeedModifier;

    public override string LabelFocus => GetLabel(targetTier).CapitalizeFirst();

    public static string GetLabel(ProficiencyTierDef tier)
    {
        if (tier.traitDef.degreeDatas.Count > 0)
        {
            return tier.traitDef.degreeDatas[0].label;
        }
        return tier.label;
    }

    public override float ProgressPerTick
    {
        get
        {
            if (studyGroup.teacher == null
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

    public override float CalculateStudentScore(Pawn student)
    {
        return 0f;
    }

    public override float CalculateTeacherScore(Pawn teacher)
    {
        if (!IsTeacherQualified(teacher))
        {
            return 0f;
        }

        var social = teacher.skills.GetSkill(SkillDefOf.Social).Level;
        var intelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
        var socialImpact = CalculateSocialImpactFactor(teacher);
        var techTraitModifier = CalculateTechTraitModifier(teacher);
        var progress = (social * 0.6f + intelligence * 0.4f) * socialImpact;
        return Mathf.Max(0, progress * techTraitModifier * 0.02f);
    }

    public float CalculateTechTraitModifier(Pawn pawn)
    {
        if (pawn == null)
        {
            return 1f;
        }

        var techTraitModifier = 1f;
        var currentTier = ProficiencyUtility.GetCurrentTier(pawn, proficiencyTrack);
        if (currentTier != null)
        {
            var tierIndex = proficiencyTrack.tiers.IndexOf(currentTier);
            if (currentTier == targetTier)
            {
                techTraitModifier += 0.2f;
            }
            else if (tierIndex > 0)
            {
                techTraitModifier -= 0.1f;
            }
        }

        return techTraitModifier;
    }

    public override ClassSubjectLogic DeepClone(StudyGroup parent)
    {
        return new ProficiencyClassLogic(parent)
        {
            proficiencyTrack = proficiencyTrack,
            targetTier = targetTier,
        };
    }

    public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
    {
        DrawProficiencyUI(rect, ref curY, classDialog);
        var progressPerTick = ProgressPerTick;
        if (progressPerTick <= 0)
        {
            return;
        }

        var progressRemaining = studyGroup.semesterGoal - studyGroup.currentProgress;
        var estimatedTicks = Mathf.CeilToInt(progressRemaining / progressPerTick);
        Widgets.Label(new Rect(rect.x, curY, 360f, 25f),
            "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
        curY += 30f;
        var sessionsNeeded =
            Mathf.Ceil(
                (float)estimatedTicks / (GenDate.TicksPerHour * studyGroup.Duration));
        Widgets.Label(new Rect(rect.x, curY, 360f, 25f),
            "PE_StudySessionsNeeded".Translate(sessionsNeeded.ToString("F0")
                .Colorize(ColoredText.DateTimeColor)));
        curY += 30f;
    }

    private void DrawProficiencyUI(Rect rect, ref float curY, IClassDialog classDialog)
    {
        var proficiencyLabel = GetLabel(targetTier).CapitalizeFirst();
        switch (classDialog)
        {
            case Dialog_CreateClass:
                Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                    "PE_ProficiencyFocus".Translate());
                if (Widgets.ButtonText(
                        new Rect(rect.x + 160f, curY, 200f, 25f),
                        proficiencyLabel))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
                    {
                        if (!ProficiencyUtility.IsTrackEnabled(track)) continue;
                        for (int i = 1; i < track.tiers.Count; i++)
                        {
                            var tier = track.tiers[i];
                            options.Add(new FloatMenuOption(GetLabel(tier).CapitalizeFirst(), () =>
                            {
                                proficiencyTrack = track;
                                targetTier = tier;
                                studyGroup.semesterGoal = tier.semesterGoal;
                                studyGroup.subjectLogic.UnassignParticipants(classDialog);
                            }));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                curY += 30;
                break;
            case Dialog_EditClass:
                Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                    "PE_ProficiencyFocus".Translate());
                Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f),
                    proficiencyLabel);
                curY += 30;
                break;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        string rawFocus = null;
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref rawFocus, "proficiencyFocus");
        }
        Scribe_Defs.Look(ref proficiencyTrack, "proficiencyTrack");
        Scribe_Defs.Look(ref targetTier, "targetTier");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && rawFocus != null)
        {
            foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
            {
                foreach (var tier in track.tiers)
                {
                    if (tier.legacyNames.Contains(rawFocus))
                    {
                        proficiencyTrack = track;
                        targetTier = tier;
                        break;
                    }
                }
            }
        }
    }

    public override void GrantCompletionRewards()
    {
        foreach (var student in studyGroup.students)
        {
            ProficiencyUtility.GrantTier(student, proficiencyTrack, targetTier);
        }
    }

    private bool HasProficiency(Pawn pawn, out string proficiencyLabel)
    {
        proficiencyLabel = GetLabel(targetTier);
        return ProficiencyUtility.MeetsOrExceedsTier(pawn, proficiencyTrack, targetTier);
    }

    public override AcceptanceReport IsStudentQualified(Pawn student)
    {
        var baseReport = base.IsStudentQualified(student);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        if (student.DevelopmentalStage < DevelopmentalStage.Child)
        {
            return new AcceptanceReport("PE_TooYoung".Translate(student.LabelShortCap));
        }

        if (studyGroup.currentProgress > 0f
            && !studyGroup.students.NotNullAndContains(student))
        {
            return new AcceptanceReport("PE_CannotAddOngoing".Translate());
        }

        var targetIdx = proficiencyTrack.tiers.IndexOf(targetTier);
        var tierBelow = targetIdx > 0 ? proficiencyTrack.tiers[targetIdx - 1] : null;

        if (tierBelow != null && !ProficiencyUtility.IsOneTierBelow(student, proficiencyTrack, targetTier))
        {
            return new AcceptanceReport("PE_StudentMustBeOneTierBelow".Translate(GetLabel(tierBelow)));
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

        if (!ProficiencyUtility.MeetsOrExceedsTier(teacher, proficiencyTrack, targetTier))
        {
            return new AcceptanceReport("PE_TeacherMustHaveProficiency".Translate(GetLabel(targetTier)));
        }

        if (studyGroup.currentProgress > 0f
            && teacher != studyGroup.teacher)
        {
            return new AcceptanceReport("PE_CantChangeTeacher".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public override string TeacherTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !IsTeacherQualified(pawn)
            || studyGroup is not { classroom: not null, semesterGoal: > 0 })
        {
            return "";
        }

        var text = new StringBuilder(base.TeacherTooltipFor(pawn));
        text.AppendLineIfNotEmpty();
        AppendSkillLevel(SkillDefOf.Social, pawn, text);
        AppendSkillLevel(SkillDefOf.Intellectual, pawn, text);
        text.AppendLine();
        var xpPerHour = CalculateTeacherScore(pawn) * GenDate.TicksPerHour;
        if (xpPerHour > 0)
        {
            var percentPerHour = xpPerHour / studyGroup.semesterGoal;
            text.AppendLineTagged($"{"PE_HourlyTeaching".Translate().AsTipTitle()}: {percentPerHour.ToStringPercent()}");
            text.AppendLine();
        }

        var socialImpact = CalculateSocialImpactFactor(pawn);
        text.AppendLineTagged(
            $"{"PE_TeachingFactor".Translate()}:".AsTipTitle()
            + $" {(LearningSpeedModifier
                 * studyGroup.classroom.ClassSpeed
                 * socialImpact
                ).ToStringPercent()}"
        );
        var techTraitModifier = CalculateTechTraitModifier(pawn);
        text.AppendLine($" - {"PE_ProficiencyFocus".Translate()}: x{techTraitModifier.ToStringPercent()}");
        text.AppendLine(
            $" - {StatDefOf.SocialImpact.LabelCap}:"
            + $" x{socialImpact.ToStringPercent()}");

        return text.ToString().TrimEndNewlines();
    }
}