using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class ProficiencyClassLogic : ClassSubjectLogic
{
    public const int FirearmTeachingDuration = 30000;
    public const int HighTechTeachingDuration = 60000;
    private ProficiencyLevel proficiencyFocus = ProficiencyLevel.Firearm;

    public ProficiencyClassLogic()
    {
    }

    public ProficiencyClassLogic(StudyGroup parent)
        : base(parent)
    {
    }

    public override string Description =>
        "PE_TrainingProficiency".Translate(ProficiencyFocus.ToStringHuman());

    public override string Label => "PE_SubjectProficiency".Translate();

    public override float LearningSpeedModifier =>
        EducationSettings.Instance.proficiencyClassesLearningSpeedModifier;

    public ProficiencyLevel ProficiencyFocus
    {
        get => proficiencyFocus;
        set
        {
            if (proficiencyFocus != value)
            {
                proficiencyFocus = value;
                cachedValidLearningBenches = null;
            }
        }
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
        if (pawn.story.traits.HasTrait(DefsOf.PE_FirearmProficiency))
        {
            if (proficiencyFocus == ProficiencyLevel.Firearm)
            {
                techTraitModifier += 0.2f;
            }
            else
            {
                techTraitModifier -= 0.1f;
            }
        }

        if (pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency))
        {
            if (ProficiencyFocus == ProficiencyLevel.HighTech)
            {
                techTraitModifier += 0.2f;
            }
            else
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
            proficiencyFocus = proficiencyFocus,
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
        var proficiencyLabel = ProficiencyFocus.ToStringHuman();
        switch (classDialog)
        {
            case Dialog_CreateClass:
                Widgets.Label(new Rect(rect.x, curY, 150f, 25f),
                    "PE_ProficiencyFocus".Translate());
                if (Widgets.ButtonText(
                        new Rect(rect.x + 160f, curY, 200f, 25f),
                        proficiencyLabel))
                {
                    List<FloatMenuOption> options =
                    [
                        new(ProficiencyLevel.Firearm.ToStringHuman().CapitalizeFirst(),
                            () =>
                            {
                                ProficiencyFocus = ProficiencyLevel.Firearm;
                                studyGroup.semesterGoal = FirearmTeachingDuration;
                                studyGroup.subjectLogic.UnassignParticipants(classDialog);
                            }),
                        new(ProficiencyLevel.HighTech.ToStringHuman().CapitalizeFirst(),
                            () =>
                            {
                                ProficiencyFocus = ProficiencyLevel.HighTech;
                                studyGroup.semesterGoal = HighTechTeachingDuration;
                                studyGroup.subjectLogic.UnassignParticipants(classDialog);
                            }),
                    ];
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
        Scribe_Values.Look(ref proficiencyFocus, "proficiencyFocus");
    }

    public override void GrantCompletionRewards()
    {
        var traitDef = ProficiencyFocus switch
        {
            ProficiencyLevel.Firearm => DefsOf.PE_FirearmProficiency,
            ProficiencyLevel.HighTech => DefsOf.PE_HighTechProficiency,
            _ => null,
        };

        foreach (var student in studyGroup.students)
        {
            ProficiencyUtility.GrantProficiencyTrait(student, traitDef);
        }
    }

    private bool HasProficiency(Pawn pawn, out string proficiencyLabel)
    {
        (proficiencyLabel, var hasProficiency) = ProficiencyFocus switch
        {
            ProficiencyLevel.Firearm => (
                ProficiencyLevel.Firearm.ToStringHuman(),
                pawn.story.traits.HasTrait(DefsOf.PE_FirearmProficiency)
                || pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency)
            ),
            ProficiencyLevel.HighTech => (
                ProficiencyLevel.HighTech.ToStringHuman(),
                pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency)
            ),
            _ => ("", false),
        };
        return hasProficiency;
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

        if (HasProficiency(student, out var proficiencyLabel))
        {
            return new AcceptanceReport(
                "PE_StudentAlreadyHasProficiency".Translate(student.LabelShort,
                    proficiencyLabel));
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

        if (!HasProficiency(teacher, out var proficiencyLabel))
        {
            return new AcceptanceReport(
                "PE_TeacherNotQualifiedProficiency".Translate(teacher.LabelShort,
                    proficiencyLabel));
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
            text.AppendLineTagged($"{
                "PE_HourlyTeaching".Translate().AsTipTitle()
            }: {
                percentPerHour.ToStringPercent()
            }");
            text.AppendLine();
        }

        var socialImpact = CalculateSocialImpactFactor(pawn);
        text.AppendLineTagged(
            $"{"PE_TeachingFactor".Translate()}:".AsTipTitle()
            + $" {
                (LearningSpeedModifier
                 * studyGroup.classroom.ClassSpeed
                 * socialImpact
                ).ToStringPercent()
            }"
        );
        var techTraitModifier = CalculateTechTraitModifier(pawn);
        text.AppendLine($" - {
            "PE_ProficiencyFocus".Translate()
        }: x{
            techTraitModifier.ToStringPercent()
        }");
        text.AppendLine(
            $" - {StatDefOf.SocialImpact.LabelCap}:"
            + $" x{socialImpact.ToStringPercent()}");

        return text.ToString().TrimEndNewlines();
    }
}