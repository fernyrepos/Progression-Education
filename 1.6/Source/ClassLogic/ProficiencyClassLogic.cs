using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class ProficiencyClassLogic : ClassSubjectLogic
{
    private const float ProgressSpeedMultiplier = 0.5f;

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

    public override bool IsEnabled => EducationMod.settings.enableProficiencySystem;

    public override int DefaultSemesterGoal => targetTier.semesterGoal;

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
                || studyGroup.classroom == null
                || studyGroup.teacher.jobs?.curDriver is not JobDriver_Teach)
            {
                return 0f;
            }

            return Mathf.Max(0,
                CalculateTeacherScore(studyGroup.teacher)
                * studyGroup.classroom.ClassSpeed
                * LearningSpeedModifier
                * ProgressSpeedMultiplier);
        }
    }

    public override float CalculateStudentScore(Pawn student)
    {
        if (student == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, student.GetStatValue(StatDefOf.GlobalLearningFactor));
    }

    public override void ApplyLearningTick(Pawn student, int delta)
    {
        base.ApplyLearningTick(student, delta);
        if (student == null)
        {
            return;
        }

        if (ProficiencyUtility.MeetsOrExceedsTier(student, proficiencyTrack, targetTier))
        {
            EducationManager.Instance.ClearProficiencyClassProgress(student, proficiencyTrack, targetTier);
            return;
        }

        var studentLearningFactor = CalculateStudentScore(student);
        var progressGain = ProgressPerTick * studentLearningFactor * delta;
        if (progressGain <= 0f)
        {
            return;
        }

        var progress = EducationManager.Instance.AddProficiencyClassProgress(
            student,
            proficiencyTrack,
            targetTier,
            progressGain,
            studyGroup.semesterGoal);
        if (progress < studyGroup.semesterGoal)
        {
            UpdateGroupProgress();
            return;
        }

        ProficiencyUtility.GrantTier(student, proficiencyTrack, targetTier);
        EducationManager.Instance.ClearProficiencyClassProgress(student, proficiencyTrack, targetTier);
        studyGroup.RemoveStudent(student);
        UpdateGroupProgress();
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
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (rawFocus != null)
            {
                foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
                {
                    foreach (var tier in track.tiers)
                    {
                        if (tier.legacyNames != null && tier.legacyNames.Contains(rawFocus))
                        {
                            proficiencyTrack = track;
                            targetTier = tier;
                            break;
                        }
                    }
                }
            }
            proficiencyTrack ??= DefsOf.PE_WeaponTrack;
            targetTier ??= DefsOf.PE_FirearmTier;
        }
    }

    public override void GrantCompletionRewards()
    {
        // Proficiency graduation is applied per student in ApplyLearningTick once they hit semester goal.
    }

    public override void HandleStudentLifecycleEvents()
    {
        var completedStudents = studyGroup.students
            .Where(student => ProficiencyUtility.MeetsOrExceedsTier(student, proficiencyTrack, targetTier))
            .ToList();
        foreach (var student in completedStudents)
        {
            EducationManager.Instance.ClearProficiencyClassProgress(student, proficiencyTrack, targetTier);
            studyGroup.RemoveStudent(student);
        }

        UpdateGroupProgress();
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

    public override string StudentTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !studyGroup.students.Contains(pawn)
            || studyGroup.semesterGoal <= 0)
        {
            return "";
        }

        var text = new StringBuilder(base.StudentTooltipFor(pawn));
        var progress = EducationManager.Instance.GetProficiencyClassProgress(pawn, proficiencyTrack, targetTier);
        var progressPercent = Mathf.Clamp01(progress / studyGroup.semesterGoal);
        text.AppendLineIfNotEmpty();
        text.AppendLineTagged($"{GetLabel(targetTier).CapitalizeFirst().AsTipTitle()}: {progressPercent.ToStringPercent()}");
        text.AppendLineTagged($"{"PE_ProgressFormat".Translate(progress.ToString("F0"), studyGroup.semesterGoal.ToString())}");
        return text.ToString().TrimEndNewlines();
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
        var progressPerHour = CalculateTeacherScore(pawn) * studyGroup.classroom.ClassSpeed * LearningSpeedModifier * ProgressSpeedMultiplier;
        var xpPerHour = progressPerHour * GenDate.TicksPerHour;
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

    public bool TryGetProgressRange(out float minProgress, out float maxProgress, out string minStudent, out string maxStudent)
    {
        minProgress = 0f;
        maxProgress = 0f;
        minStudent = "";
        maxStudent = "";
        if (studyGroup is not { semesterGoal: > 0 }
            || studyGroup.students.Count == 0)
        {
            return false;
        }

        var goal = studyGroup.semesterGoal;
        var educationManager = EducationManager.Instance;
        var firstStudent = studyGroup.students[0];
        minProgress = ProficiencyUtility.MeetsOrExceedsTier(firstStudent, proficiencyTrack, targetTier)
            ? goal
            : educationManager.GetProficiencyClassProgress(firstStudent, proficiencyTrack, targetTier);
        minProgress = Mathf.Clamp(minProgress, 0f, goal);
        maxProgress = minProgress;
        minStudent = firstStudent.LabelShort;
        maxStudent = firstStudent.LabelShort;
        for (var i = 1; i < studyGroup.students.Count; i++)
        {
            var student = studyGroup.students[i];
            var progress = ProficiencyUtility.MeetsOrExceedsTier(student, proficiencyTrack, targetTier)
                ? goal
                : educationManager.GetProficiencyClassProgress(student, proficiencyTrack, targetTier);
            progress = Mathf.Clamp(progress, 0f, goal);
            if (progress < minProgress)
            {
                minProgress = progress;
                minStudent = student.LabelShort;
            }
            if (progress > maxProgress)
            {
                maxProgress = progress;
                maxStudent = student.LabelShort;
            }
        }

        return true;
    }

    private void UpdateGroupProgress()
    {
        var studentCount = studyGroup.students.Count;
        if (studentCount == 0)
        {
            studyGroup.currentProgress = studyGroup.semesterGoal;
            return;
        }

        var totalProgress = 0f;
        foreach (var student in studyGroup.students)
        {
            if (ProficiencyUtility.MeetsOrExceedsTier(student, proficiencyTrack, targetTier))
            {
                totalProgress += studyGroup.semesterGoal;
                continue;
            }

            totalProgress += EducationManager.Instance.GetProficiencyClassProgress(student, proficiencyTrack, targetTier);
        }

        studyGroup.currentProgress = totalProgress / studentCount;
    }
}